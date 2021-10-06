using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class InteractableController : MonoBehaviour
{

    // Singleton Pattern
    private static InteractableController _instance;
    public static InteractableController instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType<InteractableController>();
            }
            return _instance;
        }
    }

    [SerializeField]
    private InputActionProperty switchToolAction;
    [SerializeField]
    private XRBaseInteractor interactor;
    public XRBaseInteractor Interactor
    {
        get => interactor;
    }
    [SerializeField]
    private XRInteractionManager xRInteractionManager;
    [SerializeField]
    private Transform imageSelector;
    [SerializeField]
    private Transform featureVisualizer;
    private Transform[] interactables = new Transform[2];
    private int activeInteractableIdx = 2 - 1;


    private void Awake()
    {
        // decativated because the second tool has no unique function yet
        //switchToolAction.action.performed += _ => SwitchTool();
    }

    private void Start()
    {
        interactables[0] = imageSelector;
        interactables[1] = featureVisualizer;

        foreach (Transform tmpTransform in interactables)
        {
            tmpTransform.gameObject.SetActive(false);
        }

        SwitchTool();
    }


    private void SwitchTool()
    {
        int newActiveInteractableIdx = activeInteractableIdx + 1;
        if (newActiveInteractableIdx >= interactables.Length) newActiveInteractableIdx = 0;
        Equip(interactables[newActiveInteractableIdx], interactables[activeInteractableIdx]);
        activeInteractableIdx = newActiveInteractableIdx;
    }


    private void Equip(Transform interactableTransform, Transform oldInteractableTransform)
    {
        oldInteractableTransform.gameObject.SetActive(false);
        interactableTransform.gameObject.SetActive(true);
        xRInteractionManager.ForceSelect(interactor, interactableTransform.GetComponent<XRBaseInteractable>());

        if (InputManagerFPSInteraction.instance != null)
        {
            ImageSelector imageSelector = interactableTransform.GetComponent<ImageSelector>();
            if (imageSelector != null)
            {
                InputManagerFPSInteraction.instance.RegisterInteractionGun(imageSelector);
            }
            else
            {
                imageSelector = oldInteractableTransform.GetComponent<ImageSelector>();
                if (imageSelector != null)
                {
                    InputManagerFPSInteraction.instance.UnRegisterInteractionGun(imageSelector);
                }
            }
        }
    }

    /*public InputActionProperty SwitchToolAction
    {
        get => switchToolAction;
        set => SetInputActionProperty(ref switchToolAction, value);
    }


    void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value)
    {
        if (Application.isPlaying)
            property.DisableDirectAction();

        property = value;

        if (Application.isPlaying && isActiveAndEnabled)
            property.EnableDirectAction();
    }
    */



}
