using System.Collections;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class NoiseGenerateButton : MonoBehaviour
{
    [SerializeField]
    private ImageGetterButton imageGetterButton;
    private DLWebClient dlClient;
    private int noiseGeneratorID = -1;


    public void Prepare(DLWebClient _dlClient, int _noiseGeneratorID)
    {
        dlClient = _dlClient;
        noiseGeneratorID = _noiseGeneratorID;
    }


    public void RequestNoiseImage()
    {
        dlClient.RequestNoiseImage(AcceptNoiseImage, noiseGeneratorID);
    }
    
    
    public IEnumerator AcceptNoiseImage(JObject jObject)
    {
        Texture2D tex = DLWebClient.StringToTex((string)jObject["tensor"]);
        
        ActivationImage activationImage = new ActivationImage();
        activationImage.noiseGeneratorID = noiseGeneratorID;
        activationImage.isRGB = true;
        activationImage.mode = ActivationImage.Mode.NoiseImage;
        activationImage.tex = tex;
        activationImage.nDim = 2;

        imageGetterButton.ActivationImageUsed = activationImage;
        yield return null;
    }
}
