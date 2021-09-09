using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoiseGenerateButton : MonoBehaviour
{
    [SerializeField]
    private ImageGetterButton imageGetterButton;
    [SerializeField]
    private DLWebClient dlClient;


    public void GenerateImage()
    {
        dlClient.RequestNoiseImage();
    }


    public IEnumerator AcceptImage(Texture2D tex)
    {
        ActivationImage activationImage = new ActivationImage();
        activationImage.isRGB = true;
        activationImage.mode = ActivationImage.Mode.NoiseImage;
        activationImage.tex = tex;
        activationImage.nDim = 2;

        imageGetterButton.ActivationImageUsed = activationImage;
        yield return null;
    }


}
