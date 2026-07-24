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

    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1.2f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float attackDuration = 0.5f;

    private bool isAttacking;

    [SerializeField] private float dodgeSpeed = 12f;
    [SerializeField] private float dodgeDuration = 0.25f;
    [SerializeField] private float dodgeCooldown = 1f;

    [SerializeField] private int hp = 100;

    private bool isDodging;
    private bool canDodge = true;
    private Vector3 dodgeDir;
    private float dodgeTimer;
    private bool isInvincible;

    private Vector3 currentMoveDir;

    private Animator animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
        animator = GetComponentInChildren<Animator>();
        mainCam = Camera.main;
    }
    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Attack.performed += OnAttack;
        inputActions.Player.Dodge.performed += OnDodge;
    }
    private void OnDisable()
    {
        inputActions.Player.Attack.performed -= OnAttack;
        inputActions.Player.Dodge.performed -= OnDodge;
        inputActions.Disable();
    }
    private void Update()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight = mainCam.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        animator.SetFloat("Speed", currentMoveDir.magnitude);

        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;
        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }
        currentMoveDir = moveDir;
        
        Vector3 localMove = transform.InverseTransformDirection(moveDir);
        animator.SetFloat("MoveX", localMove.x);
        animator.SetFloat("MoveY", localMove.z);
        HandleGravity();
        if(!isDodging)
        {
            HandleMouseRotation();
        }
        if(isDodging)
        {
            HandleDodge();
        }
        else if(isAttacking)
        {
            Move(Vector3.zero)
;        }
        else
        {
            Move(moveDir);
        }
        
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;
    }
    private void HandleMouseRotation()
    {
        Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 lookDir = hit.point - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude < 0.01f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDir);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
    private void Move(Vector3 moveDir)
    {
        Vector3 motion = moveDir * moveSpeed;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }
    private void OnAttack(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (isAttacking || isDodging)
        {
            return;
        }

        isAttacking = true;
        Debug.Log("Attack");
        animator.SetTrigger("Attack");
        Invoke(nameof(EndAttack), attackDuration);
    }
    private void EndAttack()
    {
        isAttacking = false;
    }

    public void CheckAttackHit()
    {
        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position,
            attackRadius,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            if(hit.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(10);
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }

    private void OnDodge(InputAction.CallbackContext context)
    {
        if(isDodging || !canDodge || isAttacking)
        {
            return;
        }
        isDodging = true;
        canDodge = false;
        isInvincible = true;
        dodgeTimer = dodgeDuration;
        
        dodgeDir = -transform.forward;
        animator.SetTrigger("Dodge");

    }

    private void HandleDodge()
    {
        dodgeTimer -= Time.deltaTime;

        Vector3 motion = dodgeDir * dodgeSpeed;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);

        if(dodgeTimer <= 0f)
        {
            EndDodge();
        }
    }

    private void EndDodge()
    {
        isDodging = false;
        isInvincible = false;
        Invoke(nameof(ResetDodge), dodgeCooldown);
    }
    private void ResetDodge()
    {
        canDodge = true;
    }

    public void TakeDamage(int amount)
    {
        if(isInvincible)
        {
            Debug.Log("회피 무적 - 데미지 무시");
            return;
        }
        hp -= amount;
        animator.SetTrigger("Hit");
        Debug.Log($"플레이어 피격  hp : {hp} ");
        if(hp <= 0)
        {
            Debug.Log("플레이어 사망");
        }
    }
}