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
            socket.Options.ReceiveHighWatermark = 500;
            socket.Connect(tcpip);
            Debug.Log("socket initialized");

            while (!_workerCancelled)
            {
                List<string> msg_list = new List<string>();
                System.Threading.Thread.Sleep(100);

                // send
                if (_requestQueue.TryDequeue(out msg_list))
                {
                    string lastItem = msg_list[msg_list.Count - 1];
                    msg_list.RemoveAt(msg_list.Count - 1);
                    foreach (string item in msg_list)
                    {
                        socket.SendMoreFrame(item);
                    }
                    socket.SendFrame(lastItem);

                    msg_sent_count++;

                    // receive
                    while (!_workerCancelled)
                    {
                        System.Threading.Thread.Sleep(100);
                        if (socket.TryReceiveMultipartStrings(ref msg_list)) break;
                    }
                    msg_received_count++;
                    _messageQueue.Enqueue(msg_list);
                }
            }
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