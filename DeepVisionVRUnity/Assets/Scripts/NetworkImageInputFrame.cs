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


    public void Prepare(DLManager dlManager, Camera camera, XRBaseInteractor rightInteractor, XRBaseInteractor leftInteractor)
    {
        _dlManager = dlManager;
        canvas.worldCamera = camera;
        imageGetterButton.Prepare(rightInteractor, leftInteractor);
    }

    public void LoadImageFromImageGetterButton()
    {
        (int imageId, string className, Texture tex) = imageGetterButton.GetImage();
        _dlManager.RequestPrepareForInput(imageId);
    }
}
