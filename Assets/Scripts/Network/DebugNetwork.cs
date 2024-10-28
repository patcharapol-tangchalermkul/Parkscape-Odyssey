using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class DebugNetwork : NetworkUtils
{
    private static DebugNetwork instance;

    public static DebugNetwork Instance {
        get {
            if (instance == null) {
                instance = new DebugNetwork();
            }
            return instance;
        }
    }

    private readonly bool DEBUGLOG = true;
    private readonly bool DEBUGLOGONRECEIVE = false;

    public override void broadcast(string message)
    {   
        if (!DEBUGLOG)
            return;
        Debug.Log("Broadcast: " + message);
    }

    public override List<string> getConnectedDevices()
    {
        return new();
    }

    public override List<string> getDiscoveredDevices()
    {
        return new List<string> {"2", "3"};
    }

    public override string getMessageReceived()
    {
        return "";
    }

    public override string getName()
    {
        return "";
    }

    public override void initP2P()
    {
        return;
    }

    public override void onReceive(Func<Message, CallbackStatus> callback)
    {
        if (!DEBUGLOG && !DEBUGLOGONRECEIVE)
            return;
        Debug.Log("On receive.");
    }

    public override void send(string message, string deviceID)
    {
        if (!DEBUGLOG)
            return;
        Debug.Log("Send message: " + message);
    }

    public override void setRoomCode(string roomCode)
    {
        if (!DEBUGLOG)
            return;
        Debug.Log("Set room code: " + roomCode);
    }

    public override void startAdvertising()
    {
        if (!DEBUGLOG)
            return;
        Debug.Log("Start advertising.");
    }

    public override void startDiscovering()
    {
        if (!DEBUGLOG)
            return;
        Debug.Log("Start discovering.");
    }

    public override void stopAdvertising()
    {
        if (!DEBUGLOG)
            return;
        Debug.Log("Stop advertising.");
    }

    public override void stopDiscovering()
    {
        if (!DEBUGLOG)
            return;
        Debug.Log("Stop discovering.");
    }
}