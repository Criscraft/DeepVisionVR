using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class NetworkImageInputFrame : MonoBehaviour
{
    private DLManager _dlManager;
    //private bool hasLoadedItem = false;
    [SerializeField]
    private ImageGetterButton imageGetterButton;
    [SerializeField]
    private Canvas canvas;


    private void Start()
    {
        canvas.worldCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    public void Prepare(DLManager dlManager)
    {
        _dlManager = dlManager;
    }

    public void LoadImageFromImageGetterButton()
    {
        ActivationImage activationImage  = imageGetterButton.ActivationImageUsed;
        //Debug.Log(activationImage);
        _dlManager.RequestPrepareForInput(activationImage);
    }
}
