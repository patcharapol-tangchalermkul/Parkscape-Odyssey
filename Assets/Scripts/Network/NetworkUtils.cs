using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public abstract class NetworkUtils
{
    private string currentMessage;
    /* Cache of set of message IDs received so far. */
    protected HashSet<string> messageIDs{get; set;}
    /* Discover other endpoints and connect with endpoints with same name (room code) */
    public abstract void startDiscovering();
    /* Advertise so that other endpoints can connect. */ 
    public abstract void startAdvertising(); 
    /* Stops discovering. */ 
    public abstract void stopDiscovering();
    /* Stop advertising. */ 
    public abstract void stopAdvertising();    
    /* Returns list of connected devices by device ID. */
    public abstract List<string> getConnectedDevices();
    /* Returns a list of discovered devices by device ID. */
    public abstract List<string> getDiscoveredDevices();
    /* Broadcasts a message to ALL connected devices as a JSON string. */
    public abstract void broadcast(string message);
    /* Sends a message to a device by device ID. */
    public abstract void send(string message, string deviceID);
    /* Sets own endpoint name to be the room code. */
    public abstract void setRoomCode(string roomCode);
    /* Returns a JSON string of the most recent message received. */
    public abstract string getMessageReceived();
    /* (For IOS use) Initialises P2P. */
    public abstract void initP2P();
    /* Mainly for testing. */
    public abstract string getName();
    /* Listener function */
    public abstract void onReceive(System.Func<Message, CallbackStatus> callback);
}

public enum CallbackStatus {
    PROCESSED=0,
    NOT_PROCESSED=1,
    DORMANT=2
}