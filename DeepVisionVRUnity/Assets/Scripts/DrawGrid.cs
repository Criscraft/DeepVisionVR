using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawGrid : MonoBehaviour
{

    [SerializeField]
	private GameObject lineRendererPrefab;
    [SerializeField]
    private float spacing = 20f;
    [SerializeField]
    private int number = 10;
    [SerializeField]
    private Color color;
    [SerializeField]
    private float widthMultiplier = 1f;


    void Start()
    {
        for (int i=-number; i<number+1; i++)
        {
            GenerateLine(new Vector3(spacing * i, 0f, -number*spacing), new Vector3(spacing * i, 0f, +number*spacing));
        }

        for (int j=-number; j<number+1; j++)
        {
            GenerateLine(new Vector3(-number*spacing, 0f, spacing * j), new Vector3(+number*spacing, 0f, spacing * j));
        }
    }

    private void GenerateLine(Vector3 from, Vector3 to)
    {
        Transform lineInstance = Instantiate(lineRendererPrefab).transform;
        lineInstance.name = "GridLine";
        lineInstance.SetParent(transform);
        lineInstance.localPosition = Vector3.zero;
        lineInstance.localRotation = Quaternion.identity;
        lineInstance.localScale = Vector3.one;
        LineRenderer lineRenderer = lineInstance.GetComponent<LineRenderer>();
        lineRenderer.material.color = color;
        lineRenderer.startWidth = lineRenderer.startWidth * widthMultiplier;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(new Vector3[2]{from, to});
    }
}
