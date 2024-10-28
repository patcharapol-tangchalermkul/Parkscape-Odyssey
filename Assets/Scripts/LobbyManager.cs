using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Microsoft.Geospatial;

public class LobbyManager : MonoBehaviour {
    // Singleton
    public static LobbyManager selfReference;

    // UI
    private LobbyUIManager lobbyUIManager;
    
    // Constants
    private readonly int maxPlayerCount = 6;

    // Messaging and identification
    private Dictionary<string, string> players = new();
    private string myID;
    private string myName;
    private string roomCode = "";
    private bool isLeader = false;
    private string leaderID = "";

    // Messaging
    private NetworkUtils network;
    private List<string> connectedDevices = new(); /* Tracking only for disconnections. */
    private bool AcceptMessages = false;
    private int msgFreq = 0;
    private int msgFreqCounter = 0;
    private int roomFindingTimeout = 200;
    private bool joinedLobby = false;
    private int disconnectCount = 0;
    private List<string> playerIDs;

    // Map
    private GPSManager gpsManager;

    // Initialisation
	void Awake () {
		if(!selfReference) {
			selfReference = this;
            lobbyUIManager = GetComponent<LobbyUIManager>();
            myID = SystemInfo.deviceUniqueIdentifier;
            msgFreq = maxPlayerCount;
            network = NetworkManager.Instance.NetworkUtils;
            gpsManager = GPSManager.Instance;
			DontDestroyOnLoad(gameObject);
		} else 
            Destroy(gameObject);
	}

    // Exposed methods for buttons
    // When the player wants to leave the lobby and go back to the room selection screen.
    public void ExitLobby() {
        AcceptMessages = false;

        // Tell the leader to remove you from the list of players.
        if (isLeader) {
            // If you are the leader, tell everyone to exit the lobby.
            LobbyMessage leaveLobbyMessage = new(LobbyMessageType.LEADER_LEAVE, true, "", mediumEncounterLocations: null);
            network.broadcast(leaveLobbyMessage.toJson());
        }
        network.stopAdvertising();
        network.stopDiscovering();

        joinedLobby = false;

        // Reset the lobby.
        players = new Dictionary<string, string>();
        myID = SystemInfo.deviceUniqueIdentifier;
        leaderID = "";
        connectedDevices = new List<string>();
        joinedLobby = false;
        playerIDs = new();
        lobbyUIManager.ExitLobby();

        // Reset gamestate
        GameState.Instance.Reset();
    }

    // Start game as leader and broadcast to everyone.
    public void LeaderStartGame() {
        if (!isLeader)
            return;

        // Create a list of all the players id
        Debug.Log("Creating list of player IDs");
        playerIDs = new(players.Keys);
        playerIDs.Remove(myID);

        // Initialise the game state.
        Debug.Log("Set game state, should be null gamestate");
        GameState gameState = GameState.Instance;

        Debug.Log("Initializing gamestate");
        gameState.Initialize(myID, roomCode, players);

        GameState.Instance.MyPlayer.IsLeader = true;

        StartCoroutine(FetchDataAndInitialiseGame());
    }

    // Try to get new medium encounters from the database and start the game.
    //
    // This is a coroutine instead of an async function because the latter
    // makes no guarantees of continuing on the main thread (so loading scenes
    // may not work properly). Internally, it awaits the completion of async
    // functions before starting the game on the main thread.
    private IEnumerator FetchDataAndInitialiseGame(bool follower=false) {
        // Display the loading scene while waiting for async DB operations
        // to complete. The scene is loaded additively so that this GameObject
        // doesn't get destroyed
        SceneManager.LoadScene("Initialisation", LoadSceneMode.Additive);

        int fileTimeoutSeconds = 20;

        // Get medium encounter positions asynchronously
        Debug.LogWarning("1. Getting medium encounters");

        if (!follower) {
            gpsManager.GetMediumEncounters();
        }

        Task questInitialisation = GameState.Instance.InitialiseQuests();

        // Wait for the task to complete
        while (!questInitialisation.IsCompleted) {
            yield return null;
        }

        // Get location quest data asynchronously
        Task<byte[]> locationQuestVectors = DatabaseUtils.GetFileWithTimeout(
            "locationQuestVectors.bytes", fileTimeoutSeconds);
        Task<byte[]> locationQuestGraph = DatabaseUtils.GetFileWithTimeout(
            "locationQuestGraph.bytes", fileTimeoutSeconds);
        Task<byte[]> locationQuestLabels = DatabaseUtils.GetFileWithTimeout(
            "locationQuestLabels.bytes", fileTimeoutSeconds);
        
        // Internally, these functions have will timeout if they take too long
        // so they will always complete eventually
        while (
            !locationQuestVectors.IsCompleted ||
            !locationQuestGraph.IsCompleted ||
            !locationQuestLabels.IsCompleted) {
            yield return null;
        }

        // Save the files to disk
        Debug.LogWarning("10. Saving location quest files to disk");
        FileUtils.ProcessNewQuestFiles(
            locationQuestVectors.Result,
            locationQuestGraph.Result,
            locationQuestLabels.Result
        );

        // Unload the loading scene
        SceneManager.UnloadSceneAsync("Initialisation");

        Debug.LogWarning("11. Sending encounter info to players in lobby");


        if (!follower) {
            Dictionary<string, Dictionary<string, double>> mediumEncounterLocations = MapMessage.LatLonToDict(GameState.Instance.mediumEncounterLocations);
            network.broadcast(new MapMessage(MapMessageType.MAP_INFO, new List<string>(), mediumEncounterLocations).toJson());

            Debug.LogWarning("12. Got medium encounters: " + GameState.Instance.mediumEncounterLocations.Count + " locations.");
            // Debugging
            foreach (KeyValuePair<string, LatLon> item in GameState.Instance.mediumEncounterLocations) {
                Debug.LogWarning("13. Key: " + item.Key + ", Location: (" + item.Value.LatitudeInDegrees + ", " + item.Value.LongitudeInDegrees + ")");
            }
            
            if (players.Count > 1) {
                // send map info
                // members
                // receive map info
                // receive start game
                // start game
                LeaderSendStartGameMessage();
            }
        }

        // Get count of loaded Scenes and last index
        // (corresponding to the loading scene)
        var lastSceneIndex = SceneManager.sceneCount - 1;

        // Get last Scene by index in all loaded Scenes
        var lastLoadedScene = SceneManager.GetSceneAt(lastSceneIndex);

        // Unload Scene
        SceneManager.UnloadSceneAsync(lastLoadedScene);
        
        StartGame();
    }

    private void LeaderSendStartGameMessage() {
        // Send a message to everyone to start the game.
        LobbyMessage leaderStartMessage = 
            new(LobbyMessageType.LEADER_START, true, GameState.Instance.ToMessage().toJson(), 
            mediumEncounterLocations: MapMessage.LatLonToDict(GameState.Instance.mediumEncounterLocations));
        network.broadcast(leaderStartMessage.toJson());
    }


    // Send round of messages.
    public void SendMessages(Dictionary<string, string> connectedPlayers, List<string> disconnectedPlayerIDs) {
        if (!AcceptMessages) {
            return;
        }

        if (network.getConnectedDevices().Count <=  0) {
            return;
        }

        // There are connected players, so we can send messages.

        // IDs of connected players.
        List<string> connectedIDs = new List<string>(connectedPlayers.Keys);

        if (isLeader) {
            // Leader check if any devices have disconnected.
            foreach (string id in disconnectedPlayerIDs) {
                RemovePlayer(id);
            }
        } else {
            // If I lost connection to the leader then I should exit the lobby after 10 seconds
            if (!leaderID.Equals("") && !connectedIDs.Contains(leaderID)) {
                if (disconnectCount < 100) {
                    disconnectCount++;
                } else {
                    disconnectCount = 0;
                    ExitLobby();
                }
            }

            if (!joinedLobby) {
                // If I am not in the lobby, send a message to the leader that I am in.
                LobbyMessage amIInMessage = new(LobbyMessageType.MEMBER_I_AM_IN, false, myName, mediumEncounterLocations: null, sendTo : leaderID, sendFrom: myID);
                network.broadcast(amIInMessage.toJson());
            }
        }
    }

    // Common method to start the game for both leader and non-leader.
    private void StartGame() {
        Debug.LogWarning("13. Starting game");
        AcceptMessages = false;
        GameState.Instance.isLeader = isLeader;
        // Load the game scene.
        SceneManager.LoadScene("Gameplay");
    }

    private void AddPlayer(string id, string name) {
        if (players.ContainsKey(id))
            return;
        players.Add(id, name);
        Debug.Log("Added player: " + id + "  " + name);
        lobbyUIManager.AddPlayer(name);
    }

    private void RemovePlayer(string id) {
        if (!players.ContainsKey(id)) {
            Debug.Log("Player not found in list of players.");
            return;
        }
        lobbyUIManager.RemovePlayer(players[id]);
        players.Remove(id);
    }

    public void SetUpLobby(string roomCode, bool isLeader) {
        this.roomCode = roomCode;
        myName = PlayerPrefs.GetString("name");


        // Start discovering and advertising.
        network.setRoomCode(roomCode);
        network.startDiscovering();


        this.isLeader = isLeader;
        if (!isLeader) {
            // Wait for room to be found.
            if (!FindRoom())
                throw new Exception("Room not found.");
        } else {
            network.startAdvertising();
        }

        // network.startAdvertising();
        network.stopDiscovering();
        AcceptMessages = true;

        lobbyUIManager.SetUpLobby(roomCode, isLeader);
        AddPlayer(myID, myName);
    }

    private bool FindRoom() {
        int count = 0;
        while (count < roomFindingTimeout) {
            List<string> connections = network.getConnectedDevices();
            if (connections.Count != 0)
                return true;
            System.Threading.Thread.Sleep(50);
            count++;
        }
        return false;
    }

    public CallbackStatus HandleMessage(Message message) {
        LobbyMessage lobbyMessage = (LobbyMessage) message.messageInfo;

        if (lobbyMessage.SendTo != "" && lobbyMessage.SendTo != myID)
            return CallbackStatus.DORMANT;
        
        // Ignore if no longer accepting messages.
        if (!AcceptMessages)
            return CallbackStatus.DORMANT;

        switch (lobbyMessage.Type) {
            case LobbyMessageType.MEMBER_I_AM_IN:
                // Ignore if not leader.
                if (!isLeader)
                    break;

                // Ignore if max player count reached.
                if (players.Count >= maxPlayerCount)
                    break;

                // Add player to the list of players.
                AddPlayer(lobbyMessage.SendFrom, lobbyMessage.Message);
                
                // Send the new list of players to everyone in the lobby.
                LobbyMessage newLobbyMessage = new(isLeader, players, myID, mediumEncounterLocations: null);
                network.broadcast(newLobbyMessage.toJson());
                break;
            case LobbyMessageType.LEADER_PLAYERS:
                if (isLeader)
                    break;

                leaderID = lobbyMessage.SendFrom;

                // Add all players to the list of players.
                foreach (KeyValuePair<string, string> player in lobbyMessage.Players) {
                    // Check if the player is me.
                    if (player.Key.Equals(myID)) {
                        joinedLobby = true;
                        continue;
                    }
                    AddPlayer(player.Key, player.Value);
                }

                // Remove all players that are not in the list of players.
                Dictionary<string, string> playersCopy = new(players);
                foreach (KeyValuePair<string, string> player in playersCopy) {
                    if (!lobbyMessage.Players.ContainsKey(player.Key) 
                        && !player.Key.Equals(myID) && !player.Key.Equals(leaderID))
                        RemovePlayer(player.Key);
                }

                if (!joinedLobby) {
                    // If I am not in the lobby, send a message to the leader that I am in.
                    LobbyMessage amIInMessage = new(LobbyMessageType.MEMBER_I_AM_IN, false, myName, mediumEncounterLocations: null, sendTo : leaderID, sendFrom: myID);
                    network.broadcast(amIInMessage.toJson());
                }
                break;
            case LobbyMessageType.LEADER_START:
                if (isLeader)
                    break;
                
                // Member may have received this message before.
                GameState gameState = GameState.Instance;
                if (!gameState.Initialized) {
                    gameState.InitializeFromMessage(GameStateMessage.fromJson(lobbyMessage.Message), roomCode, myID);
                }
                // Receive map info about medium encounters
                gameState.mediumEncounterLocations = MapMessage.DictToLatLon(lobbyMessage.MediumEncounterLocations);
                
                // Fetch and set location quests
                StartCoroutine(FetchDataAndInitialiseGame(follower: true));

                break;
            case LobbyMessageType.MEMBER_START:
                if (!isLeader)
                    break;

                // Send a message to say that their start message was received and it's okay to start the game.
                if (playerIDs.Contains(lobbyMessage.SendFrom))
                    playerIDs.Remove(lobbyMessage.SendFrom);
                LobbyMessage leaderStartMessage = new(LobbyMessageType.LEADER_START_OK, true, myName, mediumEncounterLocations: null, sendTo : lobbyMessage.SendFrom, sendFrom : myID);
                network.broadcast(leaderStartMessage.toJson());

                // Once everyone has sent the start message, start the game.
                if (playerIDs.Count == 0)
                    StartGame();
                break;
            case LobbyMessageType.LEADER_LEAVE:
                if (isLeader)
                    break;

                ExitLobby();
                break;
            case LobbyMessageType.LEADER_START_OK:
                if (isLeader)
                    break;

                StartGame();
                break;
        }
        return CallbackStatus.PROCESSED;
    }
}

// Handlemessages with bool to tell it to stop
// do not kill thread to handle messages (track them tho) (will have 3 or 4 at end of game to kill)

public enum LobbyMessageType {
    MEMBER_I_AM_IN,
    LEADER_START,
    LEADER_PLAYERS,
    LEADER_LEAVE,
    MEMBER_START,
    LEADER_START_OK,
}

public class LobbyMessage : MessageInfo {
    public MessageType messageType {get; set;}
    public LobbyMessageType Type {get; set;}
    public string Message {get;}
    public Dictionary<string, string> Players {get;}
    public bool IsLeader {get;}
    public string SendTo {get;}
    public string SendFrom {get;}
    public Dictionary<string, Dictionary<string, double>> MediumEncounterLocations {get;}

    public LobbyMessage(LobbyMessageType type, bool isLeader, string message, Dictionary<string, Dictionary<string, double>> mediumEncounterLocations, 
        string sendTo = "", string sendFrom = "") {
        messageType = MessageType.LOBBYMESSAGE;
        Type = type;
        IsLeader = isLeader;
        Message = message;
        SendTo = sendTo;
        SendFrom = sendFrom;
        MediumEncounterLocations = mediumEncounterLocations == null ? new() : mediumEncounterLocations;
    }

    public LobbyMessage(bool isLeader, Dictionary<string, string> players, string leaderID, Dictionary<string, Dictionary<string, double>> mediumEncounterLocations, 
        string sendTo = "") {
        if (!isLeader)
            throw new Exception("Only leader can send this message.");
        messageType = MessageType.LOBBYMESSAGE;
        Type = LobbyMessageType.LEADER_PLAYERS;
        IsLeader = isLeader;
        Players = players;
        Message = "";
        SendTo = sendTo;
        SendFrom = leaderID;
        MediumEncounterLocations = mediumEncounterLocations == null ? new() : mediumEncounterLocations;
    }

    [JsonConstructor]
    public LobbyMessage(LobbyMessageType type, string message, Dictionary<string, string> players, bool isLeader, string sendTo, string sendFrom, 
        Dictionary<string, Dictionary<string, double>> mediumEncounterLocations) {
        messageType = MessageType.LOBBYMESSAGE;
        Type = type;
        IsLeader = isLeader;
        Message = message;
        Players = players;
        SendTo = sendTo;
        SendFrom = sendFrom;
        MediumEncounterLocations = mediumEncounterLocations;
    }

    public string toJson() {
        return JsonConvert.SerializeObject(this);
    }
}
