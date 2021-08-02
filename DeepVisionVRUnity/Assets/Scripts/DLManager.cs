using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;
using UnityEngine.XR.Interaction.Toolkit;

public class DLManager : MonoBehaviour
{
    // prefabs
    public GameObject node2DPrefab;
    public GameObject layer1DParticleSystemPrefab;
    public GameObject networkImageInputFramePrefab;
    public GameObject textPrefab;
    public GameObject bezierStaticPrefab;
    public GameObject imageGetterButtonPrefab;
    public GameObject networkInfoScreenPrefab;
    public GameObject resultCanvasContentElementPrefab;

    // stored references
    public Transform imagePickerCanvasContent;
    public Transform resultCanvasContent;
    public Transform foreignResultCanvasInstance;
    private Transform ownResultCanvasInstance;
    public XRBaseInteractor rightInteractor;
    public XRBaseInteractor leftInteractor;
    private Transform networkImageInputFrameInstance;

    // network data
    private DLClient _dlClient;
    private int lenDataset = 0;
    private JArray classNames;
    private List<JObject> architecture;
    private Transform[,] gridLayerElements; // Z , X 
    private Dictionary<int, int[]> layerIDToGridPosition = new Dictionary<int, int[]>(); // Z size, X size

    // layout general
    public LayoutParams.LayoutMode layoutMode;
    public float xMargin = 0.75f;
    public float minElementSize = 0.75f;
    public float minimalInfoScreenSize = 0.75f;
    private int[] gridSize = { 0, 0 }; // the number stages and lanes in grid coordinates (Z size, X size)
    private NetworkLayouts layouts;

    // linear layout
    public bool xCentering = true;
    public bool xStrictGridPlacement = false;
    public float minimalZOffset = 0.75f;
    public float maximalZOffset = 10f;

    // spiral layout
    [SerializeField]
    private float _spiralLayout_theta_0;
    public float spiralLayout_theta_0
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
    public float spiralLayout_b
    {
        get { return _spiralLayout_b; }
        set
        {
            _spiralLayout_b = value;
            ApplyLayout();
        }
    }

    // edges
    public int nPointsInBezier = 20;
    public float edgeTextPosition = 0.7f;
    private List<Transform> edges = new List<Transform>();
    private List<Transform> edgeLabels = new List<Transform>();
    

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

        InitializeLayout();
        ApplyLayout();
        Destroy(netLayer);
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
        for (int i = 0; i < architecture.Count; i++)
        {
            RequestLayerActivation(i);
        }
        RequestClassificationResult();
    }


    private void Start()
    {
        layouts = new NetworkLayouts();
        _dlClient = GetComponent<DLClient>();
        _dlClient.Prepare();
        Debug.Log("Begin RequestDataOverview");
        RequestDataOverview();
        Debug.Log("Begin RequestNetworkArchitecture");
        RequestNetworkArchitecture();
    }

}
