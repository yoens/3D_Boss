using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private PlayerInputActions inputActions;

    [SerializeField]
    private float moveSpeed = 5f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        Vector2 moveInput =
            inputActions.Player.Move.ReadValue<Vector2>();

        Vector3 moveDir =
            new Vector3(moveInput.x, 0, moveInput.y);

        controller.Move(
            moveDir * moveSpeed * Time.deltaTime);
    }
}