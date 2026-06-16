using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Camera mainCam;
    [SerializeField]
    private float moveSpeed = 5f;
    [SerializeField]
    private float gravity = -20f;
    [SerializeField]
    private float rotationSpeed = 12f;
    private float verticalVelocity;

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
        inputActions.Disable();
        inputActions.Player.Attack.performed -= OnAttack;
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


        float forwardInput = Mathf.Max(0f, moveInput.y); 
        Vector3 faceDir = camForward * forwardInput + camRight * moveInput.x;

        HandleGravity();
        HandleRotation(faceDir);
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
    private void HandleRotation(Vector3 faceDir)
    {
        if (faceDir.sqrMagnitude < 0.01f)
        {
            return;
        }
        Quaternion targetRotation = Quaternion.LookRotation(faceDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
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
        Invoke(nameof(EndAttack), 0.5f);
    }
    private void EndAttack()
    {
        isAttacking = false;
    }
}