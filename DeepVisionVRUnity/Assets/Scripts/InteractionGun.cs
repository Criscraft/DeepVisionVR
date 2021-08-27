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
    private GameObject holoImageGo;
    [SerializeField]
    private Renderer holoImageRenderer;
    [SerializeField]
    private TextMeshPro textMeshPro;

    private ActivationImage activationImageUsed;
    public ActivationImage ActivationImageUsed
    {
        get
        {
            return this.activationImageUsed;
        }
        set
        {
            this.activationImageUsed = value;
            hasLoadedItem = true;
            textMeshPro.text = value.className;
            holoImageRenderer.material.SetTexture("_MainTex", value.tex);
            holoImageGo.SetActive(true);
        }
    }


    public void Interact()
    {
        /*
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
                LoadImage(-1, rawImage.texture, "");
            }
        }
        */
    }


    public void OnSelect()
    {
        
    }


    public void OnDeselect()
    {
        
    }
}