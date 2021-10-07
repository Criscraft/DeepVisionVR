using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using System.Text;

public class DLWebClient : MonoBehaviour
{
    [SerializeField]
    private string url = "http://127.0.0.1:5570/";
    [SerializeField]
    private GameObject dlNetworkPrefab;
    [SerializeField]
    private GameObject datasetPrefab;
    [SerializeField]
    private GameObject noiseGeneratorPrefab;
    [SerializeField]
    private float networkSpacing = 25f;
    

    private List<DLNetwork> dlNetworkList;
    public delegate IEnumerator HandleJSONDelegate(JObject jObject);
    public delegate IEnumerator HandleUploadDelegate();


    private IEnumerator GetJSON(string resource, HandleJSONDelegate handleJSONDelegate)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url + resource))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                Debug.Log(url + resource);
            }
            else
            {
                Debug.Log("Received: " + url + resource);
                JObject jObject = JObject.Parse(www.downloadHandler.text);
                UnityMainThreadDispatcher.Instance().Enqueue(handleJSONDelegate(jObject));
            }
        }
    }


    private IEnumerator Upload(string resource, string dataString, HandleUploadDelegate HandleUploadDelegate)
    {
        byte[] data = Encoding.UTF8.GetBytes(dataString);
        using (UnityWebRequest www = UnityWebRequest.Put(url + resource, data))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                Debug.Log("Upload Complete: " + url + resource);
                UnityMainThreadDispatcher.Instance().Enqueue(HandleUploadDelegate());
            }
        }
    }


    public IEnumerator DoNothing()
    {
        yield return null;
    }


    public void RequestNetworkArchitecture(HandleJSONDelegate handleJSONDelegate, int networkID)
    {
        StartCoroutine(GetJSON(string.Format("network/{0}", networkID), handleJSONDelegate));
    }


    public void RequestLayerActivation(HandleJSONDelegate handleJSONDelegate, int networkID, int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/{0}/activation/layerid/{1}", networkID, layerID), handleJSONDelegate));
    }


    public void RequestLayerFeatureVisualization(HandleJSONDelegate handleJSONDelegate, int networkID, int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/{0}/featurevisualization/layerid/{1}", networkID, layerID), handleJSONDelegate));
    }


    public void RequestAllFeatureVisualizations(int networkID) 
    {
        dlNetworkList[networkID].RequestAllFeatureVisualizations();
    }


    public void RequestWeightHistogram(HandleJSONDelegate handleJSONDelegate, int networkID, int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/{0}/weighthistogram/layerid/{1}", networkID, layerID), handleJSONDelegate));
    }


    public void RequestActivationHistogram(HandleJSONDelegate handleJSONDelegate, int networkID, int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/{0}/activationhistogram/layerid/{1}", networkID, layerID), handleJSONDelegate));
    }


    public void RequestPrepareForInput(HandleUploadDelegate handleUploadDelegate, int networkID, ActivationImage activationImage)
    {
        if (activationImage.mode == ActivationImage.Mode.Activation) return;

        ActivationImage activationImageShallowCopy = activationImage;
        activationImageShallowCopy.tex = null;
        string output = JsonConvert.SerializeObject(activationImageShallowCopy);
        StartCoroutine(Upload(string.Format("network/{0}/prepareforinput", networkID), output, handleUploadDelegate));
    }


    // server side export, currently unused
    public void RequestLayerExport(int networkID, int layerID, ActivationImage activationImage)
    {
        ActivationImage activationImageShallowCopy = activationImage;
        activationImageShallowCopy.tex = null;
        string output = JsonConvert.SerializeObject(activationImageShallowCopy);
        StartCoroutine(Upload(string.Format("network/{0}/export/layerid/{1}", networkID, layerID), output, DoNothing));
    }


    public void RequestClassificationResults(HandleJSONDelegate handleJSONDelegate, int networkID)
    {
        StartCoroutine(GetJSON(string.Format("network/{0}/classificationresult", networkID), handleJSONDelegate));
    }


    public void RequestDatasetImages(HandleJSONDelegate handleJSONDelegate, int datasetID)
    {
        StartCoroutine(GetJSON(string.Format("dataset/{0}/images", datasetID), handleJSONDelegate));
    }


    public void RequestNoiseImage(HandleJSONDelegate handleJSONDelegate, int noiseID)
    {
        StartCoroutine(GetJSON(string.Format("noiseimage/{0}", noiseID), handleJSONDelegate));
    }


    public void RequestBasicInfo()
    {
        StartCoroutine(GetJSON("network", AcceptBasicInfo));
    }


    public void SetNetworkGenFeatVis(int networkID)
    {
        string output = "dummy";
        StartCoroutine(Upload(string.Format("network/{0}/setnetworkgenfeatvis", networkID), output, DoNothing));
    }


    public void SetNetworkLoadFeatVis(int networkID)
    {
        string output = "dummy";
        StartCoroutine(Upload(string.Format("network/{0}/setnetworkloadfeatvis", networkID), output, DoNothing));
    }


    public void SetNetworkDeleteFeatVis(int networkID)
    {
        string output = "dummy";
        StartCoroutine(Upload(string.Format("network/{0}/setnetworkdeletefeatvis", networkID), output, DoNothing));
    }


    public IEnumerator AcceptBasicInfo(JObject jObject)
    {
        int Nnetworks = (int)jObject["nnetworks"];
        int Ndatasets = (int)jObject["ndatasets"];
        int NnoiseGenerators = (int)jObject["nnoiseGenerators"];
        DLNetwork dlNetwork;
        FeatureVisSettingsButtons featureVisSettingsButtons;
        Dataset dataset;
        dlNetworkList = new List<DLNetwork>();

        for (int i = 0; i < Nnetworks; i++)
        {
            Transform newInstance = Instantiate(dlNetworkPrefab).transform;
            newInstance.name = string.Format("Network{0}", i);
            newInstance.localPosition = new Vector3(networkSpacing * i, 0f, 0f);
            newInstance.localRotation = Quaternion.identity;
            newInstance.localScale = new Vector3(1f, 1f, 1f);
            newInstance.SetParent(transform);
            dlNetwork = newInstance.GetComponentInChildren<DLNetwork>();
            dlNetwork.Prepare(this, i);
            featureVisSettingsButtons = newInstance.GetComponentInChildren<FeatureVisSettingsButtons>();
            featureVisSettingsButtons.Prepare(this, i);
            dlNetwork.BuildNetwork();
            dlNetworkList.Add(dlNetwork);
        }

        for (int i = 0; i < Ndatasets; i++)
        {
            Transform newInstance = Instantiate(datasetPrefab).transform;
            newInstance.name = string.Format("Dataset{0}", i);
            newInstance.localPosition = new Vector3(networkSpacing * i, 0f, -12f);
            newInstance.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
            newInstance.localScale = new Vector3(0.007f, 0.007f, 0.007f);
            newInstance.SetParent(transform);
            dataset = newInstance.GetComponent<Dataset>();
            dataset.Prepare(this, i);
            dataset.BuildDataset();
        }


        for (int i = 0; i < NnoiseGenerators; i++)
        {
            Transform newInstance = Instantiate(noiseGeneratorPrefab).transform;
            newInstance.name = string.Format("NoiseGenerator{0}", i);
            newInstance.localPosition = new Vector3(-5f, 0f, -6f + 3f * i);
            newInstance.localRotation = Quaternion.Euler(new Vector3(0f, -90f, 0f));
            newInstance.localScale = new Vector3(0.006f, 0.006f, 0.006f);
            newInstance.SetParent(transform);
            newInstance.GetComponentInChildren<NoiseGenerateButton>().Prepare(this, i);
        }

        yield return null;
    }


    public static Texture2D StringToTex(string textureString)
    {
        byte[] b64_bytes = System.Convert.FromBase64String(textureString);
        Texture2D tex = new Texture2D(1, 1);
        if (ImageConversion.LoadImage(tex, b64_bytes))
        {
            tex.filterMode = FilterMode.Point;
            return tex;
        }
        else
        {
            Debug.Log("Texture could not be loaded");
            return null;
        }
    }


    private void Start()
    {
        RequestBasicInfo();
    }
}