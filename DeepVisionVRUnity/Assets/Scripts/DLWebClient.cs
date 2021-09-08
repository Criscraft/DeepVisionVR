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


    private IEnumerator GetJSON(string resource, HandleJSONDelegate handleJSONDelegate)
    {
        UnityWebRequest www = UnityWebRequest.Get(url + resource);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            JObject jObject = JObject.Parse(www.downloadHandler.text);
            UnityMainThreadDispatcher.Instance().Enqueue(handleJSONDelegate(jObject));
        }
    }


    private IEnumerator Upload(string resource, string dataString)
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
        }
    }


    public IEnumerator AcceptNetworkArchitecture(JObject jObject)
    {
        dlManager.AcceptNetworkArchitecture(jObject);
        Debug.Log(jObject);
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


    public void RequestLayerActivation(int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/activation?layerid={0}", layerID), DoNothing));
    }


    public void RequestLayerFeatureVisualization(int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/featurevisualization?layerid={0}", layerID), DoNothing));
    }


    public void RequestWeightHistogram(int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/weighthistogram?layerid={0}", layerID), DoNothing));
    }


    public void RequestActivationHistogram(int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/activationhistogram?layerid={0}", layerID), DoNothing));
    }


    public void RequestPrepareForInput(ActivationImage activationImage)
    {
        string output = JsonConvert.SerializeObject(activationImage);
        StartCoroutine(Upload("network/prepareforinput", output));
    }


    public void RequestClassificationResult()
    {
        StartCoroutine(GetJSON("network/classificationresult", DoNothing));
    }


    public void RequestDataOverview()
    {
        StartCoroutine(GetJSON("data", DoNothing));
    }


    public void RequestDatasetImage(int imgIndex)
    {
        StartCoroutine(GetJSON(string.Format("data/image?imgindex={0}", imgIndex), DoNothing));
    }


    public void RequestNoiseImage()
    {
        StartCoroutine(GetJSON("data/noise", DoNothing));
    }
}