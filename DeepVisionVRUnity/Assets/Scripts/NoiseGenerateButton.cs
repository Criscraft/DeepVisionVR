using System.Collections;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class NoiseGenerateButton : MonoBehaviour
{
    [SerializeField]
    private ImageGetterButton imageGetterButton;
    [SerializeField]
    private DLWebClient dlClient;


    public IEnumerator AcceptNoiseImage(JObject jObject)
    {
        Texture2D tex = DLManager.StringToTex((string)jObject["tensor"]);
        
        ActivationImage activationImage = new ActivationImage();
        activationImage.isRGB = true;
        activationImage.mode = ActivationImage.Mode.NoiseImage;
        activationImage.tex = tex;
        activationImage.nDim = 2;

        imageGetterButton.ActivationImageUsed = activationImage;
        yield return null;
    }


    public void RequestNoiseImage()
    {
        dlClient.RequestNoiseImage(AcceptNoiseImage);
    }


    


}
