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
    public GameObject imageFramePrefab;
    public GameObject textPrefab;
    public GameObject bezierStaticPrefab;
    public Transform imagePickerCanvasContent;
    public GameObject imageGetterButtonPrefab;
    public GameObject networkInfoScreenPrefab;
    private GameObject classificationResultTextInstance;
    public XRBaseInteractor rightInteractor;
    public XRBaseInteractor leftInteractor;
    public Transform resultCanvasContent;
    public GameObject resultCanvasContentElementPrefab;
    public float minimalZOffset = 0.5f;
    public float FeatMap2DOffsetZMultiplier = 0.75f;
    public float FeatMap1DOffsetZMultiplier = 0f;
    public float InfoScreenOffsetZMultiplier = 1f;
    public int nPointsInBezier = 20;
    public float edgeTextPosition = 0.7f;
    private DLClient _dlClient;
    private int nClasses = 0;
    private int lenDataset = 0;
    private string imageType = "rbg";
    private int nLayers = 0;
    private JArray classNames;
    private List<JObject> architecture;
    private List<GameObject> layers = new List<GameObject>();
    Dictionary<(int pos0, int pos1), int> pos_to_layer_ind = new Dictionary<(int pos0, int pos1), int>();


    public void RequestNetworkArchitecture()
    {
        _dlClient.RequestNetworkArchitecture();
    }

    public IEnumerator AcceptNetworkArchitecture(List<JObject> jObjectList)
    {
        Debug.Log("Received AcceptNetworkArchitecture");
        /*foreach (JObject jObject in jObjectList)
        {
            Debug.Log(jObject);
        }*/
        architecture = jObjectList;
        Debug.Log("Create Layers");
        CreateLayers();
        nLayers = layers.Count;
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
        nClasses = (int)jObject["n_classes"];
        lenDataset = (int)jObject["len"];
        imageType = (string)jObject["type"];
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
        
        //textMeshPro.ForceMeshUpdate(true, true);
        //float buttonHeight = newImageGetterButton.GetComponentInParent<GridLayoutGroup>().cellSize.y;
        //float textHeight = textMeshPro.bounds.size.y;
        //float scale = 0.9f * buttonWidth / textWidth;
        //textMesh.transform.localScale = new Vector3(scale, scale, scale);
        //textMesh.transform.Translate (new Vector3(0f, -buttonHeight, 0f));
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
        layers[layerID].GetComponent<NetLayer>().updateData(textureList, transform.localScale[0], zeroValue);
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


        // Destroy old text instance
        if (classificationResultTextInstance != null)
        {
            Destroy(classificationResultTextInstance);
        }
        // create text for classification result
        classificationResultTextInstance = Instantiate(textPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f), layers[layers.Count - 1].transform);
        classificationResultTextInstance.name = "ClassificationResultText";
        TextMeshPro textMeshPro = classificationResultTextInstance.GetComponent<TextMeshPro>();
        textMeshPro.text = (string)classNames[(int)jObject["class_indices"][0]];
        textMeshPro.ForceMeshUpdate(true, true);

        // position and scale the text
        Layer1D layer = layers[layers.Count - 1].GetComponent<Layer1D>();
        float layerWidth = layer.width;
        float textWidth = textMeshPro.bounds.size.x;
        float scale = 0.5f * layerWidth / textWidth;
        classificationResultTextInstance.transform.localScale = new Vector3(scale, scale, scale);
        classificationResultTextInstance.transform.localPosition = new Vector3(0f, 1.5f * textMeshPro.bounds.size.y * scale, 0f);
        yield return null;
    }


    public void CreateLayers()
    {
        GameObject imageFrameInstance = Instantiate(imageFramePrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f), transform);
        imageFrameInstance.transform.localScale = new Vector3(30f, 30f, 30f);
        imageFrameInstance.transform.localPosition = new Vector3(0f, 0f, 0f);
        imageFrameInstance.name = "Network Image Input Frame";
        imageFrameInstance.GetComponent<NetworkImageInputFrame>().Prepare(this);

        GameObject netLayer = new GameObject();
        List<GameObject> tmp_layers_in_this_row = new List<GameObject>();
        float offset_x = 0f;
        float offset_z = 0f;
        float lastNetLayerWidth = 0f;
        float lastStageMaxWidth = 0f;
        float thisStageMaxWidth = 0f;
        float margin = 0f;
        // initialize z offset and such that first stage is placed correctly 
        float thisStageOffsetZMultiplier = 0f;
        float lastStageOffsetZMultiplier = 0f;
        float lastStageZ = minimalZOffset;


        void resetForNewStage()
        {
            // shift layers horizontally
            centerLayers(tmp_layers_in_this_row);
            // shift layer in z direction
            offset_z = Mathf.Max(minimalZOffset, lastStageOffsetZMultiplier * lastStageMaxWidth);
            shiftzLayers(tmp_layers_in_this_row, lastStageZ + offset_z);
            // update or reset variables for the positionining of subsequent layers
            lastStageOffsetZMultiplier = thisStageOffsetZMultiplier;
            lastStageZ = lastStageZ + offset_z;
            lastStageMaxWidth = thisStageMaxWidth;
            thisStageMaxWidth = 0f;
            offset_x = 0f;
            lastNetLayerWidth = 0f;
            margin = 0f;
            tmp_layers_in_this_row = new List<GameObject>();
        }


        foreach (JObject jObject in architecture)
        {
            Vector2Int pos = new Vector2Int((int)jObject["pos"][0], (int)jObject["pos"][1]);
            GameObject newLayerInstance = null;
            
            if (pos[1] == 0)
            {
                // About to begin new stage: center the layers and reset variables.
                resetForNewStage();
            }

            string datatype = (string)jObject["data_type"];
            if (datatype == "2D_feature_map")
            {
                Vector3Int size = new Vector3Int((int)jObject["size"][1], (int)jObject["size"][2], (int)jObject["size"][3]);
                newLayerInstance = (GameObject)Instantiate(netLayer, Vector3.zero, transform.rotation, transform);
                newLayerInstance.transform.localPosition = new Vector3(offset_x, 0f, 0f);
                newLayerInstance.name = "2D_feature_map_layer " + string.Format("{0}", pos[0]) + "," + string.Format("{0}", pos[1]);
                newLayerInstance.AddComponent<Layer2D>();
                Layer2D netLayerScript = newLayerInstance.GetComponent<Layer2D>();
                netLayerScript.Prepare(node2DPrefab, pos, (string)jObject["layer_name"], size);
                margin = Mathf.Max(lastNetLayerWidth, netLayerScript.width);
                offset_x = offset_x + netLayerScript.width + margin;
                lastNetLayerWidth = netLayerScript.width;
                thisStageMaxWidth = Mathf.Max(netLayerScript.width, thisStageMaxWidth);
                thisStageOffsetZMultiplier = FeatMap2DOffsetZMultiplier;
            }
            else if (datatype == "1D_vector")
            {
                int size = (int)jObject["size"][1];
                newLayerInstance = (GameObject)Instantiate(netLayer, Vector3.zero, transform.rotation, transform);
                newLayerInstance.transform.localPosition = new Vector3(offset_x, 0f, 0f);
                newLayerInstance.name = "1D_vector_layer " + string.Format("{0}", pos[0]) + "," + string.Format("{0}", pos[1]);
                newLayerInstance.AddComponent<Layer1D>();
                Layer1D netLayerScript = newLayerInstance.GetComponent<Layer1D>();
                netLayerScript.Prepare(layer1DParticleSystemPrefab, pos, (string)jObject["layer_name"], size);
                margin = Mathf.Max(lastNetLayerWidth, netLayerScript.width);
                offset_x = offset_x + netLayerScript.width + margin;
                lastNetLayerWidth = netLayerScript.width;
                thisStageMaxWidth = Mathf.Max(netLayerScript.width, thisStageMaxWidth);
                thisStageOffsetZMultiplier = FeatMap1DOffsetZMultiplier;
            }
            else if (datatype == "None")
            {
                newLayerInstance = (GameObject)Instantiate(networkInfoScreenPrefab, Vector3.zero, transform.rotation, transform);
                newLayerInstance.transform.localPosition = new Vector3(offset_x, 0.5f * offset_z, 0f);
                newLayerInstance.name = "Info Screen " + string.Format("{0}", pos[0]) + "," + string.Format("{0}", pos[1]);
                Transform textInstance = newLayerInstance.transform.Find("TextMesh");
                TextMeshPro textMeshPro = textInstance.GetComponent<TextMeshPro>();
                textMeshPro.text = (string)jObject["layer_name"];
                textMeshPro.ForceMeshUpdate(true, true);
                float textWidth = Mathf.Max(minimalZOffset, textMeshPro.bounds.size.x);
                float width = offset_z;
                float scale = width / textWidth;
                textInstance.transform.localScale = new Vector3(scale, scale, scale);
                margin = Mathf.Max(lastNetLayerWidth, width);
                lastNetLayerWidth = width;
                offset_x = offset_x + width + margin;
                thisStageMaxWidth = Mathf.Max(width, thisStageMaxWidth);
                thisStageOffsetZMultiplier = InfoScreenOffsetZMultiplier;
            }
            else
            {
                Debug.LogError("Could not identify network layer data type", transform);
            }

            pos_to_layer_ind.Add((pos[0], pos[1]), layers.Count); 
            layers.Add(newLayerInstance);
            tmp_layers_in_this_row.Add(newLayerInstance);
        }

        resetForNewStage();
        drawNetworkEdges();
        Destroy(netLayer);
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

                // determine edge points
                newBezierStaticInstance.transform.localPosition = Vector3.zero;
                Vector3 pos_new_layer = layers[layer_id].transform.localPosition;//.localToWorldMatrix.MultiplyPoint3x4(layers[layer_id].GetComponent<NetLayer>().center_pos);
                //pos_new_layer = transform.worldToLocalMatrix.MultiplyPoint3x4(pos_new_layer);
                GameObject precursor = layers[pos_to_layer_ind[((int)token[0], (int)token[1])]];
                Vector3 pos_precursor = precursor.transform.localPosition;//.localToWorldMatrix.MultiplyPoint3x4(precursor.GetComponent<NetLayer>().center_pos);
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
                    float preferredWidth = 1f * (positions[positions.Length - 1] - point1).magnitude;
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


    private void centerLayers(List<GameObject> tmp_layers_in_this_row)
    {
        float meanX = 0f;
        foreach (GameObject layer in tmp_layers_in_this_row)
        {
            meanX += layer.transform.localPosition.x;
        }
        meanX = meanX / (float)tmp_layers_in_this_row.Count;
        foreach (GameObject layer in tmp_layers_in_this_row)
        {
            layer.transform.localPosition = layer.transform.localPosition - new Vector3(meanX, 0f, 0f);
        }
    }


    private void shiftzLayers(List<GameObject> tmp_layers_in_this_row, float offset_z)
    {
        foreach (GameObject layer in tmp_layers_in_this_row)
        {
            layer.transform.localPosition = layer.transform.localPosition + new Vector3(0f, 0f, offset_z);
        }
    }


    public void UpdateAllLayers()
    {
        for (int i = 0; i < nLayers; i++)
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
