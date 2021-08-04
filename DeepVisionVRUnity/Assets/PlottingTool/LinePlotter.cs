using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;


public class LinePlotter : MonoBehaviour
{
	public RectTransform plottingArea;
	public GameObject lineRendererPrefab;
	private List<Transform> lineRendererList;
	private List<DataItem> data;
	public Transform titelTextMesh;
	public Transform xLabel;
	public Transform yLabel;
	public GameObject xTickLabelPrefab;
	public GameObject yTickLabelPrefab;
	private List<Transform> xTicks;
	private List<Transform> yTicks;
	private Transform xAxis;
	private Transform yAxis;
	private List<Transform> xTickLabels;
	private List<Transform> yTickLabels;
	private Color[] colors = { Color.blue, Color.red, Color.green };
	public float plottingAreaMargin = 0.15f;


	void Start()
	{
		data = new List<DataItem>();
		lineRendererList = new List<Transform>();
		xTicks = new List<Transform>();
		yTicks = new List<Transform>();
		xTickLabels = new List<Transform>();
		yTickLabels = new List<Transform>();

		titelTextMesh.gameObject.SetActive(false);
		xLabel.gameObject.SetActive(false);
		yLabel.gameObject.SetActive(false);

		DataItem dataItem;
		dataItem.xData = new float[] { -1f, 0f, 1f, 2f, 3f, 4f };
		dataItem.yData = new float[] { 2f,  6f, 2f, 5f, 1f, 0f };
		AddData(dataItem);
		AddTitle("Awesome!");
		AddXLabel("Best x label");
		AddYLabel("Best y label");
		ShowGraph();
	}


	public void AddData(DataItem dataItem)
	{
		data.Add(dataItem);
	}


	public void AddTitle(string title)
	{
		titelTextMesh.GetComponent<TextMeshProUGUI>().text = title;
		titelTextMesh.gameObject.SetActive(true);
	}


	public void AddXLabel(string label)
	{
		xLabel.GetComponent<TextMeshProUGUI>().text = label;
		xLabel.gameObject.SetActive(true);
	}


	public void AddYLabel(string label)
	{
		yLabel.GetComponent<TextMeshProUGUI>().text = label;
		yLabel.gameObject.SetActive(true);
	}


	public void AddXTickLabels(float width, float height, Limits dataLimits, Limits plottingLimits)
	{
		Vector3[] tickLabelPoints;
		DataItem axisDataItem = new DataItem();

		// draw x axis
		axisDataItem.xData = new float[] { dataLimits.xMin, dataLimits.xMax };
		axisDataItem.yData = new float[] { 0f, 0f };
		tickLabelPoints = MapDataToPosition(axisDataItem, width, height, plottingLimits);
		for (int i = 0; i < tickLabelPoints.Length; i++)
		{
			Vector3 pos = tickLabelPoints[i];
			Transform tickInstance = ((GameObject)Instantiate(xTickLabelPrefab)).transform;
			tickInstance.name = "x tick";
			tickInstance.SetParent(plottingArea);
			tickInstance.localPosition = new Vector3(pos[0], pos[1], pos[2]);
			tickInstance.localRotation = Quaternion.Euler(Vector3.zero);
			tickInstance.localScale = Vector3.one;
			tickInstance.GetComponent<TextMeshProUGUI>().text = string.Format("{0}", (float)axisDataItem.xData[i]);
			xTickLabels.Add(tickInstance);


		}
	}


	public void AddYTickLabels(float width, float height, Limits dataLimits, Limits plottingLimits)
	{
		Vector3[] tickLabelPoints;
		DataItem tickLabelDataItem = new DataItem();

		// draw x axis
		tickLabelDataItem.xData = new float[] { 0f, 0f };
		tickLabelDataItem.yData = new float[] { dataLimits.yMin, dataLimits.yMax };
		tickLabelPoints = MapDataToPosition(tickLabelDataItem, width, height, plottingLimits);
		for (int i = 0; i < tickLabelPoints.Length; i++)
		{
			Vector3 pos = tickLabelPoints[i];
			Transform tickInstance = ((GameObject)Instantiate(yTickLabelPrefab)).transform;
			tickInstance.name = "y tick";
			tickInstance.SetParent(plottingArea);
			tickInstance.localPosition = new Vector3(pos[0], pos[1], pos[2]);
			tickInstance.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 90f));
			tickInstance.localScale = Vector3.one;
			tickInstance.GetComponent<TextMeshProUGUI>().text = string.Format("{0}", (float)tickLabelDataItem.yData[i]);
			yTickLabels.Add(tickInstance);
		}
	}


	public void ShowGraph()
	{
		ClearGraph();
		
		// find plotting area
		plottingArea.ForceUpdateRectTransforms();
		Rect rect = plottingArea.rect;
		float width = rect.width;
		float height = rect.height;

		// find minimal and maximal limits
		Limits dataLimits = new Limits();
		dataLimits.xMin = 0f;
		dataLimits.xMax = 0f;
		dataLimits.yMin = 0f;
		dataLimits.yMax = 0f;
		foreach (DataItem dataItem in data)
        {
			foreach (float x in dataItem.xData)
			{
				if (x < dataLimits.xMin) dataLimits.xMin = x;
				if (x > dataLimits.xMax) dataLimits.xMax = x;
			}
			foreach (float y in dataItem.yData)
			{
				if (y < dataLimits.yMin) dataLimits.yMin = y;
				if (y > dataLimits.yMax) dataLimits.yMax = y;
			}
		}


		// add margin to plotting limits
		Limits plottingLimits = new Limits();
		float deltaX = plottingAreaMargin * (dataLimits.xMax - dataLimits.xMin);
		float deltaY = plottingAreaMargin * (dataLimits.yMax - dataLimits.yMin);
		plottingLimits.xMin = dataLimits.xMin - deltaX;
		plottingLimits.xMax = dataLimits.xMax + deltaX;
		plottingLimits.yMin = dataLimits.yMin - deltaY;
		plottingLimits.yMax = dataLimits.yMax + deltaY;

		Vector3[] points;
		DataItem axisDataItem = new DataItem();

		// draw x axis
		axisDataItem.xData = new float[] { plottingLimits.xMin, plottingLimits.xMax };
		axisDataItem.yData = new float[] { 0f, 0f };
		points = MapDataToPosition(axisDataItem, width, height, plottingLimits);
		xAxis = drawLine(points, Color.white);
		// draw y axis
		axisDataItem.xData = new float[] { 0f, 0f };
		axisDataItem.yData = new float[] { plottingLimits.yMin, plottingLimits.yMax };
		points = MapDataToPosition(axisDataItem, width, height, plottingLimits);
		xAxis = drawLine(points, Color.white);

		// draw ticklabels
		AddXTickLabels(width, height, dataLimits, plottingLimits);
		AddYTickLabels(width, height, dataLimits, plottingLimits);

		// plot data
		for (int i = 0; i < data.Count; i++)
		{
			points = MapDataToPosition(data[i], width, height, plottingLimits);
			drawLine(points, colors[i % colors.Length]);
		}
	}


	public Vector3[] MapDataToPosition(DataItem dataItem, float width, float height, Limits plottingLimits)
	{
		int length = dataItem.xData.Length;
		Vector3[] points = new Vector3[length];

		for (int i = 0; i < length; i++)
		{
			points[i] = new Vector3((dataItem.xData[i] - plottingLimits.xMin) / (plottingLimits.xMax - plottingLimits.xMin) * width, (dataItem.yData[i] - plottingLimits.yMin) / (plottingLimits.yMax - plottingLimits.yMin) * height, 0f);
		}

		return points;
	}

	public Transform drawLine(Vector3[] points, Color color, float widthMultiplier=1f)
    {
		Transform lineInstance = ((GameObject)Instantiate(lineRendererPrefab)).transform;
		lineInstance.name = "GraphLine";
		lineInstance.SetParent(plottingArea);
		lineInstance.localPosition = Vector3.zero;
		lineInstance.localRotation = Quaternion.Euler(Vector3.zero);
		lineInstance.localScale = Vector3.one;
		LineRenderer lineRenderer = lineInstance.GetComponent<LineRenderer>();
		lineRenderer.material.color = color;
		lineRenderer.startWidth = lineRenderer.startWidth * widthMultiplier;
		lineRenderer.positionCount = points.Length;
		lineRenderer.SetPositions(points);
		return lineInstance;
	}


	public void ClearGraph()
	{
		var toDelete = new List<GameObject>();
		foreach (Transform item in lineRendererList) toDelete.Add(item.gameObject);
		foreach (Transform item in xTicks) toDelete.Add(item.gameObject);
		foreach (Transform item in yTicks) toDelete.Add(item.gameObject);
		foreach (Transform item in xTickLabels) toDelete.Add(item.gameObject);
		foreach (Transform item in yTickLabels) toDelete.Add(item.gameObject);
		if (xAxis != null) toDelete.Add(xAxis.gameObject);
		if (yAxis != null) toDelete.Add(yAxis.gameObject);
		toDelete.ForEach(item => GameObject.Destroy(item));
	}
}


public struct DataItem
{
	public float[] xData;
	public float[] yData;
}


public struct Limits
{
	public float xMin;
	public float xMax;
	public float yMin;
	public float yMax;
}