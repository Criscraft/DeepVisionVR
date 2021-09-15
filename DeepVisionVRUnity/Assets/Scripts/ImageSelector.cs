using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ImageSelector : MonoBehaviour
{
    [SerializeField]
    private GameObject holoImageGo;
    [SerializeField]
    private Renderer holoImageRenderer;
    [SerializeField]
    private TextMeshPro textMeshPro;
    [SerializeField]
    private RawImage screenOverlay;
    [SerializeField]
    private Material colormapMaterial;
    [SerializeField]
    private Material rgbMaterial;

    private bool hasLoadedItem = false;

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

            if (value.tex != null)
            {
                holoImageGo.SetActive(true);
                hasLoadedItem = true;

                textMeshPro.text = value.className;
                Material material = null;
                if (value.isRGB)
                {
                    material = Instantiate(rgbMaterial);
                }
                else
                {
                    material = Instantiate(colormapMaterial);
                    material.SetFloat("_TransitionValue", value.zeroValue / 255f);
                }
                material.SetTexture("_MainTex", value.tex);
                holoImageRenderer.material = material;
            }
            
            else
            {
                holoImageGo.SetActive(false);
                hasLoadedItem = false;
            }
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


    public void LayHoloImageOverScreen()
    {
        if (hasLoadedItem && screenOverlay != null && gameObject.activeSelf)
        {
            screenOverlay.enabled = true;
            screenOverlay.texture = holoImageRenderer.material.GetTexture("_MainTex");
            screenOverlay.material = holoImageRenderer.material;
            Vector2 wh = Helpers.SizeToMatchAspectRatioInCanvas(screenOverlay.texture, screenOverlay.GetComponentInParent<RectTransform>());
            screenOverlay.rectTransform.localScale = new Vector3(wh.x, wh.y, 1f);
        }
    }


    public void QuitLayHoloImageOverScreen()
    {
        if (hasLoadedItem)
        {
            screenOverlay.enabled = false;
        }
    }
}