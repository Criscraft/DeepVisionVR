using System.IO;
using UnityEngine;


public class InputManagerFPSInteraction : MonoBehaviour
{

    private PlayerControls controls;
    private PlayerControls.InteractActions interaction;

    [SerializeField]
    private string screenshotPath;
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

        screenshotPath = Path.Combine(new string[] { Application.dataPath, "..", "..", "Screenshots" });
        if (!Directory.Exists(screenshotPath))
        {
            Directory.CreateDirectory(screenshotPath);
        }

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
        string screenshotPathFinal = Path.Combine(new string[] { screenshotPath, string.Format("{0}.png", screenshotCounter) });
        ScreenCapture.CaptureScreenshot(screenshotPathFinal);
        screenshotCounter++;
    }
}
