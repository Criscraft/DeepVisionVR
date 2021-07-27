using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class InteractionGun : MonoBehaviour
{
    public XRGrabInteractable xrInteractable;
    public Transform raycastTransform;
    public float range = 10f;
    public LayerMask layerMask;

    private bool hasLoadedItem = false;
    public GameObject holoImage;
    public Renderer holoRenderer;
    public TextMeshPro textMeshPro;
    
    private int imageId = -1;
    private Texture2D tex = null;
    private string className = "";

    public void LoadImage(int _imageId, Texture2D _tex, int _label, string _className) {
        hasLoadedItem = true;
        textMeshPro.text = _className;
        holoRenderer.material.SetTexture("_MainTex", _tex);
        holoImage.SetActive(true);
        tex = _tex;
        imageId = _imageId;
        className = _className;
    }

    public void Interact()
    {
        if (hasLoadedItem)
        {
            RaycastHit raycastHit;
            if (Physics.Raycast(raycastTransform.transform.position, raycastTransform.transform.forward, out raycastHit, range, layerMask))
            {
                NetworkImageInputFrame imageFrame = raycastHit.transform.GetComponent<NetworkImageInputFrame>();
                if (imageFrame != null)
                {
                    imageFrame.PlaceImage(imageId, tex, className);
                    hasLoadedItem = false;
                    holoImage.SetActive(false);
                }
            }
        }
    }


    public void OnSelect()
    {
        
    }


    public void OnDeselect()
    {
        
    }
}