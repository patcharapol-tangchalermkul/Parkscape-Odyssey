using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

#if UNITY_IOS
public class IOSNetwork:  NetworkUtils {

    [DllImport("__Internal")]
    private static extern void modelInitialize();

    [DllImport("__Internal")]
    private static extern void modelStartDiscovery();

    [DllImport("__Internal")]
    private static extern void modelStartAdvertising();

    [DllImport("__Internal")]
    private static extern void modelStopDiscovery();

    [DllImport("__Internal")]
    private static extern void modelStopAdvertising();

    [DllImport("__Internal")]
    private static extern IntPtr modelGetEndpointName();

    [DllImport("__Internal")]
    private static extern void modelSetEndpointName(string name);

    [DllImport("__Internal")]
    private static extern IntPtr modelGetDiscoveredEndpoints();

    [DllImport("__Internal")]
    private static extern IntPtr modelGetConnectedEndpoints();

    [DllImport("__Internal")]
    private static extern void modelBroadcastString(string message);

    [DllImport("__Internal")]
    private static extern void modelSendString(string message, string destinationID);

    [DllImport("__Internal")]
    private static extern IntPtr modelPeekMessage();

    [DllImport("__Internal")] 
    private static extern IntPtr modelPopMessage();

    private static IOSNetwork instance;

    public static IOSNetwork Instance {
        get {
            if (instance == null) {
                instance = new IOSNetwork();
                instance.initP2P();
            }
            return instance;
        }
    }


    /* Discover other endpoints and connect with endpoints with same name (room code) */
    public override void startDiscovering() {
        modelStartDiscovery();
    } 

    /* Advertise so that other endpoints can connect. */ 
    public override void startAdvertising() {
        modelStartAdvertising();
    }

    /* Stops discovering. */ 
    public override void stopDiscovering() {
        modelStopDiscovery();
    }

    /* Stop advertising. */ 
    public override void stopAdvertising() {
        modelStopAdvertising();
    }   

    /* Returns list of connected devices by device ID. */
    public override List<string> getConnectedDevices() {
        string jsonString = handleStrPtr(modelGetConnectedEndpoints());
        List<string> list = handleJSONStr(jsonString);
        return list;
    }

    /* Returns a list of discovered devices by device ID. */
    public override List<string> getDiscoveredDevices() {
        string jsonString = handleStrPtr(modelGetDiscoveredEndpoints());
        List<string> list = handleJSONStr(jsonString);
        return list;
    }
    /* Broadcasts a message to ALL connected devices as a JSON string. */
    public override void broadcast(string message) {
        modelBroadcastString(message);
    }

    /* Sends a message to a device by device ID. */
    public override void send(string message, string deviceID) {
        modelSendString(message, deviceID);
    }

    /* Sets own endpoint name to be the room code. */
    public override void setRoomCode(string roomCode) {
        modelSetEndpointName(roomCode);
    }
    
    /* Returns a JSON string of messages received so far. */
    public override string getMessageReceived() {
        return handleStrPtr(modelPopMessage());
    }

    /* (For IOS use) Initialises P2P. */
    public override void initP2P() {
        modelInitialize();
    }

    /* On Receive call back*/
    public override void onReceive(System.Func<Message, CallbackStatus> callback) {
        string jsonMessage = handleStrPtr(modelPeekMessage());
        if (jsonMessage != "") {
            Message message = JsonConvert.DeserializeObject<Message>(jsonMessage, new NetworkJsonConverter());
            CallbackStatus status = callback(message);
            if (status == CallbackStatus.PROCESSED || status == CallbackStatus.DORMANT) {
                modelPopMessage();
            }
        }
    }

    /* Mainly for testing. */
    public override string getName() {
        return handleStrPtr(modelGetEndpointName());
    }

    private string handleStrPtr(IntPtr pointer) {
        string str = Marshal.PtrToStringAnsi(pointer);
        return str;
    }

    private List<string> handleJSONStr(string jsonString) {
        List<string> list = JsonConvert.DeserializeObject<List<string>>(jsonString);
        return list;
    }

}
#endif