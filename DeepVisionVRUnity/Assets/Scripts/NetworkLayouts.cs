using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;


public struct LayoutParams
{
    // general
    public enum LayoutMode { linearLayout, spiralLayout };
    public LayoutMode layoutMode;
    public float xMargin;
    public float minElementSize;
    public float minimalInfoScreenSize;

    // linear layout
    public bool xCentering;
    public bool xStrictGridPlacement;
    public float minimalZOffset;
    public float maximalZOffset;

    // spiral layout
    public float theta_0;
    public float b;

    // edges
    public int nPointsInBezier;
    public float edgeTextPosition;
    public float maxEdgeLabelSize;
}


public class NetworkLayouts
{
    private int[] numberOfElementsInStage;
    private float[] maxWidthOfLane;
    private float[] maxWidthOfStage;

    public void InitializeLayout(Transform[,] gridLayerElements, int[] gridSize, LayoutParams layoutParams)
    {
        // collect data for grid size and positioning
        Layer2D netLayer2D;

        // get the maximal number of elements for each network stage
        numberOfElementsInStage = new int[gridSize[0]];
        for (int posZ = 0; posZ < gridSize[0]; posZ++)
        {
            numberOfElementsInStage[posZ] = 0;
            for (int posX = 0; posX < gridSize[1]; posX++)
            {
                if (gridLayerElements[posZ, posX] != null) numberOfElementsInStage[posZ] = numberOfElementsInStage[posZ] + 1;
            }
        }

        // store for each lane the maximal width of an element plus margin. Ignore elements that are alone on one stage. Ignore text elements. Only process Layer2D elements.
        maxWidthOfLane = new float[gridSize[1]];
        for (int posX = 0; posX < gridSize[1]; posX++)
        {
            for (int posZ = 0; posZ < gridSize[0]; posZ++)
            {
                if (gridLayerElements[posZ, posX] != null && numberOfElementsInStage[posZ] > 1)
                {
                    netLayer2D = gridLayerElements[posZ, posX].GetComponent<Layer2D>();
                    if (netLayer2D != null)
                    {
                        if (maxWidthOfLane[posX] < netLayer2D.GetWidth()) maxWidthOfLane[posX] = netLayer2D.GetWidth();
                    }
                }
            }
            if (maxWidthOfLane[posX] > 0f) maxWidthOfLane[posX] = maxWidthOfLane[posX] + layoutParams.xMargin;
        }

        // store for each stage the maximal width of an element. Ignore text elements.
        maxWidthOfStage = new float[gridSize[0]];
        for (int i=0; i<maxWidthOfStage.Length; i++)
        {
            maxWidthOfStage[i] = layoutParams.minElementSize;
        }
        
        for (int posZ = 0; posZ < gridSize[0]; posZ++)
        {
            for (int posX = 0; posX < gridSize[1]; posX++)
            {

                if (gridLayerElements[posZ, posX] != null)
                {
                    netLayer2D = gridLayerElements[posZ, posX].GetComponent<Layer2D>();
                    if (netLayer2D != null)
                    {
                        if (maxWidthOfStage[posZ] < netLayer2D.GetWidth()) maxWidthOfStage[posZ] = netLayer2D.GetWidth();
                    }
                }
            }
        }
    }


    public void ApplyLayout(Transform[,] gridLayerElements, int[] gridSize, LayoutParams layoutParams)
    {
        if (layoutParams.layoutMode == LayoutParams.LayoutMode.linearLayout) ApplyLinearLayout(gridLayerElements, gridSize, layoutParams);
        else if (layoutParams.layoutMode == LayoutParams.LayoutMode.spiralLayout) ApplySpiralLayout(gridLayerElements, gridSize, layoutParams);
    }


    public void ApplyLinearLayout(Transform[,] gridLayerElements, int[] gridSize, LayoutParams layoutParams)
    {
        // move and scale layers in x direction to their grid positions
        Vector3 position;
        NetLayer netLayerScript;
        Transform textInstance;
        TextMeshPro textMesh;
        Transform gridLayerElement;
        float scale = 1f;
        float width = 0f;
        float xOffset = 0f; // subtract center of 0th line because of alignment

        for (int posZ = 0; posZ < gridSize[0]; posZ++)
        {
            for (int posX = 0; posX < gridSize[1]; posX++)
            {
                gridLayerElement = gridLayerElements[posZ, posX];
                if (gridLayerElements[posZ, posX] != null)
                {
                    // move and scale
                    position = new Vector3(xOffset, 0f, 0f);
                    scale = 1f;

                    textInstance = gridLayerElement.Find("TextMesh");
                    if (textInstance != null)
                    {
                        // we have a text elment
                        // add vertical offset
                        position += new Vector3(0f, 0.5f * maxWidthOfStage[posZ - 1], 0f);
                        // scale
                        textMesh = textInstance.GetComponent<TextMeshPro>();
                        textMesh.ForceMeshUpdate(true, true);
                        scale = Mathf.Max(0.5f * maxWidthOfStage[posZ - 1], layoutParams.minimalInfoScreenSize) / textMesh.bounds.size.x;
                        width = 0.5f * maxWidthOfStage[posZ - 1];
                    }

                    netLayerScript = gridLayerElement.GetComponent<NetLayer>();
                    if (netLayerScript != null)
                    {
                        width = netLayerScript.GetWidth();
                        // we have a network layer
                        if (width < layoutParams.minElementSize)
                        {
                            //scale
                            scale = layoutParams.minElementSize / width;
                            width = layoutParams.minElementSize;
                        }
                    }

                    gridLayerElement.localPosition = position;
                    if (netLayerScript == null) gridLayerElement.localScale = new Vector3(scale, scale, scale);
                    else netLayerScript.ApplyScale(scale);
                }
                if (layoutParams.xStrictGridPlacement) xOffset += maxWidthOfLane[posX];
                else xOffset += width + layoutParams.xMargin;
            }
            xOffset = 0f;
        }

        // center the layers in x direction
        if (layoutParams.xCentering)
        {
            float totalWidth;
            float meanWidth;
            int counter;
            for (int posZ = 0; posZ < gridSize[0]; posZ++)
            {
                totalWidth = 0f;
                counter = 0;
                for (int posX = 0; posX < gridSize[1]; posX++)
                {
                    if (gridLayerElements[posZ, posX] != null)
                    {
                        totalWidth += gridLayerElements[posZ, posX].localPosition.x;
                        counter++;
                    }
                }
                meanWidth = totalWidth / (float)counter;
                for (int posX = 0; posX < gridSize[1]; posX++)
                {
                    if (gridLayerElements[posZ, posX] != null)
                    {
                        gridLayerElements[posZ, posX].localPosition -= new Vector3(meanWidth, 0f, 0f);
                    }
                }
            }
        }

        // apply the z-shift to the layers

        float zOffsetStep = 0f;
        float zOffset = 0f;
        for (int posZ = 1; posZ < gridSize[0]; posZ++)
        {
            zOffsetStep = 0.75f * Mathf.Max(maxWidthOfStage[posZ], maxWidthOfStage[posZ - 1]);
            if (zOffsetStep < layoutParams.minimalZOffset) zOffsetStep = layoutParams.minimalZOffset;
            if (zOffsetStep > layoutParams.maximalZOffset) zOffsetStep = layoutParams.maximalZOffset;

            for (int posX = 0; posX < gridSize[1]; posX++)
            {
                if (gridLayerElements[posZ, posX] != null)
                {
                    gridLayerElements[posZ, posX].localPosition -= new Vector3(0f, 0f, -zOffset - zOffsetStep);
                }
            }
            zOffset += zOffsetStep;
        }
    }


    private Vector2 GetSpiral(float b, float theta)
    {
        return new Vector2(b * theta * Mathf.Cos(theta), b * theta * Mathf.Sin(theta));
    }


    private Vector2 GetSpiralTangent(float b, float theta)
    {
        Vector2 tangent = new Vector2(
            b * Mathf.Cos(theta) - b * theta * Mathf.Sin(theta),
            b * Mathf.Sin(theta) + b * theta * Mathf.Cos(theta));
        tangent = tangent.normalized;
        return tangent;
    }


    private Vector2 GetSpiralNormal(float b, float theta)
    {
        // theta in degrees
        Vector2 tangent = GetSpiralTangent(b, theta);
        Vector2 normal = new Vector3(tangent.y, -tangent.x);
        return normal;
    }


    /*private float GetSpiralTheta2(float theta1, float deltaS, float b)
    {
        // theta in degrees
        return Mathf.Sqrt(deltaS * 8f * Mathf.PI * Mathf.PI / b + theta1 * theta1);
    }*/

    private float GetDeltaTheta(float deltaS, Vector2 position2D)
    {
        // Pretend the spiral is a circle: what is the new angle when going deltaS long?
        float radius = position2D.magnitude;
        radius = Mathf.Max(radius, 1f);
        return deltaS / radius;
    }


    public void ApplySpiralLayout(Transform[,] gridLayerElements, int[] gridSize, LayoutParams layoutParams)
    {
        // move and scale layers in x direction to their grid positions
        
        NetLayer netLayerScript;
        Layer1D layer1DScript;
        Transform textInstance;
        TextMeshPro textMesh;
        float scale = 1f;
        float width = 0f;
        float thisStageLargestWidth = 0f;
        Vector2 normalOffset = new Vector2(0f, 0f); // subtract center of 0th line because of alignment
        float thetaRad = layoutParams.theta_0 / 360f * 2f * Mathf.PI;
        Vector3 position;
        Vector2 position2D = GetSpiral(layoutParams.b, thetaRad);
        Vector2 normal;
        bool stageContainsLayer1D = false;

        for (int posZ = 0; posZ < gridSize[0]; posZ++)
        {
            for (int posX = 0; posX < gridSize[1]; posX++)
            {
                Transform gridLayerElement = gridLayerElements[posZ, posX];
                if (gridLayerElement != null)
                {
                    // move and scale
                    position = new Vector3(position2D.x + normalOffset.x, 0f, position2D.y + normalOffset.y);
                    scale = 1f;

                    textInstance = gridLayerElement.Find("TextMesh");
                    if (textInstance != null)
                    {
                        // we have a text elment
                        // add vertical offset
                        position += new Vector3(0f, 0.5f * maxWidthOfStage[posZ - 1], 0f);
                        // scale
                        textMesh = textInstance.GetComponent<TextMeshPro>();
                        textMesh.ForceMeshUpdate(true, true);
                        width = Mathf.Max(0.5f * maxWidthOfStage[posZ - 1], layoutParams.minimalInfoScreenSize);
                        scale = width / textMesh.bounds.size.x;
                    }

                    netLayerScript = gridLayerElement.GetComponent<NetLayer>();
                    if (netLayerScript != null)
                    {
                        width = netLayerScript.GetWidth();
                        // we have a network layer
                        if (netLayerScript.GetWidth() < layoutParams.minElementSize)
                        {
                            //scale
                            scale = layoutParams.minElementSize / netLayerScript.GetWidth();
                            width = layoutParams.minElementSize;
                        }

                    }

                    layer1DScript = gridLayerElement.GetComponent<Layer1D>();
                    if (layer1DScript != null) 
                    {
                        width = 0.2f * width;
                        stageContainsLayer1D = true;
                    }
                    gridLayerElement.localPosition = position;
                    if (netLayerScript == null) gridLayerElement.localScale = new Vector3(scale, scale, scale);
                    else netLayerScript.ApplyScale(scale);
                    normal = GetSpiralNormal(layoutParams.b, thetaRad);
                    gridLayerElement.localRotation = Quaternion.LookRotation(new Vector3(normal.x, 0f, normal.y));
                    if (thisStageLargestWidth < width) thisStageLargestWidth = width;
                    normalOffset = normalOffset + GetSpiralNormal(layoutParams.b, thetaRad) * 0.75f * width;
                }
                
            }

            if (!stageContainsLayer1D) 
            {
                thetaRad += GetDeltaTheta(Mathf.Max(maxWidthOfStage[Mathf.Clamp(posZ + 1, 0, gridSize[0] - 1)], thisStageLargestWidth) + layoutParams.xMargin, position2D);
                position2D = GetSpiral(layoutParams.b, thetaRad);
                normalOffset.x = 0f;
                normalOffset.y = 0f;
                thisStageLargestWidth = 0f;
            }
            stageContainsLayer1D = false;
        }
    }


    public void DrawNetworkEdges(JArray architecture, GameObject bezierStaticPrefab, Transform dlManagerTransform, List<Transform> edges, List<Transform> edgeLabels, Dictionary<int, int[]> layerIDToGridPosition, Transform[,] gridLayerElements, GameObject textPrefab, LayoutParams layoutParams)
    {
        // Remove old edges and edge labels
        var toDelete = new List<GameObject>();
        foreach (Transform item in edges) toDelete.Add(item.gameObject);
        foreach (Transform item in edgeLabels) toDelete.Add(item.gameObject);
        toDelete.ForEach(item => GameObject.Destroy(item));

        // Draw NetworkGraphEdges
        int layer_id = 0;
        foreach (JToken jObject in architecture)
        {
            Vector2Int pos = new Vector2Int((int)jObject["pos"][0], (int)jObject["pos"][1]);
            string layerName = (string)jObject["layer_name"];

            foreach (JToken token in jObject["precursors"].Children())
            {
                Transform precursor = gridLayerElements[(int)token[0], (int)token[1]];
                if (precursor == null) continue;

                GameObject newBezierStaticInstance = (GameObject)GameObject.Instantiate(bezierStaticPrefab, Vector3.zero, dlManagerTransform.rotation, dlManagerTransform);
                newBezierStaticInstance.name = "NetworkGraphEdge "
                    + string.Format("{0}", (int)token[0])
                    + string.Format("{0}", (int)token[1])
                    + string.Format("{0}", pos[0])
                    + string.Format("{0}", pos[1]);

                edges.Add(newBezierStaticInstance.transform);

                // determine edge points
                newBezierStaticInstance.transform.localPosition = Vector3.zero;
                var posTmp = layerIDToGridPosition[layer_id];
                Vector3 pos_new_layer = gridLayerElements[posTmp[0], posTmp[1]].localPosition;//.localToWorldMatrix.MultiplyPoint3x4(layers[layer_id].GetComponent<NetLayer>().center_pos);
                //pos_new_layer = transform.worldToLocalMatrix.MultiplyPoint3x4(pos_new_layer);
                
                Vector3 pos_precursor = precursor.localPosition;//.localToWorldMatrix.MultiplyPoint3x4(precursor.GetComponent<NetLayer>().center_pos);
                //pos_precursor = transform.worldToLocalMatrix.MultiplyPoint3x4(pos_precursor);
                // put edges on the ground
                pos_precursor.y = dlManagerTransform.position.y;
                pos_new_layer.y = dlManagerTransform.position.y;

                // draw edges
                BezierStaticEQ bezierCurve = newBezierStaticInstance.GetComponent<BezierStaticEQ>();
                float startingPointHandleFactor = 0f;
                if (layoutParams.layoutMode == LayoutParams.LayoutMode.linearLayout) startingPointHandleFactor = 0.1f;
                if (layoutParams.layoutMode == LayoutParams.LayoutMode.spiralLayout) startingPointHandleFactor = -1f;
                bezierCurve.DrawCurve(
                    pos_precursor,
                    pos_precursor + precursor.forward * startingPointHandleFactor,
                    pos_new_layer + gridLayerElements[posTmp[0], posTmp[1]].forward * (-1f),
                    pos_new_layer,
                    layoutParams.nPointsInBezier);

                // draw text
                GameObject textInstance = GameObject.Instantiate(textPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f), dlManagerTransform);
                textInstance.name = "Text " + layerName;
                TextMeshPro textMeshPro = textInstance.GetComponent<TextMeshPro>();
                textMeshPro.text = layerName;
                edgeLabels.Add(textInstance.transform);
                textMeshPro.ForceMeshUpdate(true, true);

                // collect positional information for text
                Vector3[] positions = new Vector3[bezierCurve.lineRenderer.positionCount];
                bezierCurve.lineRenderer.GetPositions(positions);
                int idx = (int)Mathf.Floor(layoutParams.edgeTextPosition * (float)positions.Length) - 1;
                Vector3 point1 = positions[idx];
                Vector3 tangent = positions[idx + 1] - point1;
                tangent = tangent.normalized;
                Vector3 orthogonal = new Vector3(tangent.z, tangent.y, -tangent.x);

                //set text transform
                if (textMeshPro.text != "")
                {
                    float preferredWidth = Mathf.Min(0.75f * (positions[positions.Length - 1] - point1).magnitude, layoutParams.maxEdgeLabelSize);
                    float scale = preferredWidth / textMeshPro.bounds.size.x;
                    textInstance.transform.localScale = new Vector3(scale, scale, scale);
                    float sign_is_right = 1f;
                    if ( layoutParams.layoutMode == LayoutParams.LayoutMode.linearLayout && point1.x <= 0)
                    {
                        sign_is_right = -1f;
                    }
                    textInstance.transform.localPosition = point1 + 1f * textMeshPro.bounds.size.y * scale * orthogonal * sign_is_right;
                    float tangent_points_right = 1f;
                    if ( (layoutParams.layoutMode == LayoutParams.LayoutMode.linearLayout && tangent.x < 0) || layoutParams.layoutMode == LayoutParams.LayoutMode.spiralLayout)
                    {
                        tangent_points_right = -1f;
                    }
                    Quaternion textRot = Quaternion.FromToRotation(new Vector3(tangent_points_right, 0f, 0f), tangent) * Quaternion.Euler(new Vector3(90f, 0f, 0f));
                    textInstance.transform.localRotation = textRot;
                }
            }
            layer_id = layer_id + 1;
        }
    }
}
