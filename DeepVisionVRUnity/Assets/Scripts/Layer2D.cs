using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;


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
    private RectTransform infoRectTransform;
    [SerializeField]
    private GameObject linePlotterPrefab;
    [SerializeField]
    private RectTransform reloadOverlay;
    private DLManager dlManager;


    private LinePlotter weightHistogram;
    private GameObject weightHistogramGO;
    private LinePlotter activationHistogram;
    private GameObject activationHistogramGO;
    private bool rgb = false;


    public void Prepare(Vector3Int size, Camera mainCamera, XRBaseInteractor rightInteractor, XRBaseInteractor leftInteractor, DLManager _dlManager)
    {
        dlManager = _dlManager;

        transform.GetComponent<Canvas>().worldCamera = mainCamera;

        GenerateFeatureMaps(size, rightInteractor, leftInteractor);

        // generate plots and disable them for later use
        GenerateWeightHistogram();
        GenerateActivationHistogram();

        // refresh layout so that the width and height of the info elements are set correctly
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoRectTransform);
        // disable the info elements until they are needed
        weightHistogramGO.SetActive(false);
        activationHistogramGO.SetActive(false);
    }


    private void GenerateFeatureMaps(Vector3Int size, XRBaseInteractor rightInteractor, XRBaseInteractor leftInteractor)
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
            imageGetterButton.Prepare(rightInteractor, leftInteractor);
            imageGetterButton.MaterialUsed = material;
            FeatureVisualizationButton featureVisualizationButton = newChannel2DInstance.GetComponent<FeatureVisualizationButton>();
            featureVisualizationButton.Prepare(rightInteractor, leftInteractor, dlManager);
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
        Vector3[] fourCornersArray = new Vector3[4];
        featureMaps.GetLocalCorners(fourCornersArray);
        float width = Mathf.Abs(fourCornersArray[0].x - fourCornersArray[3].x) * featureMaps.localScale.x;
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
    }


    private void ScaleReloadOverlay()
    {
        float layerWidth = GetWidth(true);
        Vector3[] fourCornersArray = new Vector3[4];
        reloadOverlay.GetLocalCorners(fourCornersArray);
        float overlayWidth = Mathf.Abs(fourCornersArray[0].x - fourCornersArray[3].x) * reloadOverlay.localScale.x;
        float targetWidth = 0.5f * layerWidth;
        float scale = targetWidth / overlayWidth * reloadOverlay.localScale.x;
        reloadOverlay.localScale = new Vector3(scale, scale, scale);
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