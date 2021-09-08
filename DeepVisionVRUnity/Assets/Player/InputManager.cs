using UnityEngine;

public class InputManager : MonoBehaviour
{

    [SerializeField]
    private Movement movement;
    [SerializeField]
    private MouseLook mouseLook;

    private PlayerControls controls;
    private PlayerControls.GroundMovementActions groundMovement;
    private PlayerControls.InteractActions interaction;

    private Vector2 horizontalInput;
    private Vector2 mouseInput;

    [SerializeField]
    private string screenshotPath = "Screenshots";
    private int screenshotCounter = 1;


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
        interaction.Screenshot.performed += _ => ScreenShot();
    }


    public void RegisterInteractionGun(InteractionGun interactionGun)
    {
        interaction.Fire2.started += _ => interactionGun.LayHoloImageOverScreen();
        interaction.Fire2.canceled += _ => interactionGun.QuitLayHoloImageOverScreen();
    }


    public void UnRegisterInteractionGun(InteractionGun interactionGun)
    {
        interaction.Fire2.started -= _ => interactionGun.LayHoloImageOverScreen();
        interaction.Fire2.canceled -= _ => interactionGun.QuitLayHoloImageOverScreen();
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

    private void ScreenShot()
    {
        ScreenCapture.CaptureScreenshot(screenshotPath + "\\screenshot" + string.Format("{0}.jpg", screenshotCounter));
        screenshotCounter++;
    }
}
