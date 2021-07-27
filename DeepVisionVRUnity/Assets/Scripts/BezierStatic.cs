using UnityEngine;

public class BezierStatic : MonoBehaviour
{
    public LineRenderer lineRenderer;
    private int layerOrder = 0;


    public void DrawCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segmentCount)
    {
        if (!lineRenderer)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        lineRenderer.sortingLayerID = layerOrder;
        
        // p0 = 1st point, p1 = handle of the 1st point, p2 = handle of the 2nd point, p3 = 2nd point
        lineRenderer.positionCount = segmentCount;
        for (int i = 1; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 pixel = CalculateCubicBezierPoint(t, p0, p1, p2, p3);
            lineRenderer.SetPosition(i - 1, pixel);
        }
    }


    Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }
}