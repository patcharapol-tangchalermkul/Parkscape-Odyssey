using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;


public class NetworkManager : MonoBehaviour {
    private static NetworkManager instance;
    
    private NetworkUtils networkUtils;

    private LobbyManager lobbyManager;
    private EncounterController encounterController;
    private TradeManager tradeManager;
    private BattleManager battleManager;
    private MapManager mapManager;

    private readonly float baseFreq = 0.5f; // per second
    private readonly float baseSendFreq = 1f; // per second
    private float baseSendTimer = 0f; // per second

    public Dictionary<string, string> connectedPlayers = new ();
    private Dictionary<string, float> connectedPlayersTimer = new();
    private Dictionary<string, string> previouslyConnectedPlayers = new();
    private int numConnectedPlayers = 0;

    private readonly float pingFreq = 2f;
    private float pingTimer = 0;
    private float disconnectTimeout = 10f;


    public static NetworkManager Instance {
        get {
            if (instance == null) {
                // To make sure that script is persistent across scenes
                GameObject go = new GameObject("NetworkManager");
                instance = go.AddComponent<NetworkManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        } else {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        networkUtils = DebugNetwork.Instance;

        #if UNITY_ANDROID
        Debug.Log("ANDROID");
        networkUtils = AndroidNetwork.Instance;
        #endif
        #if UNITY_IOS
        Debug.Log("IOS");
        networkUtils = IOSNetwork.Instance;
        #endif
    }
    
    void Start() {
        InvokeRepeating("HandleMessages", 0.0f, baseFreq);
    }

    void Update() {
        if (LobbyManager.selfReference != null) {
            lobbyManager = LobbyManager.selfReference;
        }

        if (EncounterController.selfReference != null) {
            encounterController = EncounterController.selfReference;
        }

        if (TradeManager.selfReference != null) {
            tradeManager = TradeManager.selfReference;
        }
        
        if  (BattleManager.selfReference != null) {
            battleManager = BattleManager.selfReference;
        }

        if (MapManager.selfReference != null) {
            mapManager = MapManager.selfReference;
        }
    }

    public NetworkUtils NetworkUtils {
        get {
            return networkUtils;
        }
    }

    // Wrapper for sending messages.
    private void HandleMessages() {
        // Set up callback for handling incoming messages.
        Func<Message, CallbackStatus> callback = (Message msg) => {
            return HandleMessage(msg);
        };
        networkUtils.onReceive(callback);

        // Check for disconnected players.
        List<string> disconnectedPlayers = CountdownPlayersLoseConnectionTimer();

        if (baseSendTimer >= baseSendFreq) {
            SendMessages(disconnectedPlayers);
            baseSendTimer = 0;
        } else {
            baseSendTimer += baseFreq;
        }

    }

    private List<string> CountdownPlayersLoseConnectionTimer() {
        List<string> disconnectedPlayers = new List<string>();
        foreach (string id in connectedPlayersTimer.Keys.ToList()) {
            connectedPlayersTimer[id] -= baseFreq;
            if (connectedPlayersTimer[id] <= 0) {
                // Player has not pinged for more than disconnectTimeout, consider player disconnected.
                connectedPlayersTimer.Remove(id);
                connectedPlayers.Remove(id);
                numConnectedPlayers -= 1;
                disconnectedPlayers.Add(id);
            }
        }
        return disconnectedPlayers;
    }

    // Handle incoming messages for all managers.
    private CallbackStatus HandleMessage(Message message) {
        Debug.Log("Got message");
        switch(message.messageInfo.messageType) {
            case MessageType.PINGMESSAGE:
                // Received a ping message from someone else.
                PingMessageInfo pingMessage = (PingMessageInfo)message.messageInfo;
                if (!connectedPlayers.ContainsKey(pingMessage.playerId)) {
                    connectedPlayers[pingMessage.playerId] = pingMessage.playerName;
                    numConnectedPlayers += 1;
                }
                connectedPlayersTimer[pingMessage.playerId] = disconnectTimeout;
                return CallbackStatus.PROCESSED;
            case MessageType.LOBBYMESSAGE:
                if (lobbyManager != null) {
                    Debug.Log("Lobby type and lobby manager not null");
                    return lobbyManager.HandleMessage(message);
                } else {
                    return CallbackStatus.DORMANT;
                }
            case MessageType.ENCOUNTERMESSAGE:
                if (encounterController != null) {
                    return encounterController.HandleMessage(message);
                } else {
                    return CallbackStatus.DORMANT;
                }
            case MessageType.TRADE:
                if (tradeManager != null) {
                    return tradeManager.HandleMessage(message);
                } else {
                    return CallbackStatus.DORMANT;
                }
            case MessageType.BATTLEMESSAGE:
                if (battleManager != null) {
                    return battleManager.HandleMessage(message);
                } else {
                    return CallbackStatus.DORMANT;
                }
            case MessageType.MAP:
                if (mapManager != null) {
                    return mapManager.HandleMessage(message);
                } else {
                    return CallbackStatus.DORMANT;
                }
        }
        return CallbackStatus.NOT_PROCESSED;
    }

    // Handle sending messages for all managers.
    private void SendMessages(List<string> disconnectedPlayers) {
        if (networkUtils == null)
            return;

        // Debug.Log("Connected devices: " + networkUtils.getConnectedDevices().Count);
        // Debug.Log("Connected players: " + connectedPlayers.Count);

        // Send ping messages to all connected players every PingFreq.
        if (networkUtils.getConnectedDevices().Count > 0) {
        if (pingTimer >= pingFreq) {
            PingMessageInfo pingMessage = new PingMessageInfo(PlayerPrefs.GetString("name"));
            networkUtils.broadcast(pingMessage.toJson());
            pingTimer = 0;
        } else {
            pingTimer += baseFreq * (baseSendFreq / baseFreq);
        }
        }
        
        if (lobbyManager != null) {
            lobbyManager.SendMessages(connectedPlayers, disconnectedPlayers);
        }
        if (battleManager != null) {
            battleManager.SendMessages(connectedPlayers, disconnectedPlayers);
        }

        if (mapManager != null) {
            mapManager.SendMessages();
        }
    }

    public bool ChangeInConnectedPlayers()
    {
        // Check if the counts are the same
        if (previouslyConnectedPlayers.Count != connectedPlayers.Count) {
            previouslyConnectedPlayers = connectedPlayers;
            return true;
        }
            

        // Check if all keys and values are the same
        foreach (var pair in previouslyConnectedPlayers)
        {
            string value;
            if (!connectedPlayers.TryGetValue(pair.Key, out value)) {
                previouslyConnectedPlayers = connectedPlayers;
                return true;
            }

            if (!value.Equals(pair.Value)) {
                previouslyConnectedPlayers = connectedPlayers;
                return true;
            }
        }

        previouslyConnectedPlayers = connectedPlayers;
        return false;
    }
}


public class PingMessageInfo : MessageInfo {
        public MessageType messageType { get; set; }
        public string playerId { get; set; }
        public string playerName { get; set; }


        [JsonConstructor]
        public PingMessageInfo(string playerName) {
            this.messageType = MessageType.PINGMESSAGE;
            this.playerId = SystemInfo.deviceUniqueIdentifier;
            this.playerName = playerName;
        }
        
        public string toJson() {
            return JsonConvert.SerializeObject(this);
        }
}