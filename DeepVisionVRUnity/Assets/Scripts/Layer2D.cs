﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Layer2D : NetLayer
{
    [SerializeField]
    private RectTransform featureMaps;
    [SerializeField]
    private GameObject channel2DPrefab;
    [SerializeField]
    private Transform horizontalShift;
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

    private LinePlotter weightHistogram;
    private GameObject weightHistogramGO;
    private LinePlotter activationHistogram;
    private GameObject activationHistogramGO;
    private bool rgb = false;


    public void Prepare(Vector3Int size, Camera mainCamera)
    {
        transform.GetComponent<Canvas>().worldCamera = mainCamera;

        GenerateFeatureMaps(size);

        // generate plots and disable them for later use
        GenerateWeightHistogram();
        GenerateActivationHistogram();

        // refresh layout so that the width and height of the into elements are set correctly
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoRectTransform);
        // disable the info elements until they are needed
        weightHistogramGO.SetActive(false);
        activationHistogramGO.SetActive(false);
    }


    private void GenerateFeatureMaps(Vector3Int size)
    {
        for (int i = 0; i < size[0]; i++)
        {
            Transform newChannel2DInstance = ((GameObject)Instantiate(channel2DPrefab)).transform;
            newChannel2DInstance.name = "channel2D " + string.Format("{0}", i);
            newChannel2DInstance.SetParent(featureMaps);
            newChannel2DInstance.localPosition = Vector3.zero;
            newChannel2DInstance.localRotation = Quaternion.identity;
            newChannel2DInstance.localScale = Vector3.one;
            var image = newChannel2DInstance.GetComponent<RawImage>();
            image.material = Instantiate(colormapMaterial);
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


    public override void UpdateData(List<Texture2D> textureList, float scale, bool isRGB, float zeroValue=0f)
    {
        RawImage image;

        for (int i = 0; i < items.Count; i++)
        {
            image = items[i].GetComponent<RawImage>();
            image.texture = textureList[i];
            if (! isRGB ) 
            {
                if (rgb == true) image.material = Instantiate(colormapMaterial);
                image.material.SetFloat("_TransitionValue", zeroValue / 255f);
            }
            else 
            {
                if (rgb == false) image.material = Instantiate(rgbMaterial);
            }
            image.material.SetTexture("_MainTex", textureList[i]);
            //image.SetNativeSize(); // pixel perfect
        }
        rgb = isRGB;
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
        Center();
    }


    public void Center()
    {
        horizontalShift.localPosition = new Vector3(- 0.5f * GetWidth(true), 0f, 0f);
    }
}