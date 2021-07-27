using UnityEngine;

public class InputManager : MonoBehaviour
{
    public Movement movement;
    public MouseLook mouseLook;

    private PlayerControls controls;
    private PlayerControls.GroundMovementActions groundMovement;
    private PlayerControls.InteractActions interaction;

    private Vector2 horizontalInput;
    private Vector2 mouseInput;


    private void Awake()
    {
        controls = new PlayerControls();

        groundMovement = controls.GroundMovement;
        groundMovement.HorizontalMovement.performed += ctx => horizontalInput = ctx.ReadValue<Vector2>();
        groundMovement.Jump.performed += _ => movement.OnJumpPressed();
        groundMovement.Sprint.performed += _ => movement.OnSprintPressed();
        groundMovement.MouseX.performed += ctx => mouseInput.x = ctx.ReadValue<float>();
        groundMovement.MouseY.performed += ctx => mouseInput.y = ctx.ReadValue<float>();

        interaction = controls.Interact;
    }


    void OnEnable()
    {
        controls.Enable();
    }


    void OnDisable()
    {
        controls.Disable();
    }


    private void Update()
    {
        movement.ReceiveHorizontalInput(horizontalInput);
        mouseLook.ReceiveInput(mouseInput);
    }
}
