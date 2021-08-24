using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class InteractionGun : MonoBehaviour
{
    [SerializeField]
    private XRGrabInteractable xrInteractable;
    [SerializeField]
    private Transform raycastTransform;
    [SerializeField]
    private float range = 10f;
    [SerializeField]
    private LayerMask layerMask;

    private bool hasLoadedItem = false;
    [SerializeField]
    private GameObject holoImage;
    [SerializeField]
    private Renderer holoRenderer;
    [SerializeField]
    private TextMeshPro textMeshPro;
    
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
        
        RaycastHit raycastHit;
        if (Physics.Raycast(raycastTransform.transform.position, raycastTransform.transform.forward, out raycastHit, range, layerMask))
        {
            Debug.Log(raycastHit.transform.name);
            
            if (hasLoadedItem)
            {
                NetworkImageInputFrame imageFrame = raycastHit.transform.GetComponent<NetworkImageInputFrame>();
                if (imageFrame != null)
                {
                    imageFrame.PlaceImage(imageId, tex, className);
                    hasLoadedItem = false;
                    holoImage.SetActive(false);
                    return;
                }
            }
            
            RawImage rawImage = raycastHit.transform.GetComponentInChildren<RawImage>();
            if (rawImage != null)
            {
                LoadImage(-1, (Texture2D)rawImage.texture, -1, "");
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