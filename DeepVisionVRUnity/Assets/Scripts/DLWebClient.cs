using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using UnityEngine.Networking;
using System.Text;

public class DLWebClient : MonoBehaviour
{
    [SerializeField]
    private string url = "http://127.0.0.1:5570/";
    [SerializeField]
    private DLManager dlManager;
    private delegate IEnumerator HandleJSONDelegate(JObject jObject);
    private delegate IEnumerator HandleUploadDelegate();


    private IEnumerator GetJSON(string resource, HandleJSONDelegate handleJSONDelegate)
    {
        UnityWebRequest www = UnityWebRequest.Get(url + resource);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            Debug.Log(url + resource);
        }
        else
        {
            Debug.Log("Received");
            Debug.Log(url + resource);
            JObject jObject = JObject.Parse(www.downloadHandler.text);
            UnityMainThreadDispatcher.Instance().Enqueue(handleJSONDelegate(jObject));
        }
    }


    private IEnumerator Upload(string resource, string dataString, HandleUploadDelegate HandleUploadDelegate)
    {
        byte[] data = Encoding.UTF8.GetBytes(dataString);
        UnityWebRequest www = UnityWebRequest.Put(url + resource, data);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Show results as text
            Debug.Log("Upload Complete!");
            Debug.Log(url + resource);
            UnityMainThreadDispatcher.Instance().Enqueue(HandleUploadDelegate());
        }
    }


    public IEnumerator AcceptNetworkArchitecture(JObject jObject)
    {
        dlManager.AcceptNetworkArchitecture(jObject);
        yield return null;
    }


    public void RequestNetworkArchitecture()
    {
        StartCoroutine(GetJSON("network", AcceptNetworkArchitecture));
    }


    public IEnumerator DoNothing(JObject jObject)
    {
        yield return null;
    }


    public IEnumerator AcceptLayerActivation(JObject jObject)
    {
        dlManager.AcceptLayerActivation(jObject);
        yield return null;
    }


    public void RequestLayerActivation(int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/activation/layerid/{0}", layerID), AcceptLayerActivation));
    }


    public void RequestLayerFeatureVisualization(int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/featurevisualization/layerid/{0}", layerID), AcceptLayerActivation));
    }


    public void RequestWeightHistogram(int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/weighthistogram/layerid/{0}", layerID), DoNothing));
    }


    public void RequestActivationHistogram(int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/activationhistogram/layerid/{0}", layerID), DoNothing));
    }


    public IEnumerator AcceptPrepareForInput()
    {
        dlManager.AcceptPrepareForInput();
        yield return null;
    }


    public void RequestPrepareForInput(ActivationImage activationImage)
    {
        ActivationImage activationImageShallowCopy = activationImage;
        activationImageShallowCopy.tex = null;
        string output = JsonConvert.SerializeObject(activationImageShallowCopy);
        StartCoroutine(Upload("network/prepareforinput", output, AcceptPrepareForInput));
    }


    public void RequestClassificationResult()
    {
        StartCoroutine(GetJSON("network/classificationresult", DoNothing));
    }


    public IEnumerator AcceptDatasetImages(JObject jObject)
    {
        dlManager.AcceptDatasetImages(jObject);
        yield return null;
    }


    public void RequestDatasetImages()
    {
        StartCoroutine(GetJSON("data/images", AcceptDatasetImages));
    }


    public void RequestNoiseImage()
    {
        StartCoroutine(GetJSON("data/noiseimage", DoNothing));
    }
}