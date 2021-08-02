using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class DLManager : MonoBehaviour
{
    public GameObject node2DPrefab;
    public GameObject layer1DParticleSystemPrefab;
    public GameObject networkImageInputFramePrefab;
    public GameObject textPrefab;
    public GameObject bezierStaticPrefab;
    public Transform imagePickerCanvasContent;
    public GameObject imageGetterButtonPrefab;
    public GameObject networkInfoScreenPrefab;
    public Transform foreignResultCanvasInstance;
    private Transform ownResultCanvasInstance;
    public XRBaseInteractor rightInteractor;
    public XRBaseInteractor leftInteractor;
    public Transform resultCanvasContent;
    public GameObject resultCanvasContentElementPrefab;
    public bool xCentering = true;
    public float minElementSize = 0.75f;
    public float minimalZOffset = 0.75f;
    public float maximalZOffset = 10f;
    public float xMargin = 0.75f;
    public float minimalInfoScreenSize = 0.75f;
    public int nPointsInBezier = 20;
    public float edgeTextPosition = 0.7f;
    private DLClient _dlClient;
    private int lenDataset = 0;
    private JArray classNames;
    private List<JObject> architecture;
    private Transform[,] gridLayerElements; // Z , X 
    private Dictionary<int,int[]> layerIDToGridPosition = new Dictionary<int,int[]>(); // Z size, X size
    private Transform networkImageInputFrameInstance;
    private int[] gridSize = {0,0}; // Z size, X size
    private List<Transform> edges = new List<Transform>();
    private List<Transform> edgeLabels = new List<Transform>();
    private int[] numberOfXElements;

    public void RequestNetworkArchitecture()
    {
        _dlClient.RequestNetworkArchitecture();
    }

    public IEnumerator AcceptNetworkArchitecture(List<JObject> jObjectList)
    {
        Debug.Log("Received AcceptNetworkArchitecture");
        architecture = jObjectList;

        // find grid size
        int posX = 0;
        int posZ = 0;
        foreach (JObject jObject in architecture)
        {
            posZ = (int)jObject["pos"][0];
            if (gridSize[0] < posZ) gridSize[0] = posZ; 
            posX = (int)jObject["pos"][1];
            if (gridSize[1] < posX) gridSize[1] = posX; 
        }
        gridSize[0] = gridSize[0] + 1;
        gridSize[1] = gridSize[1] + 1;
        Debug.Log("Create Layers");
        CreateLayers();
        UpdateAllLayers();
        yield return null;
    }


    public void RequestDataOverview()
    {
        _dlClient.RequestDataOverview();
    }


    public IEnumerator AcceptDataOverview(JObject jObject)
    {
        Debug.Log("Received AcceptDataOverview");
        //nClasses = (int)jObject["n_classes"];
        lenDataset = (int)jObject["len"];
        classNames = JArray.Parse(jObject["class_names"].ToString());
        
        Debug.Log("Begin RequestDatasetImage");
        for (int i = 0; i < lenDataset; i++)
        {
            RequestDatasetImage(i);
        }
        yield return null;
    }


    public void RequestDatasetImage(int imgIndex)
    {
        _dlClient.RequestDatasetImage(imgIndex);
    }


    public IEnumerator AcceptDatasetImage(Texture2D tex, int label, int imgIndex)
    {
        GameObject newImageGetterButton = (GameObject)Instantiate(imageGetterButtonPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f));
        newImageGetterButton.name = "ImageGetterButton " + string.Format("{0}", imgIndex);
        newImageGetterButton.transform.SetParent(imagePickerCanvasContent, false);
        newImageGetterButton.GetComponent<ImageGetterButton>().Prepare(rightInteractor, leftInteractor, imgIndex, label, (string)classNames[label], tex);

        Transform textMesh = newImageGetterButton.transform.GetChild(0);
        TextMeshProUGUI textMeshPro = textMesh.GetComponent<TextMeshProUGUI>();
        textMeshPro.text = (string)classNames[label];
        yield return null;
    }


    public void RequestLayerActivation(int layerID)
    {
        string datatype = (string)architecture[layerID]["data_type"];
        if (datatype == "2D_feature_map" || datatype == "1D_vector")
        {
            _dlClient.RequestLayerActivation(layerID);
        }
    }

    public IEnumerator AcceptLayerActivation(List<Texture2D> textureList, int layerID, float zeroValue)
    {
        Debug.Log("Received AcceptLayerActivation for layer " + string.Format("{0}", layerID));
        var pos = layerIDToGridPosition[layerID];
        gridLayerElements[pos[0], pos[1]].GetComponent<NetLayer>().updateData(textureList, transform.localScale[0], zeroValue);
        yield return null;
    }


    public void RequestPrepareForInput(int imgIndex)
    {
        _dlClient.RequestPrepareForInput(imgIndex);
    }

    public IEnumerator AcceptPrepareForInput()
    {
        UpdateAllLayers();
        yield return null;
    }


    public void RequestClassificationResult()
    {
        _dlClient.RequestClassificationResult();
    }

    public IEnumerator AcceptClassificationResult(JObject jObject)
    {
        // Remove old classification Results
        var children = new List<GameObject>();
        foreach (Transform child in resultCanvasContent) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        // fill the foreign result canvas
        JArray classIndices = JArray.Parse(jObject["class_indices"].ToString());
        JArray confidenceValues = JArray.Parse(jObject["confidence_values"].ToString());
        for (int i = 0; i < classIndices.Count; i++)
        {
            GameObject newResultCanvasContentElement = (GameObject)Instantiate(resultCanvasContentElementPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f));
            newResultCanvasContentElement.name = "ResultField " + (string)classNames[(int)classIndices[i]];
            newResultCanvasContentElement.transform.SetParent(resultCanvasContent, false);
            TextMeshProUGUI textMeshProUGIO = newResultCanvasContentElement.GetComponentInChildren<TextMeshProUGUI>();
            textMeshProUGIO.text = (string)classNames[(int)classIndices[i]] + " - " + string.Format("{0}%", (float)confidenceValues[i]);
        }

        // Destroy old own canvas instance
        if (ownResultCanvasInstance != null)
        {
            Destroy(ownResultCanvasInstance);
        }
        // create new own canvas instance
        var pos = layerIDToGridPosition[architecture.Count - 1];
        ownResultCanvasInstance = Instantiate(foreignResultCanvasInstance, Vector3.zero, Quaternion.Euler(0f, 0f, 0f), gridLayerElements[pos[0], pos[1]]).transform;
        ownResultCanvasInstance.name = "ClassificationResultCanvas";
        ownResultCanvasInstance.localPosition = new Vector3(0f, 1.7f, 0f);
        yield return null;
    }


    public void CreateLayers()
    {
        // create image frame
        networkImageInputFrameInstance = Instantiate(networkImageInputFramePrefab).transform;
        networkImageInputFrameInstance.SetParent(transform);
        networkImageInputFrameInstance.localScale = new Vector3(30f, 30f, 30f);
        networkImageInputFrameInstance.localPosition = new Vector3(0f, 0f, -minimalZOffset);
        networkImageInputFrameInstance.localRotation = Quaternion.Euler(0f, 0f, 0f);
        networkImageInputFrameInstance.name = "Network Image Input Frame";
        networkImageInputFrameInstance.GetComponent<NetworkImageInputFrame>().Prepare(this);

        // create network layers without positioning or scaling them
        gridLayerElements = new Transform[gridSize[0], gridSize[1]];
        
        GameObject netLayer = new GameObject();
        GameObject newLayerInstance = null;
        int layerID = 0;

        foreach (JObject jObject in architecture)
        {
            int[] gridPos = new int[2] {(int)jObject["pos"][0], (int)jObject["pos"][1]};
            string datatype = (string)jObject["data_type"];
            
            if (datatype == "2D_feature_map")
            {
                Vector3Int size = new Vector3Int((int)jObject["size"][1], (int)jObject["size"][2], (int)jObject["size"][3]);
                newLayerInstance = (GameObject)Instantiate(netLayer, Vector3.zero, transform.rotation, transform);
                newLayerInstance.name = "2D_feature_map_layer " + string.Format("{0}", gridPos[0]) + "," + string.Format("{0}", gridPos[1]);
                newLayerInstance.AddComponent<Layer2D>();
                Layer2D layer2DScript = newLayerInstance.GetComponent<Layer2D>();
                layer2DScript.Prepare(node2DPrefab, (string)jObject["layer_name"], size);
            }
            else if (datatype == "1D_vector")
            {
                int size = (int)jObject["size"][1];
                newLayerInstance = (GameObject)Instantiate(netLayer, Vector3.zero, transform.rotation, transform);
                newLayerInstance.name = "1D_vector_layer " + string.Format("{0}", gridPos[0]) + "," + string.Format("{0}", gridPos[1]);
                newLayerInstance.AddComponent<Layer1D>();
                Layer1D layer1DScript = newLayerInstance.GetComponent<Layer1D>();
                layer1DScript.Prepare(layer1DParticleSystemPrefab, (string)jObject["layer_name"], size);
            }
            else if (datatype == "None") // could change to "InfoScreen"
            {
                newLayerInstance = (GameObject)Instantiate(networkInfoScreenPrefab, Vector3.zero, transform.rotation, transform);
                newLayerInstance.name = "Info Screen " + string.Format("{0}", gridPos[0]) + "," + string.Format("{0}", gridPos[1]);
                Transform textInstance = newLayerInstance.transform.Find("TextMesh");
                TextMeshPro textMeshPro = textInstance.GetComponent<TextMeshPro>();
                textMeshPro.text = (string)jObject["layer_name"];
            }
            else
            {
                Debug.LogError("Could not identify network layer data type", transform);
            }

            gridLayerElements[gridPos[0], gridPos[1]] = newLayerInstance.transform;
            layerIDToGridPosition.Add(layerID, new int[2] {gridPos[0], gridPos[1]});
            layerID ++;
        }

        applyLayout();
        drawNetworkEdges();
        Destroy(netLayer);
    }
        

    private void applyLayout()
    {
        // collect data for grid size and positioning
        // get the maximal width for each network lane

        NetLayer netLayerScript;
        Layer2D netLayer2D;

        numberOfXElements = new int[gridSize[0]];
        for (int posZ = 0; posZ < gridSize[0]; posZ ++) 
        {
            numberOfXElements[posZ] = 0;
            for (int posX = 0; posX < gridSize[1]; posX ++)
            {
                if (gridLayerElements[posZ, posX] != null) numberOfXElements[posZ] = numberOfXElements[posZ] + 1;
            }
        }

        // store for each lane the maximal width of an element plus margin. Ignore elements that are alone on one stage. Ignore text elements. Only process Layer2D elements.
        float[] gridLaneWidths = new float[gridSize[1]];
        for (int posX = 0; posX < gridSize[1]; posX++)
        {
            for (int posZ = 0; posZ < gridSize[0]; posZ++)
            {
                if (gridLayerElements[posZ, posX] != null && numberOfXElements[posZ] > 1)
                {
                    netLayer2D = gridLayerElements[posZ, posX].GetComponent<Layer2D>();
                    if (netLayer2D != null)
                    {
                        if (gridLaneWidths[posX] < netLayer2D.width) gridLaneWidths[posX] = netLayer2D.width;
                    }
                }
            }
            if (gridLaneWidths[posX] > 0f) gridLaneWidths[posX] = gridLaneWidths[posX] + xMargin;
        }

        // store for each stage the maximal width of an element. Ignore text elements.
        float[] gridStageWidths = new float[gridSize[0]];
        for (int posZ = 0; posZ < gridSize[0]; posZ++)
        {
            for (int posX = 0; posX < gridSize[1]; posX++)
            {
                
                if (gridLayerElements[posZ, posX] != null)
                {
                    netLayer2D = gridLayerElements[posZ, posX].GetComponent<Layer2D>();
                    if (netLayer2D != null)
                    {
                        if (gridStageWidths[posZ] < netLayer2D.width) gridStageWidths[posZ] = netLayer2D.width;
                    }
                }
            }
        }

        // move and scale layers in x direction to their grid positions
        Vector3 position;
        Transform textInstance;
        TextMeshPro textMesh;
        float scale = 1f;
        float xOffset = -0.5f * gridLaneWidths[0]; // subtract center of 0th line because of alignment

        for (int posX = 0; posX < gridSize[1]; posX ++) 
        {
            for (int posZ = 0; posZ < gridSize[0]; posZ ++)
            {
                if (gridLayerElements[posZ, posX] != null)
                {
                    // move and scale
                    position = new Vector3(xOffset + 0.5f * gridLaneWidths[posX], 0f, 0f);
                    scale = 1f;

                    textInstance = gridLayerElements[posZ, posX].Find("TextMesh");
                    if (textInstance != null)
                    {
                        // we have a text elment
                        // add vertical offset
                        position += new Vector3(0f, 0.5f * gridStageWidths[posZ - 1], 0f);
                        // scale
                        textMesh = textInstance.GetComponent<TextMeshPro>();
                        textMesh.ForceMeshUpdate(true, true);
                        scale = Mathf.Max(0.5f * gridStageWidths[posZ - 1], minimalInfoScreenSize) / textMesh.bounds.size.x;
                    }
                    
                    netLayerScript = gridLayerElements[posZ, posX].GetComponent<NetLayer>();
                    if (netLayerScript != null)
                    {
                        // we have a network layer
                        if (netLayerScript.width < minElementSize)
                        {
                            //scale
                            scale = minElementSize / netLayerScript.width;
                        }
                    }

                    gridLayerElements[posZ, posX].localPosition = position;
                    gridLayerElements[posZ, posX].localScale = new Vector3(scale, scale, scale);
                }
            }
            xOffset += gridLaneWidths[posX];
        }

        // center the layers in x direction
        if (xCentering)
        {
            float totalWidth;
            float meanWidth;
            int counter;
            for (int posZ = 0; posZ < gridSize[0]; posZ ++) 
            {
                totalWidth = 0f;
                counter = 0;
                for (int posX = 0; posX < gridSize[1]; posX ++)
                {
                    if (gridLayerElements[posZ, posX] != null)
                    {
                        totalWidth += gridLayerElements[posZ, posX].localPosition.x;
                        counter ++;
                    }
                }
                meanWidth = totalWidth / (float)counter;
                for (int posX = 0; posX < gridSize[1]; posX ++)
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
        for (int posZ = 1; posZ < gridSize[0]; posZ ++) 
        {
            zOffsetStep = gridStageWidths[posZ - 1];
            if (zOffsetStep < minimalZOffset) zOffsetStep = minimalZOffset;
            if (zOffsetStep > maximalZOffset) zOffsetStep = maximalZOffset;

            for (int posX = 0; posX < gridSize[1]; posX ++)
            {
                if (gridLayerElements[posZ, posX] != null)
                {
                    gridLayerElements[posZ, posX].localPosition -= new Vector3(0f, 0f, -zOffset - zOffsetStep);
                }
            }
            zOffset += zOffsetStep;
        }
    }


    private void drawNetworkEdges()
    {
        // Draw NetworkGraphEdges
        int layer_id = 0;
        foreach (JObject jObject in architecture)
        {
            Vector2Int pos = new Vector2Int((int)jObject["pos"][0], (int)jObject["pos"][1]);
            string layerName = (string)jObject["layer_name"];

            foreach (JToken token in jObject["precursors"].Children())
            {
                GameObject newBezierStaticInstance = (GameObject)Instantiate(bezierStaticPrefab, Vector3.zero, transform.rotation, transform);
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
                Transform precursor = gridLayerElements[(int)token[0], (int)token[1]];
                Vector3 pos_precursor = precursor.localPosition;//.localToWorldMatrix.MultiplyPoint3x4(precursor.GetComponent<NetLayer>().center_pos);
                //pos_precursor = transform.worldToLocalMatrix.MultiplyPoint3x4(pos_precursor);
                // put edges on the ground
                pos_precursor.y = transform.position.y;
                pos_new_layer.y = transform.position.y;

                // draw edges
                BezierStaticEQ bezierCurve = newBezierStaticInstance.GetComponent<BezierStaticEQ>();
                bezierCurve.DrawCurve(
                    pos_precursor,
                    pos_precursor + new Vector3(0f, 0f, 0.1f),
                    pos_new_layer + new Vector3(0f, 0f, -1f),
                    pos_new_layer,
                    nPointsInBezier);

                // draw text
                GameObject textInstance = Instantiate(textPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f), transform);
                textInstance.name = "Text " + layerName;
                TextMeshPro textMeshPro = textInstance.GetComponent<TextMeshPro>();
                textMeshPro.text = layerName;
                edgeLabels.Add(textInstance.transform);
                textMeshPro.ForceMeshUpdate(true, true);

                // collect positional information for text
                Vector3[] positions = new Vector3[bezierCurve.lineRenderer.positionCount];
                bezierCurve.lineRenderer.GetPositions(positions);
                int idx = (int)Mathf.Floor(edgeTextPosition * (float)positions.Length) - 1;
                Vector3 point1 = positions[idx];
                Vector3 tangent = positions[idx + 1] - point1;
                tangent = tangent.normalized;
                Vector3 orthogonal = new Vector3(tangent.z, tangent.y, -tangent.x);
                
                //set text transform
                if (textMeshPro.text != "")
                {
                    float preferredWidth = 0.75f * (positions[positions.Length - 1] - point1).magnitude;
                    float scale = preferredWidth / textMeshPro.bounds.size.x;
                    textInstance.transform.localScale = new Vector3(scale, scale, scale);
                    float sign_is_right = 1f;
                    if (point1.x <= 0)
                    {
                        sign_is_right = -1f;
                    }
                    textInstance.transform.localPosition = point1 + 1f * textMeshPro.bounds.size.y * scale * orthogonal * sign_is_right;
                    float tangent_points_right = 1f;
                    if (tangent.x < 0)
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


    public void UpdateAllLayers()
    {
        for (int i = 0; i < architecture.Count; i++)
        {
            RequestLayerActivation(i);
        }
        RequestClassificationResult();
    }


    private void Start()
    {
        _dlClient = GetComponent<DLClient>();
        _dlClient.Prepare();
        Debug.Log("Begin RequestDataOverview");
        RequestDataOverview();
        Debug.Log("Begin RequestNetworkArchitecture");
        RequestNetworkArchitecture();
    }

}
