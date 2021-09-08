using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Globalization;

public class DLClientDebug : MonoBehaviour
{
    public string tcpip = "tcp://localhost:5570";
    private DLNetMQ _dlNetMQ;


    private void HandleMessage(List<string> msgList)
    {
        CultureInfo provider = new CultureInfo("en-US");

        if (msgList[0] == "TestShort")
        {
            Debug.Log("handle TestShort");
            foreach (string item in msgList)
            {
                Debug.Log(item);
            }
        }

        else if (msgList[0] == "TestLong")
        {
            Debug.Log("handle TestLong");
            foreach (string item in msgList)
            {
                Debug.Log(item);
            }
        }
    }


    public void TestShort()
    {
        List<string> msg_list = new List<string>();
        msg_list.Add("TestShort");
        _dlNetMQ.QueueRequest(msg_list);
    }


    public void TestLong()
    {
        List<string> msg_list = new List<string>();
        msg_list.Add("TestLong");
        _dlNetMQ.QueueRequest(msg_list);
    }

    private void Start()
    {
        _dlNetMQ = new DLNetMQ(HandleMessage);
        _dlNetMQ.tcpip = tcpip;
        _dlNetMQ.Start();
        TestShort();
        TestLong();
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