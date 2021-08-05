using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class LinePlotter : MonoBehaviour
{
	public RectTransform plottingArea;
	public GameObject lineRendererPrefab;
	public GameObject boxPrefab;
	private List<Transform> plotItemList;
	private List<DataItem> data;
	public Transform titelTextMesh;
	public Transform xLabel;
	public Transform yLabel;
	public GameObject xTickLabelPrefab;
	public GameObject yTickLabelPrefab;
	private Axis axis;
	private Color[] colors = { Color.blue, Color.red, Color.green };
	public float plottingAreaMargin = 0.15f;
	public enum DiagramType { linearPlot, histogramPlot };
    public DiagramType diagramType;


	void Start()
	{
		// prepare
		data = new List<DataItem>();
		plotItemList = new List<Transform>();
		titelTextMesh.gameObject.SetActive(false);
		xLabel.gameObject.SetActive(false);
		yLabel.gameObject.SetActive(false);
		
		// done by external script
		AddTitle("Awesome!");
		AddXLabel("Best x label");
		AddYLabel("Best y label");
		DataItem dataItem;
		//dataItem.xData = new float[] { -1f, 0f, 1f, 2f, 3f, 4f };
		dataItem.xData = new float[] { 0f, 1f, 2f, 3f, 4f, 5f };
		dataItem.yData = new float[] { 2f, 6f, 2f, 5f, 1f, 0f };
		AddData(dataItem);
		
		// draw
		axis = new Axis(xTickLabelPrefab, yTickLabelPrefab, this);
		Draw();
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


	public void Draw()
	{
		ClearGraph();
		
		// find plotting area
		plottingArea.ForceUpdateRectTransforms();
		Rect rect = plottingArea.rect;
		float width = rect.width;
		float height = rect.height;

		(Limits dataLimits, Limits plottingLimits) = ComputeLimits();

		axis.Draw(width, height, dataLimits, plottingLimits);

		if (diagramType == DiagramType.linearPlot) PlotLinear(width, height, plottingLimits);
		else PlotHistogram(width, height, plottingLimits);
	}


	public void PlotLinear(float width, float height, Limits plottingLimits)
	{
		Vector3[] points;
		for (int i = 0; i < data.Count; i++)
		{
			points = MapDataToPosition(data[i], width, height, plottingLimits);
			plotItemList.Add(drawLine(points, colors[i % colors.Length]));
		}
	}


	public void PlotHistogram(float width, float height, Limits plottingLimits)
	{
		DataItem dataItem;
		Vector3[] points;

		// find y = 0 position
		dataItem = new DataItem();
		dataItem.yData = new float[1] {0f};
		dataItem.xData = new float[1] {0f};
		points = MapDataToPosition(dataItem, width, height, plottingLimits);
		float bottomYPosition = points[0].y;

		// plot first data item only. Overwrite xData.
		dataItem = data[0];
		dataItem.xData = new float[dataItem.yData.Length];
		for (int i = 0; i < dataItem.yData.Length; i++)
		{
			dataItem.xData[i] = (float)i + 0.5f;
		}

		// plot boxes
		points = MapDataToPosition(dataItem, width, height, plottingLimits);
		float boxWidth = (points[1].x - points[0].x) * 0.9f;
		float boxHeight;
		Vector3 position;
		for (int i = 0; i < dataItem.yData.Length; i++)
		{
			boxHeight = points[i].y;
			position = new Vector3(points[i].x, bottomYPosition, 0f);
			plotItemList.Add(DrawBox(position, boxWidth, boxHeight, Color.blue));
		}
	}


	public (Limits, Limits) ComputeLimits()
	{
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

		return (dataLimits, plottingLimits);
	}

	public Vector3[] MapDataToPosition(DataItem dataItem, float width, float height, Limits plottingLimits)
	{
		int length = dataItem.xData.Length;
		Vector3[] points = new Vector3[length];

		for (int i = 0; i < length; i++)
		{
			points[i] = new Vector3((dataItem.xData[i] - plottingLimits.xMin) / (plottingLimits.xMax - plottingLimits.xMin) * width, (dataItem.yData[i] - plottingLimits.yMin) / (plottingLimits.yMax - plottingLimits.yMin) * height, -0.001f);
		}

		return points;
	}


	public Transform drawLine(Vector3[] points, Color color, float widthMultiplier=1f)
    {
		Transform lineInstance = ((GameObject)Instantiate(lineRendererPrefab)).transform;
		lineInstance.name = "GraphLine";
		lineInstance.SetParent(plottingArea);
		lineInstance.localPosition = Vector3.zero;
		lineInstance.localRotation = Quaternion.identity;
		lineInstance.localScale = Vector3.one;
		LineRenderer lineRenderer = lineInstance.GetComponent<LineRenderer>();
		lineRenderer.material.color = color;
		lineRenderer.startWidth = lineRenderer.startWidth * widthMultiplier;
		lineRenderer.positionCount = points.Length;
		lineRenderer.SetPositions(points);
		return lineInstance;
	}

	public Transform DrawBox(Vector3 position, float boxWidth, float boxHeight, Color color)
	{
		Transform boxInstance = ((GameObject)Instantiate(boxPrefab)).transform;
		boxInstance.name = "Box";
		boxInstance.SetParent(plottingArea);
		boxInstance.localPosition = position;
		boxInstance.localRotation = Quaternion.identity;
		boxInstance.localScale = Vector3.one;
		RectTransform rectTransform = boxInstance.GetComponent<RectTransform>();
		rectTransform.sizeDelta = new Vector2(boxWidth, boxHeight);
		CanvasRenderer renderer = boxInstance.GetComponent<CanvasRenderer>();
		renderer.SetColor(color);
		return boxInstance;
	}


	public void ClearGraph()
	{
		axis.Clear();

		var toDelete = new List<GameObject>();
		foreach (Transform item in plotItemList) toDelete.Add(item.gameObject);
		toDelete.ForEach(item => GameObject.Destroy(item));
	}






	public class Axis 
	{
		private List<Transform> xTicks;
		private List<Transform> yTicks;
		private Transform xAxis;
		private Transform yAxis;
		private List<Transform> xTickLabels;
		private List<Transform> yTickLabels;
		private GameObject xTickLabelPrefab;
		private GameObject yTickLabelPrefab;
		private LinePlotter linePlotter;

		public Axis(GameObject _xTickLabelPrefab, GameObject _yTickLabelPrefab, LinePlotter _linePlotter)
		{
			xTicks = new List<Transform>();
			yTicks = new List<Transform>();
			xTickLabels = new List<Transform>();
			yTickLabels = new List<Transform>();
			xTickLabelPrefab = _xTickLabelPrefab;
			yTickLabelPrefab = _yTickLabelPrefab;
			linePlotter = _linePlotter;
		}


		public float[] getTickPositions(float a, float b) 
		{
			// find step size
			float d = b - a;
			// we want to have about n = 6 ticks
			float n = 6;
			// compute approximate tick distance
			float l = d / n;
			float p = Mathf.Round(Mathf.Log10(l));
			float[] stepCandidates = {
				1f * Mathf.Pow(10, p),
				2f * Mathf.Pow(10, p),
				5f * Mathf.Pow(10, p)
			};
			float[] distances = {
				Mathf.Abs(stepCandidates[0] - l), 
				Mathf.Abs(stepCandidates[1] - l),
				Mathf.Abs(stepCandidates[2] - l)};
			float m = distances.Min();
			int idx = Array.IndexOf(distances, m);
			float stepSize = stepCandidates[idx];
			// find first tick
			float startTick = Mathf.Floor(a / stepSize) * stepSize;
			// compute number of ticks
			int nTicks = (int)Mathf.Ceil((b - startTick) / stepSize) + 1;
			// create tick array
			float[] ticks = new float[nTicks];
			for (int i = 0; i < nTicks; i++) 
			{
				ticks[i] = startTick + i * stepSize;
			}
			return ticks;
		}


		public void AddXTickLabels(float width, float height, Limits dataLimits, Limits plottingLimits)
		{
			Vector3[] tickLabelPoints;
			DataItem tickLabelDataItem = new DataItem();
			Vector3[] tickPoints = new Vector3[2];
			tickLabelDataItem.xData = getTickPositions(dataLimits.xMin, dataLimits.xMax);
			tickLabelDataItem.yData = new float[tickLabelDataItem.xData.Length];
			tickLabelPoints = linePlotter.MapDataToPosition(tickLabelDataItem, width, height, plottingLimits);
			float deltaTick = 0.01f * width;
			for (int i = 0; i < tickLabelPoints.Length; i++)
			{
				// add label
				Vector3 pos = tickLabelPoints[i];
				Transform tickInstance = ((GameObject)Instantiate(xTickLabelPrefab)).transform;
				tickInstance.name = "x tick";
				tickInstance.SetParent(linePlotter.plottingArea);
				tickInstance.localPosition = new Vector3(pos[0], pos[1], pos[2]);
				tickInstance.localRotation = Quaternion.identity;
				tickInstance.localScale = Vector3.one;
				tickInstance.GetComponent<TextMeshProUGUI>().text = string.Format("{0}", (float)tickLabelDataItem.xData[i]);
				xTickLabels.Add(tickInstance);

				// add ticks
				tickPoints[0] = new Vector3(pos[0], pos[1] - deltaTick, pos[2]);
				tickPoints[1] = new Vector3(pos[0], pos[1] + deltaTick, pos[2]);
				xTicks.Add(linePlotter.drawLine(tickPoints, Color.white));
			}
		}


		public void AddYTickLabels(float width, float height, Limits dataLimits, Limits plottingLimits)
		{
			Vector3[] tickLabelPoints;
			DataItem tickLabelDataItem = new DataItem();
			Vector3[] tickPoints = new Vector3[2];
			tickLabelDataItem.yData = getTickPositions(dataLimits.yMin, dataLimits.yMax);
			tickLabelDataItem.xData = new float[tickLabelDataItem.yData.Length];
			tickLabelPoints = linePlotter.MapDataToPosition(tickLabelDataItem, width, height, plottingLimits);
			float deltaTick = 0.01f * width;
			for (int i = 0; i < tickLabelPoints.Length; i++)
			{
				// add label
				Vector3 pos = tickLabelPoints[i];
				Transform tickInstance = ((GameObject)Instantiate(yTickLabelPrefab)).transform;
				tickInstance.name = "y tick";
				tickInstance.SetParent(linePlotter.plottingArea);
				tickInstance.localPosition = new Vector3(pos[0], pos[1], pos[2]);
				tickInstance.localRotation = Quaternion.identity;
				tickInstance.localScale = Vector3.one;
				tickInstance.GetComponent<TextMeshProUGUI>().text = string.Format("{0}", (float)tickLabelDataItem.yData[i]);
				yTickLabels.Add(tickInstance);

				// add ticks
				tickPoints[0] = new Vector3(pos[0] - deltaTick, pos[1], pos[2]);
				tickPoints[1] = new Vector3(pos[0] + deltaTick, pos[1], pos[2]);
				yTicks.Add(linePlotter.drawLine(tickPoints, Color.white));
			}
		}


		public void Draw(float width, float height, Limits dataLimits, Limits plottingLimits)
		{
			Vector3[] points;
			DataItem axisDataItem = new DataItem();

			// draw x axis
			axisDataItem.xData = new float[] { plottingLimits.xMin, plottingLimits.xMax };
			axisDataItem.yData = new float[] { 0f, 0f };
			points = linePlotter.MapDataToPosition(axisDataItem, width, height, plottingLimits);
			xAxis = linePlotter.drawLine(points, Color.white);
			// draw y axis
			axisDataItem.xData = new float[] { 0f, 0f };
			axisDataItem.yData = new float[] { plottingLimits.yMin, plottingLimits.yMax };
			points = linePlotter.MapDataToPosition(axisDataItem, width, height, plottingLimits);
			xAxis = linePlotter.drawLine(points, Color.white);

			// draw ticklabels
			AddXTickLabels(width, height, dataLimits, plottingLimits);
			AddYTickLabels(width, height, dataLimits, plottingLimits);
		}


		public void Clear()
		{
			var toDelete = new List<GameObject>();
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
}