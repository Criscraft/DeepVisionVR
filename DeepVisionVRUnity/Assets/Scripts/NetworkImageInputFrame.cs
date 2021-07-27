using UnityEngine;
using TMPro;

public class NetworkImageInputFrame : MonoBehaviour
{
    private DLManager _dlManager;
    //private bool hasLoadedItem = false;
    public GameObject holoImage;
    public Renderer holoRenderer;
    public TextMeshPro textMeshPro;


    private void Start()
    {
        holoImage.SetActive(false);
        textMeshPro.text = "";
    }

    public void Prepare(DLManager dlManager)
    {
        _dlManager = dlManager;
    }

    public void PlaceImage(int imageId, Texture2D tex, string className)
    {
        //hasLoadedItem = true;
        textMeshPro.text = className;
        holoImage.SetActive(true);
        holoRenderer.material.SetTexture("_MainTex", tex);
        _dlManager.RequestPrepareForInput(imageId);
    }
}
