using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class EnemyController : MonoBehaviour, IDamageable
{
    private StateMachine stateMachine;

    private IdleState idleState;
    private ChaseState chaseState;
    private AttackState attackState;
    private DeadState deadState;
    private HitState hitState;

    [SerializeField] private Transform player;

    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float detectRange = 8f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int hp = 100;
    [SerializeField] private int maxHp = 100;
    [SerializeField] private float hitDuration = 0.4f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Navigation")]
    [SerializeField] private NavMeshSurface navigationSurface;
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float sampleDistance = 1f;
    [SerializeField] private float chaseLoseRange = 15f;

    [Header("Attack Obstruction")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private float wanderSpeed = 1.5f;

    private Animator animator;
    private CharacterController controller;

    private float verticalVelocity;
    private Vector3 moveDirection;
    private float lastAttackTime = -999f;
    private bool victoryProcessed;

    private NavMeshPath navigationPath;
    private Vector3[] pathCorners = Array.Empty<Vector3>();
    private int cornerIndex;
    private float nextRepathTime;

    public Transform Player => player;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float DetectRange => detectRange;
    public float AttackRange => attackRange;
    public float HitDuration => hitDuration;
    public float ChaseLoseRange => chaseLoseRange;
    public Animator Animator => animator;
    public int Hp => hp;
    public int MaxHp => maxHp;

    public IdleState IdleState => idleState;
    public ChaseState ChaseState => chaseState;
    public AttackState AttackState => attackState;
    public HitState HitState => hitState;
    public DeadState DeadState => deadState;

    public bool CanAttack =>
        Time.time >= lastAttackTime + attackCooldown;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        stateMachine = new StateMachine();
        navigationPath = new NavMeshPath();

        idleState = new IdleState(this, stateMachine);
        chaseState = new ChaseState(this, stateMachine);
        attackState = new AttackState(this, stateMachine);
        hitState = new HitState(this, stateMachine);
        deadState = new DeadState(this, stateMachine);

        hp = maxHp;
    }

    private void Start()
    {
        stateMachine.ChangeState(idleState);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        moveDirection = Vector3.zero;

        stateMachine.Update();
        ApplyMovement();
    }

    public float GetDistanceToPlayer()
    {
        if (player == null)
            return float.PositiveInfinity;

        return Vector3.Distance(transform.position, player.position);
    }

    public void Move(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        moveDirection = direction;
    }

    private void ApplyMovement()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = moveDirection * moveSpeed;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);
    }

    public void MoveToPlayer()
    {
        if (player == null)
        {
            Move(Vector3.zero);
            animator.SetFloat("Speed", 0f);
            return;
        }

        MoveToPosition(player.position);
    }

    public void MoveToPosition(
        Vector3 destination,
        bool walking = false)
    {
        Move(Vector3.zero);
        animator.SetFloat("Speed", 0f);

        if (navigationSurface == null || !controller.isGrounded)
            return;

        if (moveSpeed <= 0f || Time.deltaTime <= 0f)
            return;

        if (Time.time >= nextRepathTime)
        {
            RebuildPath(destination);
        }

        float currentSpeed = walking
            ? Mathf.Clamp(wanderSpeed, 0f, moveSpeed)
            : moveSpeed;

        if (currentSpeed <= 0f)
            return;

        while (cornerIndex < pathCorners.Length)
        {
            Vector3 direction =
                pathCorners[cornerIndex] - GetFeetPosition();

            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance <= 0.02f)
            {
                cornerIndex++;
                continue;
            }

            float frameDistance = currentSpeed * Time.deltaTime;
            float actualDistance = Mathf.Min(distance, frameDistance);

            float moveRatio =
                actualDistance / (moveSpeed * Time.deltaTime);

            Move(direction.normalized * moveRatio);
            LookInDirection(direction);

            animator.SetFloat("Speed", walking ? 0.5f : 1f);
            return;
        }
    }

    private void LookInDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void LookAtPlayer()
    {
        if (player == null)
            return;

        LookInDirection(player.position - transform.position);
    }

    private Vector3 GetFeetPosition()
    {
        return transform.TransformPoint(
            controller.center - Vector3.up * controller.height * 0.5f
        );
    }

    private NavMeshQueryFilter CreateNavigationFilter()
    {
        return new NavMeshQueryFilter
        {
            agentTypeID = navigationSurface.agentTypeID,
            areaMask = NavMesh.AllAreas
        };
    }

    public void ClearNavigationPath()
    {
        pathCorners = Array.Empty<Vector3>();
        cornerIndex = 0;
        nextRepathTime = 0f;
    }

    private void RebuildPath(Vector3 destination)
    {
        nextRepathTime =
            Time.time + Mathf.Max(0.05f, repathInterval);

        pathCorners = Array.Empty<Vector3>();
        cornerIndex = 0;

        if (navigationSurface == null)
            return;

        NavMeshQueryFilter filter = CreateNavigationFilter();

        if (!NavMesh.SamplePosition(
            GetFeetPosition(),
            out NavMeshHit startHit,
            sampleDistance,
            filter))
        {
            return;
        }

        if (!NavMesh.SamplePosition(
            destination,
            out NavMeshHit targetHit,
            sampleDistance,
            filter))
        {
            return;
        }

        bool found = NavMesh.CalculatePath(
            startHit.position,
            targetHit.position,
            filter,
            navigationPath
        );

        if (!found ||
            navigationPath.status != NavMeshPathStatus.PathComplete)
        {
            return;
        }

        pathCorners = navigationPath.corners;
        cornerIndex = 1;
    }

    public bool TryGetWanderDestination(
        Vector3 center,
        out Vector3 destination)
    {
        destination = transform.position;

        if (navigationSurface == null || !controller.isGrounded)
            return false;

        NavMeshQueryFilter filter = CreateNavigationFilter();
        Vector3 feetPosition = GetFeetPosition();

        if (!NavMesh.SamplePosition(
            feetPosition,
            out NavMeshHit startHit,
            sampleDistance,
            filter))
        {
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
            Vector2 randomOffset =
                UnityEngine.Random.insideUnitCircle * wanderRadius;

            Vector3 candidate = new Vector3(
                center.x + randomOffset.x,
                startHit.position.y,
                center.z + randomOffset.y
            );

            if (!NavMesh.SamplePosition(
                candidate,
                out NavMeshHit targetHit,
                sampleDistance,
                filter))
            {
                continue;
            }

            if (Mathf.Abs(
                targetHit.position.y - startHit.position.y) > 0.5f)
            {
                continue;
            }

            Vector3 fromCenter = targetHit.position - center;
            fromCenter.y = 0f;

            if (fromCenter.sqrMagnitude > wanderRadius * wanderRadius)
                continue;

            Vector3 fromCurrent = targetHit.position - feetPosition;
            fromCurrent.y = 0f;

            if (fromCurrent.sqrMagnitude < 1f)
                continue;

            bool found = NavMesh.CalculatePath(
                startHit.position,
                targetHit.position,
                filter,
                navigationPath
            );

            if (!found ||
                navigationPath.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            destination = targetHit.position;
            return true;
        }

        return false;
    }

    public bool HasReachedDestination(Vector3 destination)
    {
        Vector3 difference = destination - GetFeetPosition();

        if (Mathf.Abs(difference.y) > 0.5f)
            return false;

        difference.y = 0f;
        return difference.sqrMagnitude <= 0.2f * 0.2f;
    }

    public bool CanHitPlayer()
    {
        if (player == null || GetDistanceToPlayer() > attackRange)
            return false;

        Vector3 start = controller.bounds.center;
        Collider playerCollider = player.GetComponent<Collider>();

        Vector3 end = playerCollider != null
            ? playerCollider.bounds.center
            : player.position + Vector3.up * 0.5f;

        bool blocked = Physics.Linecast(
            start,
            end,
            obstacleLayer,
            QueryTriggerInteraction.Ignore
        );

        return !blocked;
    }

    public void Attack()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (hp <= 0 || player == null)
            return;

        if (stateMachine.CurrentState != attackState ||
            !attackState.IsAttacking)
        {
            return;
        }

        if (!CanHitPlayer())
            return;

        if (player.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(attackDamage, gameObject);
        }
    }

    public void TakeDamage(int damage, GameObject attacker)
    {
        if (stateMachine.CurrentState == deadState)
            return;

        hp = Mathf.Max(0, hp - damage);

        Debug.Log($"{name} 피격! 남은 HP : {hp}");

        OnHealthChanged?.Invoke(hp, maxHp);

        if (hp <= 0)
        {
            stateMachine.ChangeState(deadState);
            return;
        }

        ResetAttackCooldown();
        stateMachine.ChangeState(hitState);
    }

    public void FinishAttack()
    {
        if (stateMachine.CurrentState != attackState)
            return;

        attackState.FinishAttack();
    }

    public void ResetAttackCooldown()
    {
        lastAttackTime = Time.time;
    }

    public void FinishDeath()
    {
        if (stateMachine.CurrentState != deadState ||
            victoryProcessed)
        {
            return;
        }

        victoryProcessed = true;
        GameManager.Instance.Victory();
    }
}