using UnityEngine;

public struct ActivationImage
{
    public enum Mode
    {
        Unknown,
        DatasetImage,
        Activation,
        FeatureVisualization
    }

    public ActivationImage(
        int _imageID = -1,
        int _layerID = -1,
        int _channelID = -1,
        string _className = "",
        Mode _mode = Mode.Unknown,
        bool _isRGB = false,
        float _zeroValue = 0f,
        int _nDim = -1,
        Texture _tex = null)
    {
        imageID = _imageID;
        layerID = _layerID;
        channelID = _channelID;
        className = _className;
        mode = _mode;
        isRGB = _isRGB;
        zeroValue = _zeroValue;
        nDim = _nDim;
        tex = _tex;
    }

    public int imageID;
    public int layerID;
    public int channelID;
    public string className;
    public Mode mode;
    public bool isRGB;
    public float zeroValue;
    public int nDim;
    public Texture tex;
}