using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///     Example of requester who only sends Hello. Very nice guy.
///     You can copy this class and modify Run() to suits your needs.
///     To use this class, you just instantiate, call Start() when you want to start and Stop() when you want to stop.
/// </summary>
public class DLRequester : RunAbleThread
{

    public static DLRequester instance;


    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one instance in scene!");
            return;
        }
        instance = this;
    }


    protected override void Run()
    {

        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
        using (RequestSocket client = new RequestSocket())
        {
            client.Connect("tcp://localhost:5570");
            bool gotMessage = false;
            string message = null;

            Debug.Log("Sending get_data_overview");
            client.SendFrame("get_data_overview");
            
            while (Running)
            {
                gotMessage = client.TryReceiveFrameString(out message); // this returns true if it's successful
                if (gotMessage) break;
            }
            if (gotMessage)
            {
                var result1 = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                Debug.Log(result1);
                Debug.Log(result1["n_classes"]);
            }

            Debug.Log("Sending get_data_item");
            client.SendMoreFrame("get_data_item")
                .SendFrame("0");
            gotMessage = false;
            message = null;
            while (Running)
            {
                gotMessage = client.TryReceiveFrameString(out message); // this returns true if it's successful
                if (gotMessage) break;
            }
            if (gotMessage)
            {
                var b64_bytes = System.Convert.FromBase64String(message);
                var tex = new Texture2D(1, 1);
                if (!ImageConversion.LoadImage(tex,b64_bytes))
                {
                    Debug.Log("Texture could not be loaded");
                }
            }

            client.Close();
        }

        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }


    public List<Dictionary<string, object>> getArchitecture()
    {
        return null;
    }




}