using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;

public class DLNetwork : MonoBehaviour
{
    // prefabs
    [SerializeField]
    private GameObject layerCanvasPrefab;
    [SerializeField]
    private GameObject layer1DParticleSystemPrefab;
    [SerializeField]
    private GameObject networkImageInputFramePrefab;
    [SerializeField]
    private GameObject textPrefab;
    [SerializeField]
    private GameObject bezierStaticPrefab;
    [SerializeField]
    private GameObject imageGetterButtonPrefab;
    [SerializeField]
    private GameObject networkInfoScreenPrefab;
    [SerializeField]
    private GameObject resultCanvasContentElementPrefab;

    // stored references
    [SerializeField]
    private Transform resultCanvasContent;
    [SerializeField]
    private Transform foreignResultCanvasInstance;
    private Transform ownResultCanvasInstance;
    private Transform networkImageInputFrameInstance;
    [SerializeField]
    private Canvas resultCanvas;
    [SerializeField]
    private Canvas networkSettingsCanvas;

    // network data
    private DLWebClient dlClient;
    private int networkID = -1;
    private JArray classNames;
    private JArray architecture;
    private Transform[,] gridLayerElements; // Z , X 
    private Dictionary<int, int[]> layerIDToGridPosition = new Dictionary<int, int[]>(); // Z size, X size
    [SerializeField]
    private bool showWeightHistograms = true;
    [SerializeField]
    private bool showActivationHistograms = true;

    // layout general
    [SerializeField]
    private LayoutParams.LayoutMode layoutMode;
    [SerializeField]
    private float xMargin = 0.75f;
    [SerializeField]
    private float minElementSize = 0.75f;
    [SerializeField]
    private float minimalInfoScreenSize = 0.75f;
    private int[] gridSize = { 0, 0 }; // the number stages and lanes in grid coordinates (Z size, X size)
    private NetworkLayouts layouts;

    // linear layout
    [SerializeField]
    private bool xCentering = true;
    [SerializeField]
    private bool xStrictGridPlacement = false;
    [SerializeField]
    private float minimalZOffset = 0.75f;
    [SerializeField]
    private float maximalZOffset = 10f;

    // spiral layout
    [SerializeField]
    private float _spiralLayout_theta_0;
    [SerializeField]
    private float spiralLayout_theta_0
    {
        get {return _spiralLayout_theta_0; }
        set 
        {
            _spiralLayout_theta_0 = value;
            ApplyLayout();
        }
    }
    [SerializeField]
    private float _spiralLayout_b;
    [SerializeField]
    private float spiralLayout_b
    {
        get { return _spiralLayout_b; }
        set
        {
            _spiralLayout_b = value;
            ApplyLayout();
        }
    }

    // edges
    [SerializeField]
    private int nPointsInBezier = 20;
    [SerializeField]
    private float edgeTextPosition = 0.7f;
    [SerializeField]
    private float maxEdgeLabelSize = 1f;
    private List<Transform> edges = new List<Transform>();
    private List<Transform> edgeLabels = new List<Transform>();


    public void RequestNetworkArchitecture()
    {
        dlClient.RequestNetworkArchitecture(AcceptNetworkArchitecture, networkID);
    }


    public IEnumerator AcceptNetworkArchitecture(JObject jObject)
    {
        architecture = (JArray)jObject["architecture"];

        // find grid size
        int posX = 0;
        int posZ = 0;
        foreach (JToken token in architecture)
        {
            posZ = (int)token["pos"][0];
            if (gridSize[0] < posZ) gridSize[0] = posZ; 
            posX = (int)token["pos"][1];
            if (gridSize[1] < posX) gridSize[1] = posX; 
        }
        gridSize[0] = gridSize[0] + 1;
        gridSize[1] = gridSize[1] + 1;
        
        CreateLayers();
        
        UpdateAllLayers();
        if (showWeightHistograms)
        {
            for (int i = 0; i < architecture.Count; i++)
            {
                RequestWeightHistogram(i);
            }
        }
        yield return null;
    }


    public void RequestLayerActivation(int layerID)
    {
        string datatype = (string)architecture[layerID]["data_type"];
        if (datatype == "2D_feature_map" || datatype == "1D_vector")
        {
            dlClient.RequestLayerActivation(AcceptLayerActivation, networkID, layerID);
        }
    }


    public void RequestLayerFeatureVisualization(int layerID)
    {
        string datatype = (string)architecture[layerID]["data_type"];
        if (datatype == "2D_feature_map" || datatype == "1D_vector")
        {
            dlClient.RequestLayerFeatureVisualization(AcceptLayerActivation, networkID, layerID);
            SetLoading(layerID);
        }
    }


    public void RequestAllFeatureVisualizations() 
    {
        for (int i = 0; i < architecture.Count; i++)
            {
                RequestLayerFeatureVisualization(i);
            }
    }
    

    public IEnumerator AcceptLayerActivation(JObject jObject)
    {
        int layerID = (int)jObject["layerID"];
        ActivationImage.Mode mode = (ActivationImage.Mode) Enum.Parse(typeof(ActivationImage.Mode), (string)jObject["mode"]);
        float zeroValue = 0f;
        if (jObject["zeroValue"] != null) zeroValue = (float)jObject["zeroValue"];

        bool isRGB = false;
        if (mode == ActivationImage.Mode.Activation) isRGB = false;
        else if (mode == ActivationImage.Mode.FeatureVisualization) isRGB = true;

        ActivationImage activationImage;
        List <ActivationImage> activationImageList = new List<ActivationImage>();
        
        JArray texArray = (JArray)jObject["tensors"];
        for (int i=0; i<texArray.Count; i++) 
        {
            activationImage = new ActivationImage();
            activationImage.networkID = networkID;
            activationImage.layerID = layerID;
            activationImage.channelID = i;
            activationImage.isRGB = isRGB;
            activationImage.mode = mode;
            activationImage.tex = DLWebClient.StringToTex((string)texArray[i]);
            activationImage.zeroValue = zeroValue;
            activationImageList.Add(activationImage);
        }

        var pos = layerIDToGridPosition[layerID];
        gridLayerElements[pos[0], pos[1]].GetComponent<NetLayer>().UpdateData(activationImageList, transform.localScale[0]);
        yield return null;
    }


    public void RequestWeightHistogram(int layerID)
    {
        string datatype = (string)architecture[layerID]["data_type"];
        if (datatype == "2D_feature_map")
        {
            dlClient.RequestWeightHistogram(AcceptWeightHistogram, networkID, layerID);
        }
    }

    public IEnumerator AcceptWeightHistogram(JObject jObject)
    {
        int layerID = (int)jObject["layer_id"];
        float[] counts;
        float[] bins;
        if ((string)jObject["has_weights"] == "True")
        {
            counts = jObject["counts"].ToObject<float[]>();
            bins = jObject["bins"].ToObject<float[]>();
            var pos = layerIDToGridPosition[layerID];
            gridLayerElements[pos[0], pos[1]].GetComponent<Layer2D>().SetWeightHistogramData(counts, bins);
        }
        yield return null;
    }


    public void RequestActivationHistogram(int layerID)
    {
        string datatype = (string)architecture[layerID]["data_type"];
        if (datatype == "2D_feature_map")
        {
            dlClient.RequestActivationHistogram(AcceptActivationHistogram, networkID, layerID);
        }
    }

    public IEnumerator AcceptActivationHistogram(JObject jObject)
    {
        int layerID = (int)jObject["layer_id"];
        float[] counts;
        float[] bins;
        counts = jObject["counts"].ToObject<float[]>();
        bins = jObject["bins"].ToObject<float[]>();
        var pos = layerIDToGridPosition[layerID];
        gridLayerElements[pos[0], pos[1]].GetComponent<Layer2D>().SetActivationHistogramData(counts, bins);
        yield return null;
    }


    public void RequestPrepareForInput(ActivationImage activationImage)
    {
        dlClient.RequestPrepareForInput(AcceptPrepareForInput, networkID, activationImage);
    }

    public IEnumerator AcceptPrepareForInput()
    {
        UpdateAllLayers();
        yield return null;
    }


    public void RequestClassificationResults()
    {
        dlClient.RequestClassificationResults(AcceptClassificationResults, networkID);
    }


    public IEnumerator AcceptClassificationResults(JObject jObject)
    {
        // Remove old classification Results
        var children = new List<GameObject>();
        foreach (Transform child in resultCanvasContent) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        // fill the foreign result canvas
        JArray classNames = (JArray) jObject["class_names"];
        JArray confidenceValues = (JArray) jObject["confidence_values"];
        for (int i = 0; i < classNames.Count; i++)
        {
            GameObject newResultCanvasContentElement = Instantiate(resultCanvasContentElementPrefab, Vector3.zero, Quaternion.identity);
            newResultCanvasContentElement.name = "ResultField " + (string)classNames[i];
            newResultCanvasContentElement.transform.SetParent(resultCanvasContent, false);
            TextMeshProUGUI textMeshProUGIO = newResultCanvasContentElement.GetComponentInChildren<TextMeshProUGUI>();
            textMeshProUGIO.text = (string)classNames[i] + " - " + string.Format("{0}%", (float)confidenceValues[i]);
        }

        // Destroy old own canvas instance
        if (ownResultCanvasInstance != null)
        {
            Destroy(ownResultCanvasInstance.gameObject);
            ownResultCanvasInstance = null;
        }
        
        StartCoroutine(CreateOwnResultCanvasInstance());
        
        yield return null;
    }


    private IEnumerator CreateOwnResultCanvasInstance()
    {
        // wait for one frame
        yield return null;
        // create new own canvas instance
        var pos = layerIDToGridPosition[architecture.Count - 1];
        Transform lastLayer = gridLayerElements[pos[0], pos[1]];
        ownResultCanvasInstance = Instantiate(foreignResultCanvasInstance);
        ownResultCanvasInstance.SetParent(lastLayer);
        ownResultCanvasInstance.name = "ClassificationResultCanvas";
        ownResultCanvasInstance.localScale = new Vector3(3f, 3f, 3f);
        ownResultCanvasInstance.localRotation = Quaternion.identity;
        ownResultCanvasInstance.localPosition = new Vector3(0f, 0f, 0f);
    }


    public void CreateLayers()
    {
        // create image frame
        networkImageInputFrameInstance = Instantiate(networkImageInputFramePrefab).transform;
        networkImageInputFrameInstance.SetParent(transform);
        networkImageInputFrameInstance.localScale = new Vector3(3f, 3f, 3f);
        networkImageInputFrameInstance.localPosition = new Vector3(0f, 0f, -minimalZOffset);
        networkImageInputFrameInstance.localRotation = Quaternion.identity;
        networkImageInputFrameInstance.name = "Network Image Input Frame";
        networkImageInputFrameInstance.GetComponent<NetworkImageInputFrame>().Prepare(this);

        // create network layers without positioning or scaling them
        gridLayerElements = new Transform[gridSize[0], gridSize[1]];
        
        GameObject newLayerInstance = null;
        int layerID = 0;

        foreach (JToken jObject in architecture)
        {
            int[] gridPos = new int[2] {(int)jObject["pos"][0], (int)jObject["pos"][1]};
            string datatype = (string)jObject["data_type"];
            
            if (datatype == "2D_feature_map")
            {
                Vector3Int size = new Vector3Int((int)jObject["size"][1], (int)jObject["size"][2], (int)jObject["size"][3]);
                newLayerInstance = (GameObject)Instantiate(layerCanvasPrefab);
                newLayerInstance.transform.SetParent(transform);
                newLayerInstance.transform.localPosition = Vector3.zero;
                newLayerInstance.transform.localRotation = transform.localRotation;
                newLayerInstance.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);
                newLayerInstance.name = "2D_feature_map_layer " + string.Format("{0}", gridPos[0]) + "," + string.Format("{0}", gridPos[1]);
                newLayerInstance.GetComponent<Layer2D>().Prepare(size, this, networkID, layerID);
            }
            else if (datatype == "1D_vector")
            {
                Vector3Int size = new Vector3Int((int)jObject["size"][1], 1, 1);
                newLayerInstance = (GameObject)Instantiate(layerCanvasPrefab);
                newLayerInstance.transform.SetParent(transform);
                newLayerInstance.transform.localPosition = Vector3.zero;
                newLayerInstance.transform.localRotation = transform.localRotation;
                newLayerInstance.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);
                newLayerInstance.name = "1D_vector_layer " + string.Format("{0}", gridPos[0]) + "," + string.Format("{0}", gridPos[1]);
                newLayerInstance.GetComponent<Layer2D>().Prepare(size, this, networkID, layerID);
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

        InitializeLayout();
        ApplyLayout();
    }
        

    private LayoutParams GetLayoutParams()
    {
        LayoutParams layoutParams = new LayoutParams();
        layoutParams.layoutMode = layoutMode;
        layoutParams.xMargin = xMargin;
        layoutParams.minElementSize = minElementSize;
        layoutParams.minimalInfoScreenSize = minimalInfoScreenSize;

        if (layoutMode == LayoutParams.LayoutMode.linearLayout)
        {
            layoutParams.xCentering = xCentering;
            layoutParams.xStrictGridPlacement = xStrictGridPlacement;
            layoutParams.minimalZOffset = minimalZOffset;
            layoutParams.maximalZOffset = maximalZOffset;
        }
        else if (layoutMode == LayoutParams.LayoutMode.spiralLayout)
        {
            layoutParams.theta_0 = spiralLayout_theta_0;
            layoutParams.b = spiralLayout_b;
        }

        layoutParams.nPointsInBezier = nPointsInBezier;
        layoutParams.edgeTextPosition = edgeTextPosition;
        layoutParams.maxEdgeLabelSize = maxEdgeLabelSize;
        return layoutParams;
    }


    private void InitializeLayout()
    {
        LayoutParams layoutParams = GetLayoutParams();
        layouts.InitializeLayout(gridLayerElements, gridSize, layoutParams);
    }


    private void ApplyLayout()
    {
        LayoutParams layoutParams = GetLayoutParams();
        layouts.ApplyLayout(gridLayerElements, gridSize, layoutParams);
        layouts.DrawNetworkEdges(architecture, bezierStaticPrefab, transform, edges, edgeLabels, layerIDToGridPosition, gridLayerElements, textPrefab, layoutParams);
    }


    public void UpdateAllLayers()
    {
        // update network activations and activation histograms
        for (int i = 0; i < architecture.Count; i++)
        {
            RequestLayerActivation(i);
        }
        RequestClassificationResults();

        if (showActivationHistograms)
        {
            for (int i = 0; i < architecture.Count; i++)
            {
                RequestActivationHistogram(i);
            }
        }
    }


    public void SetLoading(int layerID)
    {
        var pos = layerIDToGridPosition[layerID];
        gridLayerElements[pos[0], pos[1]].GetComponent<Layer2D>().EnableReloadOverlay();
    }


    public void Prepare(DLWebClient _dlClient, int _networkID)
    {
        dlClient = _dlClient;
        networkID = _networkID;
    }


    public void BuildNetwork()
    {
        layouts = new NetworkLayouts();
        Debug.Log("Begin RequestNetworkArchitecture");
        RequestNetworkArchitecture();
    }

}
