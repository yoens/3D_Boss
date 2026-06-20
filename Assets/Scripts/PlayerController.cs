using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
        mainCam = Camera.main;
    }
    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Attack.performed += OnAttack;
    }
    private void OnDisable()
    {
        inputActions.Player.Attack.performed -= OnAttack;
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

        
        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;
        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }

        HandleGravity();
        HandleMouseRotation();
        Move(moveDir);
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
        if (isAttacking)
        {
            return;
        }

        isAttacking = true;
        Debug.Log("Attack");
        CheckAttackHit();
        Invoke(nameof(EndAttack), attackDuration);
    }
    private void EndAttack()
    {
        isAttacking = false;
    }

    private void CheckAttackHit()
    {
        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position,
            attackRadius,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            Debug.Log("Hit : " + hit.name);
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}