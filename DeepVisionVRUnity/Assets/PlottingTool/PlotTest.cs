using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlotTest : MonoBehaviour
{
    [SerializeField]
    private LinePlotter plot;
    [SerializeField]
    private bool update;

    void Update()
    {
        if (update)
        {
            plot.diagramType = LinePlotter.DiagramType.histogramPlot;
            plot.Prepare();
            LinePlotter.DataItem dataItem;
            dataItem.xData = new float[8] { 10.74f + 1e-4f * 0f, 10.74f + 1e-4f * 1f, 10.74f + 1e-4f * 2f, 10.74f + 1e-4f * 3f, 10.74f + 1e-4f * 4f, 10.74f + 1e-4f * 5f, 10.74f + 1e-4f * 6f, 10.74f + 1e-4f * 7f };
            dataItem.yData = new float[8] { 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f };
            plot.AddData(dataItem);
            plot.Draw();
            update = false;
        }
    }
}
