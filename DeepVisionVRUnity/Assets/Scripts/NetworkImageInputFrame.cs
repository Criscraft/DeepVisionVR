using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class NetworkImageInputFrame : MonoBehaviour
{
    private DLNetwork dlNetwork;
    //private bool hasLoadedItem = false;
    [SerializeField]
    private ImageGetterButton imageGetterButton;
    [SerializeField]
    private Canvas canvas;


    private void Start()
    {
        canvas.worldCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    public void Prepare(DLNetwork _dlNetwork)
    {
        dlNetwork = _dlNetwork;
    }

    public void LoadImageFromImageGetterButton()
    {
        ActivationImage activationImage  = imageGetterButton.ActivationImageUsed;
        dlNetwork.RequestPrepareForInput(activationImage);
    }
}
