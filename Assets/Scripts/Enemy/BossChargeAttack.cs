using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Unity.AI.Navigation;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class BossChargeAttack : MonoBehaviour
{
    [SerializeField] private bool phaseTwoOnly = true;
    [SerializeField, Min(0f)] private float minStartDistance = 4f;
    [SerializeField, Min(0.1f)] private float maxStartDistance = 10f;
    [SerializeField, Min(0.1f)] private float prepareDuration = 1.2f;
    [SerializeField, Range(0f, 1f)] private float aimLockRatio = 0.7f;
    [SerializeField, Min(0.1f)] private float chargeSpeed = 10f;
    [SerializeField, Min(0.1f)] private float maxChargeDistance = 12f;
    [SerializeField, Min(0f)] private float overshootDistance = 1.5f;
    [SerializeField, Min(0f)] private float recoveryDuration = 1.2f;
    [SerializeField, Min(0f)] private float chargeCooldown = 12f;
    [SerializeField, Min(1)] private int chargeDamage = 20;

    [Header("Warning")]
    [SerializeField] private Material warningMaterial;
    [SerializeField] private Color prepareColor = Color.yellow;
    [SerializeField] private Color dashColor = Color.red;
    [SerializeField, Min(0.01f)] private float warningLineWidth = 0.08f;
    [SerializeField] private float groundOffset = 0.05f;

    private enum Stage { Inactive, Preparing, Dashing, Recovering, Finished }
    private Stage stage;
    private EnemyController enemy;
    private CharacterController controller;
    private NavMeshSurface surface;
    private LayerMask obstacleLayer;
    private LineRenderer warning;
    private Vector3 direction;
    private Vector3 origin;
    private float plannedDistance;
    private float travelled;
    private float elapsed;
    private float nextChargeTime;
    private bool hitProcessed;
    private bool aimLocked;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int ChargePreparingHash = Animator.StringToHash("IsChargePreparing");

    public bool IsDashing => stage == Stage.Dashing && isActiveAndEnabled;
    public bool IsFinished => stage == Stage.Finished || stage == Stage.Inactive;
    private float BodyRadius => Mathf.Max(0.05f,Mathf.Max(controller.bounds.extents.x, controller.bounds.extents.z));
    private float ContactMargin => Mathf.Max(0.03f,controller.skinWidth * Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z)) + 0.03f);

    public void Initialize(EnemyController owner, CharacterController body, NavMeshSurface navigationSurface, LayerMask obstacles)
    {
        enemy = owner;
        controller = body;
        surface = navigationSurface;
        obstacleLayer = obstacles;

        if (warning != null)
            return;

        GameObject visual = new GameObject(name + "_ChargeWarning");
        visual.layer = gameObject.layer;
        visual.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        warning = visual.AddComponent<LineRenderer>();
        warning.useWorldSpace = true;
        warning.alignment = LineAlignment.TransformZ;
        warning.loop = true;
        warning.positionCount = 4;
        warning.sharedMaterial = warningMaterial;
        warning.shadowCastingMode = ShadowCastingMode.Off;
        warning.receiveShadows = false;
        visual.SetActive(false);
    }

    public bool CanStart()
    {
        if (!isActiveAndEnabled || enemy == null || !enemy.IsBoss ||
            !enemy.IsInCombat || enemy.Hp <= 0 || enemy.Player == null ||
            !enemy.CanAttack || Time.time < nextChargeTime ||
            surface == null || warningMaterial == null || warning == null)
            return false;

        if (phaseTwoOnly && enemy.BossPhase < 2)
            return false;

        Vector3 delta = enemy.Player.position - transform.position;
        if (Mathf.Abs(delta.y) > 1.5f)
            return false;

        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance < minStartDistance || distance > maxStartDistance ||
            distance > maxChargeDistance)
            return false;

        Collider target = enemy.Player.GetComponent<Collider>();
        Vector3 targetCenter = target != null
            ? target.bounds.center : enemy.Player.position + Vector3.up;
        if (Physics.Linecast(controller.bounds.center, targetCenter,
            obstacleLayer, QueryTriggerInteraction.Ignore))
            return false;

        float freeDistance = GetAllowedTravel(delta.normalized, distance);
        return freeDistance >= distance - ContactMargin;
    }

    public void Begin()
    {
        stage = Stage.Preparing;
        elapsed = 0f;
        travelled = 0f;
        hitProcessed = false;
        aimLocked = false;
        origin = FeetPosition();
        enemy.Animator.ResetTrigger("Attack");
        enemy.Animator.ResetTrigger("Hit");
        enemy.Animator.SetFloat(SpeedHash, 0f);
        enemy.Animator.SetBool(ChargePreparingHash, true);
        enemy.Sfx?.PlayChargePrepare();
        Aim();
        warning.sharedMaterial = warningMaterial;
        warning.widthMultiplier = warningLineWidth;
        SetWarningColor(prepareColor);
        DrawWarning();
        warning.gameObject.SetActive(true);
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || IsFinished)
            return;

        if (enemy.Player == null || enemy.Hp <= 0)
        {
            stage = Stage.Finished;
            HideWarning();
            return;
        }

        elapsed += deltaTime;
        if (stage == Stage.Preparing)
        {
            if (!aimLocked)
            {
                Aim();
                DrawWarning();
                aimLocked = elapsed >= prepareDuration * aimLockRatio;
            }

            if (elapsed >= prepareDuration)
            {
                if (plannedDistance <= ContactMargin)
                {
                    BeginRecovery();
                    return;
                }
                stage = Stage.Dashing;
                elapsed = 0f;
                enemy.Animator.SetBool(ChargePreparingHash, false);
                SetWarningColor(dashColor);
                enemy.Animator.SetFloat(SpeedHash, 1f);
            }
        }
        else if (stage == Stage.Dashing)
        {
            if (elapsed >= plannedDistance / Mathf.Max(0.1f, chargeSpeed) + 0.5f)
                BeginRecovery();
        }
        else if (stage == Stage.Recovering && elapsed >= recoveryDuration)
            stage = Stage.Finished;
    }

    private void Aim()
    {
        if (enemy.Player == null)
            return;

        Vector3 delta = enemy.Player.position - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.0001f)
            return;

        direction = delta.normalized;
        transform.rotation = Quaternion.LookRotation(direction);
        plannedDistance = GetAllowedTravel(direction,
            Mathf.Min(maxChargeDistance, delta.magnitude + overshootDistance));
    }

    public void ApplyDashMovement(float verticalVelocity, float deltaTime)
    {
        if (!IsDashing || deltaTime <= 0f)
            return;

        float requested = Mathf.Min(chargeSpeed * deltaTime,
            Mathf.Max(0f, plannedDistance - travelled));
        float allowed = GetAllowedTravel(direction, requested);
        bool blockedAhead = allowed + 0.001f < requested;
        Vector3 before = transform.position;
        Vector3 beforeCenter = controller.bounds.center;
        GetCapsule(out Vector3 top, out Vector3 bottom, out float radius);

        Collider hitCollider = FindPlayerHit(top, bottom, radius, allowed,
            out float hitDistance);

        Vector3 motion = direction * allowed;
        motion.y = verticalVelocity * deltaTime;
        CollisionFlags flags = controller.Move(motion);

        float actualTravel = Mathf.Max(0f,
            Vector3.Dot(transform.position - before, direction));
        travelled += actualTravel;

        if (!hitProcessed && hitCollider != null &&
            hitDistance <= actualTravel + ContactMargin &&
            !Physics.Linecast(beforeCenter, hitCollider.bounds.center,
                obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            hitProcessed = true;
            BeginRecovery();
            if (enemy.Player != null &&
                enemy.Player.TryGetComponent<PlayerController>(out var target))
                target.TakeDamage(chargeDamage, enemy.gameObject, false);
            return;
        }

        if (blockedAhead || allowed <= 0.001f ||
            (flags & CollisionFlags.Sides) != 0 ||
            travelled >= plannedDistance - 0.01f)
            BeginRecovery();
    }

    private Collider FindPlayerHit(Vector3 top, Vector3 bottom, float radius,
        float distance, out float hitDistance)
    {
        hitDistance = float.PositiveInfinity;
        if (hitProcessed || enemy.Player == null)
            return null;

        foreach (Collider candidate in Physics.OverlapCapsule(top, bottom,
            radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!IsPlayerCollider(candidate))
                continue;
            hitDistance = 0f;
            return candidate;
        }

        Collider nearest = null;
        foreach (RaycastHit hit in Physics.CapsuleCastAll(top, bottom, radius,
            direction, distance + ContactMargin, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance >= hitDistance || !IsPlayerCollider(hit.collider))
                continue;
            nearest = hit.collider;
            hitDistance = hit.distance;
        }
        return nearest;
    }

    private bool IsPlayerCollider(Collider candidate)
    {
        return candidate != null && enemy.Player != null &&
            (candidate.transform == enemy.Player ||
             candidate.transform.IsChildOf(enemy.Player));
    }

    private float GetAllowedTravel(Vector3 heading, float requested)
    {
        if (surface == null || requested <= 0f)
            return 0f;

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = surface.agentTypeID,
            areaMask = NavMesh.AllAreas
        };

        if (!NavMesh.SamplePosition(FeetPosition(), out NavMeshHit start,
            0.5f, filter))
            return 0f;

        float allowed = requested;
        if (NavMesh.Raycast(start.position,
            start.position + heading * requested, out NavMeshHit edge, filter))
        {
            Vector3 delta = edge.position - start.position;
            delta.y = 0f;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, delta.magnitude - 0.03f));
        }

        GetCapsule(out Vector3 top, out Vector3 bottom, out float radius);
        if (Physics.CapsuleCast(top, bottom, radius, heading,
            out RaycastHit wall, allowed + 0.03f, obstacleLayer,
            QueryTriggerInteraction.Ignore))
            allowed = Mathf.Min(allowed, Mathf.Max(0f, wall.distance - 0.03f));

        return allowed;
    }

    private void GetCapsule(out Vector3 top, out Vector3 bottom, out float radius)
    {
        Bounds bounds = controller.bounds;
        radius = BodyRadius;
        float halfLine = Mathf.Max(0f, bounds.extents.y - radius);
        top = bounds.center + Vector3.up * halfLine;
        bottom = bounds.center - Vector3.up * halfLine;
    }

    private Vector3 FeetPosition()
    {
        return transform.TransformPoint(
            controller.center - Vector3.up * controller.height * 0.5f);
    }

    private void BeginRecovery()
    {
        stage = Stage.Recovering;
        elapsed = 0f;

        enemy.Animator.SetBool(ChargePreparingHash, false);
        enemy.Animator.SetFloat(SpeedHash, 0f);

        HideWarning();
    }

    public void Cancel()
    {
        if (stage != Stage.Inactive)
            nextChargeTime = Time.time + chargeCooldown;

        stage = Stage.Inactive;

        if (enemy != null && enemy.Animator != null)
        {
            enemy.Animator.SetBool(ChargePreparingHash, false);
            enemy.Animator.SetFloat(SpeedHash, 0f);
        }

        HideWarning();
    }

    private void DrawWarning()
    {
        Vector3 start = origin + Vector3.up * groundOffset;
        Vector3 end = start + direction * plannedDistance;
        Vector3 side = Vector3.Cross(Vector3.up, direction) * BodyRadius;
        warning.SetPosition(0, start - side);
        warning.SetPosition(1, start + side);
        warning.SetPosition(2, end + side);
        warning.SetPosition(3, end - side);
    }

    private void SetWarningColor(Color color)
    {
        warning.startColor = color;
        warning.endColor = color;
    }

    private void HideWarning()
    {
        if (warning != null)
            warning.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Cancel();
    }

    private void OnDestroy()
    {
        if (warning != null)
            Destroy(warning.gameObject);
    }
}
