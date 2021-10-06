using UnityEngine;

public class NetworkSettingsButtons : MonoBehaviour
{
    [SerializeField]
    private Transform network;


    public void OnNetworkScaleSliderChanged(float value)
    {
        network.localScale = new Vector3(value, value, value);
    }

}
