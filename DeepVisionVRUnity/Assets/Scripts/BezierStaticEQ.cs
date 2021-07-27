using UnityEngine;

public class BezierStaticEQ : MonoBehaviour
{
    public LineRenderer lineRenderer;
    private int layerOrder = 0;


	public void DrawCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segmentCount)
    {
		// p0 = 1st point, p1 = handle of the 1st point, p2 = handle of the 2nd point, p3 = 2nd point

		if (!lineRenderer)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        lineRenderer.sortingLayerID = layerOrder;

		int _precision = 4 * segmentCount;
		float length = 0f;
		float[] arcLengths = new float[_precision];
		Vector3 oldPoint = CalculateCubicBezierPoint(0, p0, p1, p2, p3);

		for (int p = 1; p < arcLengths.Length; p++)
		{
			Vector3 newPoint = CalculateCubicBezierPoint((float) p / _precision, p0, p1, p2, p3);//get next point
			arcLengths[p] = Vector3.Distance(oldPoint, newPoint); //find distance to old point
			length += arcLengths[p]; //add it to the bezier's length
			oldPoint = newPoint; //new is old for next loop
		}

		//create our points array
		Vector3[] points = new Vector3[segmentCount];
		//target length for spacing
		float segmentLength = length / segmentCount;

		//arc index is where we got up to in the array to avoid the Shlemiel error http://www.joelonsoftware.com/articles/fog0000000319.html
		int arcIndex = 0;

		float walkLength = 0; //how far along the path we've walked
		oldPoint = CalculateCubicBezierPoint(0, p0, p1, p2, p3);

		//iterate through points and set them
		for (int i = 0; i < points.Length; i++)
		{
			float iSegLength = i * segmentLength; //what the total length of the walkLength must equal to be valid
													//run through the arcLengths until past it
			while (walkLength < iSegLength)
			{
				walkLength += arcLengths[arcIndex]; //add the next arcLength to the walk
				arcIndex++; //go to next arcLength
			}
			//walkLength has exceeded target, so lets find where between 0 and 1 it is
			points[i] = CalculateCubicBezierPoint((float)arcIndex / arcLengths.Length, p0, p1, p2, p3);
		}

		lineRenderer.positionCount = segmentCount;
        lineRenderer.SetPositions(points);
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