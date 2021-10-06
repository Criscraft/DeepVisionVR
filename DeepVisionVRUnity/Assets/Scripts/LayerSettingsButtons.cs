using UnityEngine;
using UnityEngine.UI;

public class LayerSettingsButtons : MonoBehaviour
{
    private DLNetwork dLNetwork;
    private int layerID = -1;


    public void Prepare(DLNetwork _dLNetwork, int _layerID)
    {
        layerID = _layerID;
        dLNetwork = _dLNetwork;
    }


    public void OnGenFeatVisButtonClick()
    {
        dLNetwork.RequestLayerFeatureVisualization(layerID);
        dLNetwork.SetLoading(layerID);
    }


    public void OnExportButtonClick()
    {
        dLNetwork.RequestLayerExport(layerID);
    }

}