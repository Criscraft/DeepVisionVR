using UnityEngine;

public class LayerSettingsButtons : MonoBehaviour
{
    private DLNetwork dLNetwork;
    [SerializeField]
    private Layer2D layer2D;
    private int layerID = -1;


    public void Prepare(DLNetwork _dLNetwork, int _layerID)
    {
        dLNetwork = _dLNetwork;
        layerID = _layerID;
    }


    public void OnGenFeatVisButtonClick()
    {
        dLNetwork.RequestLayerFeatureVisualization(layerID);
    }


    public void OnExportButtonClick()
    {
        layer2D.ExportLayer();
    }

}