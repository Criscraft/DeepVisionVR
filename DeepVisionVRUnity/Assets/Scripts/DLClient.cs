using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Globalization;

public class DLClient : MonoBehaviour
{
    public string tcpip = "tcp://localhost:5570";
    private DLManager _dlManager;
    private DLNetMQ _dlNetMQ;


    private void HandleMessage(List<string> msgList)
    {
        CultureInfo provider = new CultureInfo("en-US");

        if (msgList[0] == "RequestDataOverview")
        {
            Debug.Log("handle RequestDataOverview");
            JObject result = JObject.Parse(msgList[1]);
            UnityMainThreadDispatcher.Instance().Enqueue(_dlManager.AcceptDataOverview(result));
        }
        else if (msgList[0] == "RequestNetworkArchitecture")
        {
            Debug.Log("handle RequestNetworkArchitecture");
            List<JObject> result = new List<JObject>();
            msgList.RemoveAt(0);
            foreach (string item in msgList)
            {
                result.Add(JObject.Parse(item));
            }
            UnityMainThreadDispatcher.Instance().Enqueue(_dlManager.AcceptNetworkArchitecture(result));
        }
        else if (msgList[0] == "RequestLayerActivation")
        {
            Debug.Log("handle RequestLayerActivation");

            List<Texture2D> result = new List<Texture2D>();
            int layerID = int.Parse(msgList[1]);
            float zeroValue = float.Parse(msgList[2], provider);
            msgList.RemoveAt(0);
            msgList.RemoveAt(0);
            msgList.RemoveAt(0);
            foreach (string item in msgList)
            {
                byte[] b64_bytes = System.Convert.FromBase64String(item);
                Texture2D tex = new Texture2D(1, 1);
                if (ImageConversion.LoadImage(tex, b64_bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    result.Add(tex);
                }
                else
                {
                    Debug.Log("Texture could not be loaded");
                }
            }
            UnityMainThreadDispatcher.Instance().Enqueue(_dlManager.AcceptLayerActivation(result, layerID, zeroValue));
        }
        else if (msgList[0] == "RequestPrepareForInput")
        {
            Debug.Log("handle RequestPrepareForInput");
            UnityMainThreadDispatcher.Instance().Enqueue(_dlManager.AcceptPrepareForInput());
        }
        else if (msgList[0] == "RequestDatasetImage")
        {
            Debug.Log("handle RequestDatasetImage");

            int imgIndex = int.Parse(msgList[1]);
            int label = int.Parse(msgList[3]);
            byte[] b64_bytes = System.Convert.FromBase64String(msgList[2]);
            Texture2D tex = new Texture2D(1, 1);
            if (ImageConversion.LoadImage(tex, b64_bytes))
            {
                tex.filterMode = FilterMode.Point;
            }
            else
            {
                Debug.Log("Texture could not be loaded");
            }
            UnityMainThreadDispatcher.Instance().Enqueue(_dlManager.AcceptDatasetImage(tex, label, imgIndex));
        }
        else if (msgList[0] == "RequestClassificationResult")
        {
            Debug.Log("handle RequestClassificationResult");
            JObject jObject = JObject.Parse(msgList[1]);
            UnityMainThreadDispatcher.Instance().Enqueue(_dlManager.AcceptClassificationResult(jObject));
        }
    }


    public void RequestDataOverview()
    {
        List<string> msg_list = new List<string>();
        msg_list.Add("RequestDataOverview");
        _dlNetMQ.QueueRequest(msg_list);
    }


    public void RequestNetworkArchitecture()
    {
        List<string> msg_list = new List<string>();
        msg_list.Add("RequestNetworkArchitecture");
        _dlNetMQ.QueueRequest(msg_list);
    }


    public void RequestLayerActivation(int layerID)
    {
        List<string> msg_list = new List<string>();
        msg_list.Add("RequestLayerActivation");
        msg_list.Add(string.Format("{0}", layerID));
        _dlNetMQ.QueueRequest(msg_list);
    }


    public void RequestPrepareForInput(int imgIndex)
    {
        List<string> msg_list = new List<string>();
        msg_list.Add("RequestPrepareForInput");
        msg_list.Add(string.Format("{0}", imgIndex));
        _dlNetMQ.QueueRequest(msg_list);
    }


    public void RequestDatasetImage(int imgIndex)
    {
        List<string> msg_list = new List<string>();
        msg_list.Add("RequestDatasetImage");
        msg_list.Add(string.Format("{0}", imgIndex));
        _dlNetMQ.QueueRequest(msg_list);
    }


    public void RequestClassificationResult()
    {
        List<string> msg_list = new List<string>();
        msg_list.Add("RequestClassificationResult");
        _dlNetMQ.QueueRequest(msg_list);
    }


    public void Prepare()
    {
        _dlManager = GetComponent<DLManager>();
        _dlNetMQ = new DLNetMQ(HandleMessage);
        _dlNetMQ.tcpip = tcpip;
        _dlNetMQ.Start();
    }

    private void Update()
    {
        _dlNetMQ.Update();
    }

    private void OnDestroy()
    {
        _dlNetMQ.Stop();
    }
}