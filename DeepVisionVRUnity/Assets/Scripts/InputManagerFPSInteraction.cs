using UnityEngine;

public class InputManagerFPSInteraction : MonoBehaviour
{

    private PlayerControls controls;
    private PlayerControls.InteractActions interaction;

    [SerializeField]
    private string screenshotPath = "Screenshots";
    private int screenshotCounter = 1;


    // Singleton Pattern
    private static InputManagerFPSInteraction _instance = null;
    public static InputManagerFPSInteraction instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType<InputManagerFPSInteraction>();
            }
            return _instance;
        }
    }


    private void Awake()
    {
        controls = new PlayerControls();

        interaction = controls.Interact;
        interaction.Screenshot.performed += _ => ScreenShot();
    }


    public void RegisterInteractionGun(ImageSelector interactionGun)
    {
        interaction.Fire2.started += _ => interactionGun.LayHoloImageOverScreen();
        interaction.Fire2.canceled += _ => interactionGun.QuitLayHoloImageOverScreen();
    }


    public void UnRegisterInteractionGun(ImageSelector interactionGun)
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


    private void ScreenShot()
    {
        ScreenCapture.CaptureScreenshot(screenshotPath + "\\screenshot" + string.Format("{0}.jpg", screenshotCounter));
        screenshotCounter++;
    }
}
