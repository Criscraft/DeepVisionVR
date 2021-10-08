using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class Layer2D : NetLayer
{
    [SerializeField]
    private RectTransform featureMaps;
    [SerializeField]
    private GameObject channel2DPrefab;
    [SerializeField]
    private Material colormapMaterial;
    [SerializeField]
    private Material rgbMaterial;
    [SerializeField]
    private Transform info;
    [SerializeField]
    private RectTransform scrollbar;
    [SerializeField]
    private LayerSettingsButtons layerSettingsButtons;
    [SerializeField]
    private RectTransform infoRectTransform;
    [SerializeField]
    private GameObject linePlotterPrefab;
    private string exportPath;
    [SerializeField]
    private RectTransform reloadOverlay;
    [SerializeField]
    private RectTransform background;
    private DLNetwork dlNetwork;
    private int networkID;
    private int layerID;


    private LinePlotter weightHistogram;
    private GameObject weightHistogramGO;
    private LinePlotter activationHistogram;
    private GameObject activationHistogramGO;
    private bool rgb = false;


    public void Prepare(Vector3Int size, DLNetwork _dlNetwork,  int _networkID, int _layerID)
    {
        dlNetwork = _dlNetwork;
        networkID = _networkID;
        layerID = _layerID;

        layerSettingsButtons.Prepare(dlNetwork, layerID);

        transform.GetComponent<Canvas>().worldCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        GenerateFeatureMaps(size);

        // generate plots and disable them for later use
        GenerateWeightHistogram();
        GenerateActivationHistogram();

        // refresh layout so that the width and height of the info elements are set correctly
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoRectTransform);
        // disable the info elements until they are needed
        weightHistogramGO.SetActive(false);
        activationHistogramGO.SetActive(false);

        exportPath = Path.Combine(new string[] {Application.dataPath, "..", "..", "Export"});
    }


    private void GenerateFeatureMaps(Vector3Int size)
    {
        Material material = Instantiate(colormapMaterial);
        for (int i = 0; i < size[0]; i++)
        {
            Transform newChannel2DInstance = ((GameObject)Instantiate(channel2DPrefab)).transform;
            newChannel2DInstance.name = "channel2D " + string.Format("{0}", i);
            newChannel2DInstance.SetParent(featureMaps);
            newChannel2DInstance.localPosition = Vector3.zero;
            newChannel2DInstance.localRotation = Quaternion.identity;
            newChannel2DInstance.localScale = Vector3.one;
            ImageGetterButton imageGetterButton = newChannel2DInstance.GetComponent<ImageGetterButton>();
            imageGetterButton.MaterialUsed = material;
            //FeatureVisualizationButton featureVisualizationButton = newChannel2DInstance.GetComponent<FeatureVisualizationButton>();
            //featureVisualizationButton.Prepare(dlNetwork);
            items.Add(newChannel2DInstance.gameObject);
        }
        // refresh layout so that the dimensions of the featureMaps layer is up to date ( important for applying the network layout )
        LayoutRebuilder.ForceRebuildLayoutImmediate(featureMaps);
        LayoutRebuilder.ForceRebuildLayoutImmediate(featureMaps);
        rgb = false;
    }


    private void GenerateWeightHistogram()
    {
        RectTransform linePlotterInstance = ((GameObject)Instantiate(linePlotterPrefab)).GetComponent<RectTransform>();
        linePlotterInstance.name = "Weight Histogram";
        linePlotterInstance.SetParent(info);
        linePlotterInstance.localScale = Vector3.one;
        linePlotterInstance.sizeDelta = new Vector2(800, 600);
        weightHistogram = linePlotterInstance.GetComponentInChildren<LinePlotter>();
        weightHistogram.diagramType = LinePlotter.DiagramType.histogramPlot;
        weightHistogram.Prepare();
        weightHistogram.AddTitle("Weight Distribution");
        weightHistogram.AddXLabel("Value");
        weightHistogram.AddYLabel("Count");
        weightHistogram.axis.yAxisLocation = "left";
        weightHistogramGO = linePlotterInstance.gameObject;
    }


    private void GenerateActivationHistogram()
    {
        RectTransform linePlotterInstance = ((GameObject)Instantiate(linePlotterPrefab)).GetComponent<RectTransform>();
        linePlotterInstance.name = "Activation Histogram";
        linePlotterInstance.SetParent(info);
        linePlotterInstance.localScale = Vector3.one;
        linePlotterInstance.sizeDelta = new Vector2(800, 600);
        activationHistogram = linePlotterInstance.GetComponentInChildren<LinePlotter>();
        activationHistogram.diagramType = LinePlotter.DiagramType.histogramPlot;
        activationHistogram.Prepare();
        activationHistogram.AddTitle("Activation Distribution");
        activationHistogram.AddXLabel("Value");
        activationHistogram.AddYLabel("Count");
        activationHistogram.axis.yAxisLocation = "left";
        activationHistogramGO = linePlotterInstance.gameObject;
    }


    public override float GetWidth(bool local=false)
    {
        float width = featureMaps.sizeDelta.x * featureMaps.localScale.x;
        if (!local) width *= transform.localScale.x;
        return width;
    }


    public override void UpdateData(List<ActivationImage> activationImageList, float scale)
    {
        //RawImage image;
        Material material = null;
        bool isRGB = activationImageList[0].isRGB;
        float zeroValue = activationImageList[0].zeroValue;

        if (!isRGB)
        {
            material = Instantiate(colormapMaterial);
            material.SetFloat("_TransitionValue", zeroValue / 255f);
        }
        else
        {
            if (rgb == false) material = Instantiate(rgbMaterial);
        }

        ImageGetterButton imageGetterButton;
        for (int i = 0; i < items.Count; i++)
        {
            imageGetterButton = items[i].GetComponent<ImageGetterButton>();
            imageGetterButton.ActivationImageUsed = activationImageList[i];
            imageGetterButton.MaterialUsed = material;
        }
        rgb = isRGB;

        DisableReloadOverlay();
    }


    public void SetWeightHistogramData(float[] weightCounts, float[] weightBins)
    {
        weightHistogramGO.gameObject.SetActive(true);
        weightHistogram.ClearData();
        LinePlotter.DataItem dataItem;
        dataItem.xData = weightBins;
        dataItem.yData = weightCounts;
        weightHistogram.AddData(dataItem);
        weightHistogram.Draw();
    }


    public void SetActivationHistogramData(float[] weightCounts, float[] weightBins)
    {
        activationHistogramGO.gameObject.SetActive(true);
        activationHistogram.ClearData();
        LinePlotter.DataItem dataItem;
        dataItem.xData = weightBins;
        dataItem.yData = weightCounts;
        activationHistogram.AddData(dataItem);
        activationHistogram.Draw();
    }


    public override void ApplyScale(float newScale)
    {
        float scale = featureMaps.localScale.x * newScale;
        featureMaps.localScale = new Vector3(scale, scale, scale);
        LayoutRebuilder.ForceRebuildLayoutImmediate(featureMaps);
        ScaleReloadOverlay();
        ScaleSlider();
        ScaleBackground();
    }


    private void ScaleReloadOverlay()
    {
        float layerWidth = GetWidth(true);
        float width = reloadOverlay.sizeDelta.x * reloadOverlay.localScale.x;
        float targetWidth = 0.5f * layerWidth;
        float scale = targetWidth / width * reloadOverlay.localScale.x;
        reloadOverlay.localScale = new Vector3(scale, scale, scale);
    }


    private void ScaleBackground()
    {
        float layerWidth = GetWidth(true);
        float width = background.sizeDelta.x * background.localScale.x;
        float targetWidth = layerWidth;
        float scale = targetWidth / width * background.localScale.x;
        background.localScale = new Vector3(scale, scale, scale);
    }


    private void ScaleSlider()
    {
        float layerHeight = GetWidth(true);
        float scaleFactor = (float)layerHeight / (float)scrollbar.sizeDelta.y;

        int newWidth = (int)(scrollbar.sizeDelta.x * scaleFactor);
        scrollbar.sizeDelta = new Vector2(newWidth, layerHeight);
    }


    public void ExportLayer()
    {
        ActivationImage activationImage;
        ActivationImage.Mode mode;
        ImageGetterButton imageGetterButton;
        Texture2D texture;
        byte[] bytes;
        string exportPathFinal;

        // create export path
        imageGetterButton = items[0].GetComponent<ImageGetterButton>();
        activationImage = imageGetterButton.ActivationImageUsed;
        mode = activationImage.mode;
        exportPathFinal = Path.Combine(new string[] {exportPath, string.Format("network{0}", networkID), mode.ToString(), string.Format("layer{0}", layerID)});
        if (!Directory.Exists(exportPathFinal))
        {
            Directory.CreateDirectory(exportPathFinal);
        }

        for (int i = 0; i < items.Count; i++)
        {
            imageGetterButton = items[i].GetComponent<ImageGetterButton>();
            activationImage = imageGetterButton.ActivationImageUsed;
            texture = activationImage.tex as Texture2D;
            // Encode texture into PNG
            bytes = texture.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(new string[] {exportPathFinal, string.Format("{0}.png", i) }), bytes);
        }
    }


    private void DisableReloadOverlay()
    {
        reloadOverlay.gameObject.SetActive(false);
    }

    public void EnableReloadOverlay()
    {
        reloadOverlay.gameObject.SetActive(true);
    }
}