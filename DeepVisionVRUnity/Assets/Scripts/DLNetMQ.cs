using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using NetMQ;
using UnityEngine;
using NetMQ.Sockets;


public class DLNetMQ
{
    public string tcpip;
    private readonly Thread _worker;
    private bool _workerCancelled;
    public delegate void MessageDelegate(List<string> msg_list);
    private readonly MessageDelegate _messageDelegate;
    private readonly ConcurrentQueue<List<string>> _requestQueue = new ConcurrentQueue<List<string>>();
    private readonly ConcurrentQueue<List<string>> _messageQueue = new ConcurrentQueue<List<string>>();
    private long msg_sent_count;
    private long msg_received_count;


    public DLNetMQ(MessageDelegate messageDelegate)
    {
        _messageDelegate = messageDelegate;
        AsyncIO.ForceDotNet.Force();
        _worker = new Thread(new ThreadStart(ThreadWork));
        //_worker.Priority = UnityEngine.ThreadPriority.BelowNormal;
        _worker.Priority = System.Threading.ThreadPriority.Lowest;
    }


    public void QueueRequest(List<string>  msg_list)
    {
        _requestQueue.Enqueue(msg_list);
    }


    private void ThreadWork()
    {
        Debug.Log("Setting up socket");
        AsyncIO.ForceDotNet.Force();
        using (var socket = new RequestSocket())
        {
            // set limit on how many messages in memory
            socket.Options.ReceiveHighWatermark = 1000;
            NetMQConfig.Linger = new System.TimeSpan(0, 0, 0);
            socket.Options.Linger = new System.TimeSpan(0, 0, 0);
            socket.Options.DisableTimeWait = true;
            socket.Options.MulticastRecoveryInterval = new System.TimeSpan(0, 2, 0);
            socket.Options.ReconnectIntervalMax = new System.TimeSpan(0, 2, 0);

            socket.Connect(tcpip);
            
            Debug.Log("socket initialized");
            int sleepTime = 0;

            while (!_workerCancelled)
            {
                List<string> msg_list = new List<string>();
                sleepTime = 200;
                if (_requestQueue.Count>1) sleepTime = 50;
                System.Threading.Thread.Sleep(sleepTime);

                // send
                if (_requestQueue.TryDequeue(out msg_list))
                {
                    Debug.Log("send:");
                    Debug.Log(msg_list[0]);
                    string lastItem = msg_list[msg_list.Count - 1];
                    msg_list.RemoveAt(msg_list.Count - 1);
                    foreach (string item in msg_list)
                    {
                        socket.SendMoreFrame(item);
                    }
                    socket.SendFrame(lastItem);

                    msg_sent_count++;
                    msg_list.Clear();
                    // receive

                    while (!_workerCancelled)
                    {
                        if (socket.TryReceiveMultipartStrings(new System.TimeSpan(0, 1, 0), ref msg_list)) break;
                        Debug.Log("Did not receive anything");
                    }
                    
                    //msg_list = socket.ReceiveMultipartStrings();


                    msg_received_count++;
                    _messageQueue.Enqueue(msg_list);
                }
            }
            Debug.Log("Close socket");
            socket.Close();
        }
        NetMQConfig.Cleanup();
    }


    // check queue for messages
    public void Update()
    {
        while (!_messageQueue.IsEmpty)
        {
            List<string> msg_list;
            if (_messageQueue.TryDequeue(out msg_list))
            {
                _messageDelegate(msg_list);
            }
            else
            {
                break;
            }
        }
    }


    public void Start()
    {
        msg_sent_count = -1;
        msg_received_count = -1;
        _workerCancelled = false;
        _worker.Start();
    }

    public void Stop()
    {
        _workerCancelled = true;
        _worker.Join();
    }
}