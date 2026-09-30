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
    private SpecialAttackState specialAttackState;
    private ChargeState chargeState;
    private BossChargeAttack chargeAttack;
    private EnemyHitFlash hitFlash;
    private CharacterSfx sfx;

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

    [SerializeField] private bool isBoss;
    public bool IsBoss => isBoss;
    public bool IsInCombat => isInCombat;
    public int BossPhase => phaseTwo ? 2 : 1;
    public float SpecialRecoveryDuration => specialRecoveryDuration;
    public float SpecialTimeout => specialTimeout;
    public event Action<bool> OnCombatStateChanged;

    [Header("Boss Phase")]
    [SerializeField, Range(0.01f, 0.99f)] private float phaseTwoHpRatio = 0.5f;

    [Header("Boss Special Attack")]
    [SerializeField, Min(0.1f)] private float specialRadius = 3f;
    [SerializeField, Min(0.1f)] private float specialHitHeight = 2f;
    [SerializeField, Min(1)] private int specialDamage = 25;
    [SerializeField, Min(0f)] private float specialCooldown = 8f;
    [SerializeField, Min(0f)] private float specialRecoveryDuration = 1f;
    [SerializeField, Min(1f)] private float specialTimeout = 8f;
    [SerializeField] private BossRangeIndicator specialIndicator;

    private bool isInCombat;
    private bool phaseTwo;
    private float nextSpecialTime;
    private Vector3 specialOrigin;

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
    public CharacterSfx Sfx => sfx;
    public int Hp => hp;
    public int MaxHp => maxHp;

    public IdleState IdleState => idleState;
    public ChaseState ChaseState => chaseState;
    public AttackState AttackState => attackState;
    public HitState HitState => hitState;
    public DeadState DeadState => deadState;

    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        hitFlash = GetComponent<EnemyHitFlash>();
        chargeAttack = GetComponent<BossChargeAttack>();

        stateMachine = new StateMachine();
        navigationPath = new NavMeshPath();

        idleState = new IdleState(this, stateMachine);
        chaseState = new ChaseState(this, stateMachine);
        attackState = new AttackState(this, stateMachine);
        hitState = new HitState(this, stateMachine);
        deadState = new DeadState(this, stateMachine);
        specialAttackState = new SpecialAttackState(this, stateMachine);
        chargeState = new ChargeState(this, stateMachine, chargeAttack);
        if (chargeAttack != null)
            chargeAttack.Initialize(this, controller, navigationSurface, obstacleLayer);
        sfx = GetComponentInChildren<CharacterSfx>();
        hp = maxHp;
    }

    private void Start()
    {
        stateMachine.ChangeState(idleState);

        if (isBoss && (specialIndicator == null || !specialIndicator.IsReady))
            Debug.LogWarning($"{name}: Special Indicator와 Indicator Material을 연결해야 특수 공격을 사용할 수 있습니다.");
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        moveDirection = Vector3.zero;

        UpdateBossPhase();
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

        if (stateMachine.CurrentState == chargeState &&
            chargeAttack != null && chargeAttack.IsDashing)
        {
            chargeAttack.ApplyDashMovement(verticalVelocity, Time.deltaTime);
            return;
        }

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

        if (!attackState.TryConsumeHit() || !CanHitPlayer())
            return;

        if (player.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(attackDamage, gameObject);
        }
    }

    public void TakeDamage(int damage, GameObject attacker)
    {
        ApplyDamage(damage, false);
    }

    public void TakeParryDamage(int damage, GameObject attacker)
    {
        ApplyDamage(damage, true);
    }

    private void ApplyDamage(int damage, bool stagger)
    {
        if (hp <= 0 || stateMachine.CurrentState == deadState || damage <= 0)
            return;

        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        hp = Mathf.Max(0, hp - damage);
        sfx?.PlayHit();
        if (isBoss && hitFlash != null)
            hitFlash.Play();
        OnHealthChanged?.Invoke(hp, maxHp);

        if (hp <= 0)
        {
            SetInCombat(false);
            stateMachine.ChangeState(deadState);
            return;
        }

        SetInCombat(true);

        if (isBoss && (!stagger || stateMachine.CurrentState == specialAttackState ||
            stateMachine.CurrentState == chargeState))
        {
            if (stateMachine.CurrentState == idleState)
                stateMachine.ChangeState(chaseState);
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
        if (stateMachine.CurrentState != deadState || victoryProcessed)
        {
            return;
        }

        victoryProcessed = true;
        SetInCombat(false);

        if (isBoss)
        {
            GameManager.Instance.Victory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetInCombat(bool value)
    {
        bool nextValue = value && isBoss && hp > 0 && isActiveAndEnabled;
        if (isInCombat == nextValue)
            return;

        isInCombat = nextValue;
        OnCombatStateChanged?.Invoke(isInCombat);
    }

    private void OnDisable()
    {
        SetInCombat(false);
        HideSpecialIndicator();
        if (chargeAttack != null)
            chargeAttack.Cancel();
    }

    private void UpdateBossPhase()
    {
        if (!isBoss || !isInCombat || phaseTwo || hp <= 0 || maxHp <= 0)
            return;

        if ((float)hp / maxHp > phaseTwoHpRatio)
            return;

        if (stateMachine.CurrentState == hitState ||
            stateMachine.CurrentState == specialAttackState ||
            stateMachine.CurrentState == chargeState ||
            attackState.IsAttacking)
            return;

        phaseTwo = true;
        Debug.Log($"{name}: 보스 2페이즈 시작");
    }

    public bool TryStartChargeAttack()
    {
        if (stateMachine.CurrentState != chaseState || chargeAttack == null ||
            !chargeAttack.CanStart())
            return false;

        stateMachine.ChangeState(chargeState);
        return true;
    }

    public bool TryStartSpecialAttack()
    {
        if (!isBoss || !phaseTwo || !isInCombat || hp <= 0 || player == null)
            return false;

        if (!CanAttack || Time.time < nextSpecialTime)
            return false;

        if (specialIndicator == null || !specialIndicator.IsReady)
            return false;

        if (!IsPlayerInSpecialArea(GetFeetPosition()))
            return false;

        if (stateMachine.CurrentState != chaseState &&
            stateMachine.CurrentState != attackState)
            return false;

        stateMachine.ChangeState(specialAttackState);
        return true;
    }

    public void BeginSpecialAttack()
    {
        Move(Vector3.zero);
        ClearNavigationPath();
        LookAtPlayer();
        specialOrigin = GetFeetPosition();
        specialIndicator.ShowWarning(specialOrigin, specialRadius);
        animator.SetFloat("Speed", 0f);
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hit");
        animator.SetBool("IsSpecialAttacking", true);
        animator.SetTrigger("SpecialAttack");
    }

    private bool IsPlayerInSpecialArea(Vector3 origin)
    {
        if (player == null)
            return false;

        Collider targetCollider = player.GetComponent<Collider>();
        Vector3 targetPoint = targetCollider != null
            ? targetCollider.bounds.center : player.position;
        Vector3 horizontal = targetPoint - origin;
        horizontal.y = 0f;

        if (horizontal.sqrMagnitude > specialRadius * specialRadius)
            return false;

        if (targetCollider != null)
        {
            if (targetCollider.bounds.max.y < origin.y - 0.1f ||
                targetCollider.bounds.min.y > origin.y + specialHitHeight)
                return false;
        }
        else if (Mathf.Abs(targetPoint.y - origin.y) > specialHitHeight)
        {
            return false;
        }

        return !Physics.Linecast(
            origin + Vector3.up * 0.5f,
            targetPoint,
            obstacleLayer,
            QueryTriggerInteraction.Ignore);
    }

    public void SpecialAttackHit()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing || hp <= 0)
            return;

        if (stateMachine.CurrentState != specialAttackState)
            return;

        specialAttackState.Hit();
    }

    public void ApplySpecialAttackDamage()
    {
        specialIndicator.ShowExplosion();

        if (!IsPlayerInSpecialArea(specialOrigin))
            return;

        if (player.TryGetComponent<PlayerController>(out var target))
            target.TakeDamage(specialDamage, gameObject, false);
    }

    public void FinishSpecialAttack()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing || hp <= 0)
            return;

        if (stateMachine.CurrentState == specialAttackState)
            specialAttackState.FinishAnimation();
    }

    public void EndSpecialAttack()
    {
        HideSpecialIndicator();
        animator.ResetTrigger("SpecialAttack");
        animator.SetBool("IsSpecialAttacking", false);
        nextSpecialTime = Time.time + specialCooldown;
        ResetAttackCooldown();
    }

    public void HideSpecialIndicator()
    {
        if (specialIndicator != null)
            specialIndicator.Hide();
    }
}
