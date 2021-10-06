using UnityEngine;
using UnityEngine.UI;

public class FeatureVisSettingsButtons : MonoBehaviour
{
    private DLWebClient dlClient;
    private int networkID = -1;
    [SerializeField]
    private Image generateButtonImage;
    [SerializeField]
    private Image loadButtonImage;
    [SerializeField]
    private GameObject deleteSureButton;
    [SerializeField]
    private Color colorActive;
    [SerializeField]
    private Color colorInactive;


    public void Prepare(DLWebClient _dlClient, int _networkID)
    {
        dlClient = _dlClient;
        networkID = _networkID;
        OnLoadButtonClick();
    }


    public void OnGenerateButtonClick()
    {
        dlClient.SetNetworkGenFeatVis(networkID);
        generateButtonImage.color = colorActive;
        loadButtonImage.color = colorInactive;
    }


    public void OnLoadButtonClick()
    {
        dlClient.SetNetworkLoadFeatVis(networkID);
        generateButtonImage.color = colorInactive;
        loadButtonImage.color = colorActive;
    }


    public void OnDeleteButtonClick()
    {
        if (deleteSureButton.activeSelf) deleteSureButton.SetActive(false);
        else deleteSureButton.SetActive(true);
    }


    public void OnDeleteSureButtonClick()
    {
        dlClient.SetNetworkDeleteFeatVis(networkID);
        deleteSureButton.SetActive(false);
    }

    public void OnGenerateAllButtonClick()
    {
        dlClient.RequestAllFeatureVisualizations(networkID);
    }

}
