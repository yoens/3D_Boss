using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IDamageable
{
    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Camera mainCam;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rotationSpeed = 12f;
    private float verticalVelocity;

    [SerializeField] private float hitLockDuration = 1.83f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1.2f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackDuration = 0.5f;
    [SerializeField] private float parryDuration = 0.2f;
    [SerializeField] private float parryCooldown = 0.8f;
    [SerializeField] private int parryDamage = 10;
    private bool isAttacking;
    private readonly HashSet<IDamageable> attackTargets = new HashSet<IDamageable>();

    [Header("Dodge")]
    [SerializeField] private float dodgeSpeed = 10f;
    [SerializeField, Min(0.05f)] private float dodgeDuration = 0.6f;   // 구르기 애니메이션 길이에 맞출 것
    [SerializeField] private float dodgeCooldown = 0.5f;
    // 구르기 진행도(0~1)에 따른 속도 배율: 초반엔 빠르고 끝에서 감속
    [SerializeField] private AnimationCurve dodgeSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.6f, 1f), new Keyframe(1f, 0.2f));
    // 무적 구간 (구르기 진행도 기준, 0~1)
    [SerializeField, Range(0f, 1f)] private float iFrameStart = 0.05f;
    [SerializeField, Range(0f, 1f)] private float iFrameEnd = 0.6f;
    [SerializeField] private int hp = 100;
    [SerializeField] private int maxHp = 100;

    public int Hp => hp;
    public int MaxHp => maxHp;
    public event Action<int, int> OnHealthChanged;

    private float hitLockUntil = -1f;
    private bool IsHitStunned => Time.time < hitLockUntil;
    private float nextAttackTime;
    private bool isDodging;
    private bool canDodge = true;
    private Vector3 dodgeDir;
    private float dodgeTimer;
    private bool isInvincible;
    private bool isParrying;
    private bool canParry = true;
    private Vector3 currentMoveDir;
    private Animator animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
        animator = GetComponentInChildren<Animator>();
        mainCam = Camera.main;
        hp = maxHp;
    }

    private void OnEnable()
    {
        inputActions.Player.Attack.performed += OnAttack;
        inputActions.Player.Dodge.performed += OnDodge;
        inputActions.Player.Parry.performed += OnParry;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Attack.performed -= OnAttack;
        inputActions.Player.Dodge.performed -= OnDodge;
        inputActions.Player.Parry.performed -= OnParry;
        inputActions.Disable();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (IsHitStunned)
        {
            currentMoveDir = Vector3.zero;
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            HandleGravity();
            Move(Vector3.zero);
            return;
        }

        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        Vector3 moveDir = GetCameraRelativeDirection(moveInput);

        currentMoveDir = moveDir;
        animator.SetFloat("Speed", currentMoveDir.magnitude);
        Vector3 localMove = transform.InverseTransformDirection(moveDir);
        animator.SetFloat("MoveX", localMove.x);
        animator.SetFloat("MoveY", localMove.z);

        HandleGravity();
        if (!isDodging)
            HandleMouseRotation();

        if (isDodging)
            HandleDodge();
        else if (isAttacking)
            Move(Vector3.zero);
        else
            Move(moveDir);
    }

    // 입력(WASD)을 카메라 기준 월드 방향으로 변환
    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight = mainCam.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 dir = camForward * input.y + camRight * input.x;
        if (dir.sqrMagnitude > 1f)
            dir.Normalize();
        return dir;
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleMouseRotation()
    {
        if (Mouse.current == null)
            return;

        Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            return;

        Vector3 lookDir = hit.point - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
        float adjustedRotationSpeed = rotationSpeed * GameSettings.MouseSensitivity;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation,
            adjustedRotationSpeed * Time.deltaTime);
    }

    private void Move(Vector3 moveDir)
    {
        Vector3 motion = moveDir * moveSpeed;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing || hp <= 0)
            return;

        if (isAttacking || isDodging || isParrying || IsHitStunned || Time.time < nextAttackTime)
            return;

        isAttacking = true;
        attackTargets.Clear();
        animator.SetTrigger("Attack");
    }

    public void EndAttack()
    {
        if (!isAttacking)
            return;

        isAttacking = false;
        nextAttackTime = Time.time + attackDuration;
    }

    public void CheckAttackHit()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing || hp <= 0 ||
            !isAttacking || IsHitStunned || attackPoint == null)
            return;

        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position, attackRadius, enemyLayer,
            QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target != null && !ReferenceEquals(target, this) && attackTargets.Add(target))
                target.TakeDamage(10, gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }

    private void OnDodge(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing || hp <= 0)
            return;

        if (isDodging || !canDodge || isAttacking || isParrying || IsHitStunned)
            return;

        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        Vector2 localDir = GetDodgeLocalDirection(moveInput);

        // 애니메이션과 실제 이동 방향이 일치하도록, 스냅된 로컬 방향을 월드로 다시 변환
        dodgeDir = transform.TransformDirection(new Vector3(localDir.x, 0f, localDir.y));
        dodgeDir.y = 0f;
        dodgeDir.Normalize();

        isDodging = true;
        canDodge = false;
        isInvincible = false;   // 무적은 HandleDodge에서 iFrame 구간에만 켬
        dodgeTimer = 0f;

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Parry");
        animator.SetFloat("DodgeX", localDir.x);
        animator.SetFloat("DodgeY", localDir.y);
        animator.SetTrigger("Dodge");
    }

    // 입력이 없으면 앞구르기, 있으면 캐릭터 기준 앞/뒤/좌/우 중 가장 가까운 방향
    // 반환값: (x = 좌우 -1/0/1, y = 앞뒤 -1/0/1)
    private Vector2 GetDodgeLocalDirection(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude < 0.01f)
            return Vector2.up;

        Vector3 worldDir = GetCameraRelativeDirection(moveInput);
        Vector3 local = transform.InverseTransformDirection(worldDir);

        if (Mathf.Abs(local.x) > Mathf.Abs(local.z))
            return new Vector2(Mathf.Sign(local.x), 0f);

        return new Vector2(0f, Mathf.Sign(local.z));
    }

    private void HandleDodge()
    {
        dodgeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(dodgeTimer / dodgeDuration);

        isInvincible = t >= iFrameStart && t <= iFrameEnd;

        float speed = dodgeSpeed * dodgeSpeedCurve.Evaluate(t);
        Vector3 motion = dodgeDir * speed;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);

        if (t >= 1f)
            EndDodge();
    }

    private void EndDodge()
    {
        if (!isDodging)
            return;

        isDodging = false;
        isInvincible = false;
        CancelInvoke(nameof(ResetDodge));
        Invoke(nameof(ResetDodge), dodgeCooldown);
    }

    private void ResetDodge()
    {
        canDodge = true;
    }

    public void TakeDamage(int amount, GameObject attacker)
    {
        TakeDamage(amount, attacker, true);
    }

    public void TakeDamage(int amount, GameObject attacker, bool canBeParried)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing ||
            hp <= 0 || isInvincible || amount <= 0)
            return;

        if (canBeParried && isParrying)
        {
            EndParry();
            if (attacker != null)
            {
                EnemyController targetEnemy = attacker.GetComponentInParent<EnemyController>();
                if (targetEnemy != null)
                    targetEnemy.TakeParryDamage(parryDamage, gameObject);
                else if (attacker.TryGetComponent<IDamageable>(out var target))
                    target.TakeDamage(parryDamage, gameObject);
            }
            return;
        }

        if (isParrying)
            EndParry();

        // 무적 구간 밖(구르기 끝자락)에 맞으면 구르기 취소
        if (isDodging)
            EndDodge();

        if (isAttacking)
        {
            isAttacking = false;
            nextAttackTime = Time.time + attackDuration;
        }

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Parry");
        hitLockUntil = Time.time + hitLockDuration;
        hp = Mathf.Max(0, hp - amount);
        animator.SetTrigger("Hit");
        OnHealthChanged?.Invoke(hp, maxHp);

        if (hp <= 0)
            GameManager.Instance.Defeat();
    }

    private void OnParry(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing || hp <= 0)
            return;

        if (isParrying || !canParry || isAttacking || isDodging || IsHitStunned)
            return;

        isParrying = true;
        canParry = false;
        animator.SetTrigger("Parry");
        Invoke(nameof(EndParry), parryDuration);
    }

    private void EndParry()
    {
        if (!isParrying)
            return;

        CancelInvoke(nameof(EndParry));
        isParrying = false;
        Invoke(nameof(ResetParry), parryCooldown);
    }

    private void ResetParry()
    {
        canParry = true;
    }
}