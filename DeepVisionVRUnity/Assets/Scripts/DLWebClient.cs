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
                Debug.Log("Received");
                Debug.Log(url + resource);
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
                Debug.Log("Upload Complete!");
                Debug.Log(url + resource);
                UnityMainThreadDispatcher.Instance().Enqueue(HandleUploadDelegate());
            }
        }
    }

    public void RequestNetworkArchitecture(HandleJSONDelegate handleJSONDelegate)
    {
        StartCoroutine(GetJSON("network", handleJSONDelegate));
    }


    public IEnumerator DoNothing(JObject jObject)
    {
        yield return null;
    }


    public void RequestLayerActivation(HandleJSONDelegate handleJSONDelegate, int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/activation/layerid/{0}", layerID), handleJSONDelegate));
    }


    public void RequestLayerFeatureVisualization(HandleJSONDelegate handleJSONDelegate, int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/featurevisualization/layerid/{0}", layerID), handleJSONDelegate));
    }


    public void RequestWeightHistogram(HandleJSONDelegate handleJSONDelegate, int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/weighthistogram/layerid/{0}", layerID), handleJSONDelegate));
    }


    public void RequestActivationHistogram(HandleJSONDelegate handleJSONDelegate, int layerID)
    {
        StartCoroutine(GetJSON(string.Format("network/activationhistogram/layerid/{0}", layerID), handleJSONDelegate));
    }


    public void RequestPrepareForInput(HandleUploadDelegate handleUploadDelegate, ActivationImage activationImage)
    {
        ActivationImage activationImageShallowCopy = activationImage;
        activationImageShallowCopy.tex = null;
        string output = JsonConvert.SerializeObject(activationImageShallowCopy);
        StartCoroutine(Upload("network/prepareforinput", output, handleUploadDelegate));
    }


    public void RequestClassificationResults(HandleJSONDelegate handleJSONDelegate)
    {
        StartCoroutine(GetJSON("network/classificationresult", handleJSONDelegate));
    }


    public void RequestDatasetImages(HandleJSONDelegate handleJSONDelegate)
    {
        StartCoroutine(GetJSON("data/images", handleJSONDelegate));
    }


    public void RequestNoiseImage(HandleJSONDelegate handleJSONDelegate)
    {
        StartCoroutine(GetJSON("data/noiseimage", handleJSONDelegate));
    }
}