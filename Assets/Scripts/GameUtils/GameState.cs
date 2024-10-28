using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Maps.Unity;
using Microsoft.Geospatial;
using UnityEngine;

using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;

public class GameState {
    public static readonly bool DEBUGMODE = 
    #if UNITY_EDITOR 
        true;
    #else
        false;
    #endif
    public static readonly bool MAPDEBUGMODE = 
    #if UNITY_EDITOR 
        true;
    #else
        false;
    #endif

    private static readonly Lazy<GameState> LazyGameState = new(() => new GameState());

    public static GameState Instance { get {
        GameState gameState = LazyGameState.Value;
        // if (DEBUGMODE && !gameState.Initialized) {
        //     gameState.Initialize("1", "123", new Dictionary<string, string> {
        //         {"1", "Player 1"},
        //         {"2", "Player 2"},
        //         {"3", "Player 3"},
        //         {"4", "Player 4"},
        //     });
        //     gameState.isLeader = true;
        // }
        return gameState; } }

    public int maxPlayerCount = 6;

    // Fields
    public string myID;
    public bool Initialized = false;
    public string RoomCode = ""; 
    public Player MyPlayer = null;
    public List<Player> OtherPlayers = new();

    public Dictionary<string, Player> PlayersDetails = new();
    private Dictionary<int, CardName> MyCards = new();
    public bool isLeader = false;
    public bool IsInEncounter = false;
    public int Score = 0;

    private List<CardName> InitialCards = new List<CardName> {
        CardName.BASE_ATK, CardName.BASE_ATK, CardName.BASE_ATK, 
        CardName.BASE_DEF, CardName.BASE_DEF, CardName.BASE_DEF
    };

    private int cardID = 0;

    // ENCOUNTER
    public List<Monster> encounterMonsters;

    public Dictionary<string, string> partyMembers;

    // MAP
    // Medium Encounter Locations broadcasted by the leader/ web authoring tool in the beginning of the game
    public Dictionary<string, LatLon> mediumEncounterLocations = new();
    // Medium Encounter IDs found by the player, to be shared with other players
    public HashSet<string> foundMediumEncounters = new();

    // LOCATION QUESTS
    // Map of location names to locationQuest objects, as well as
    // files required for the object classifier
    public Dictionary<string, LocationQuest> locationQuests = new();
    public byte[] locationQuestVectors;
    public byte[] locationQuestGraph;
    public byte[] locationQuestLabels;
    // Quests
    public List<BasicQuest> basicQuests = new();

    // Method will be called only during Game initialization.
    public void Initialize(string myID, string roomCode, Dictionary<string, string> players) {

        CheckNotInitialised();

        UnityEngine.Debug.Log("Finished executing StartFirebase().");

        RoomCode = roomCode;

        // Random roles for each player.
        List<string> roles = PlayerFactory.GetRoles();
        System.Random random = new System.Random();
        foreach (string id in players.Keys) {
            string name = players[id];
            string role = roles[random.Next(roles.Count)];
            roles.Remove(role);
            Player player = PlayerFactory.CreatePlayer(id, name, role);
            PlayersDetails.Add(id, player);
            if (id == myID) {
                MyPlayer = player;
            } else {
                OtherPlayers.Add(player);
            }
        }

        isLeader = true;
        this.myID = myID;
        FindAndLoadQuestFiles();

        Initialized = true;
        InitialiseCards();

    }

    // Method to specify the initial state of the game.
    public void Initialize(string roomCode, Player myPlayer, List<Player> otherPlayers, List<CardName> initialCards) {
        CheckNotInitialised();
        RoomCode = roomCode;
        MyPlayer = myPlayer;
        OtherPlayers = otherPlayers;

        FindAndLoadQuestFiles();

        Initialized = true;
        InitialiseCards();
    }

    public void UpdateLocationQuest(LocationQuest quest) {
        CheckInitialised();
        if (!locationQuests.ContainsKey(quest.Label)) {
            quest.SetOngoing();
            locationQuests.Add(quest.Label, quest);
        }
    }

    public void RemoveLocationQuest(string label) {
        CheckInitialised();
        locationQuests.Remove(label);
    }

    // This method returns a reference to a player with the given name.
    // If you want to update the player's stats, you should do so through the reference.
    public Player GetPlayer(string name) {
        CheckInitialised();
        if (MyPlayer.Name == name) {
            return MyPlayer;
        }
        foreach (Player player in OtherPlayers) {
            if (player.Name == name) {
                return player;
            }
        }
        return null;
    }

    public Player GetPlayerByID(string id) {
        CheckInitialised();
        if (MyPlayer.Id == id) {
            return MyPlayer;
        }
        foreach (Player player in OtherPlayers) {
            if (player.Id == id) {
                return player;
            }
        }
        return null;
    }

    public void FindAndLoadQuestFiles() {
        UnityEngine.Debug.LogWarning("Finding and loading quest files. ShouldUseDefaultQuestFiles: " + FileUtils.ShouldUseDefaultQuestFiles());
        if (FileUtils.ShouldUseDefaultQuestFiles()) {
            locationQuestVectors = FileUtils.LoadBytesFromResources("locationQuestVectors");
            locationQuestGraph = FileUtils.LoadBytesFromResources("locationQuestGraph");
            locationQuestLabels = FileUtils.LoadBytesFromResources("locationQuestLabels");
        } else {
            locationQuestVectors = FileUtils.Load<byte[]>("locationQuestVectors", "quests");
            locationQuestGraph = FileUtils.Load<byte[]>("locationQuestGraph", "quests");
            locationQuestLabels = FileUtils.Load<byte[]>("locationQuestLabels", "quests");
        }
    }

    public void AddCard(CardName card) {
        CheckInitialised();
        cardID++;
        MyCards.Add(cardID, card);
    }

    public void RemoveCard(int cardID) {
        CheckInitialised();
        MyCards.Remove(cardID);
    }

    public List<CardName> GetCards() {
        CheckInitialised();
        return new List<CardName>(MyCards.Values);
    }

    public bool HasCard(int cardID) {
        CheckInitialised();
        return MyCards.ContainsKey(cardID);
    }

    public CardName GetCard(int cardID) {
        CheckInitialised();
        return MyCards[cardID];
    }

    public List<int> GetCardIDs() {
        CheckInitialised();
        return new List<int>(MyCards.Keys);
    }

    public void InitialiseCards() {
        MyCards = new();
        cardID = 0;
        foreach (CardName card in InitialCards) {
            AddCard(card);
        }
    }

    // Medium Encounters
    public void AddFoundMediumEncounter(string encounterId) {
        foundMediumEncounters.Add(encounterId);
    }

    public GameStateMessage ToMessage() {
        CheckInitialised();
        Dictionary<string, string> playerRoles = new();
        playerRoles.Add(myID, MyPlayer.Role);
        foreach (Player player in OtherPlayers) {
            playerRoles.Add(player.Id, player.Role);
        }

        Dictionary<string, string> playerNames = new();
        playerNames.Add(myID, MyPlayer.Name);
        foreach (Player player in OtherPlayers) {
            playerNames.Add(player.Id, player.Name);
        }
        return new GameStateMessage(playerRoles, playerNames);
    }

    public void InitializeFromMessage(GameStateMessage message, string roomCode, string myID) {
        CheckNotInitialised();
        List<Player> players = new();
        foreach (string id in message.playerRoles.Keys) {
            string name = message.playerNames[id];
            string role = message.playerRoles[id];
            Player player = PlayerFactory.CreatePlayer(id, name, role);
            PlayersDetails.Add(id, player);
            if (id == myID) {
                MyPlayer = player;
            } else {
                players.Add(player);
            }
        }
        OtherPlayers = players;

        // Initialise other fields
        RoomCode = roomCode;
        this.myID = myID;
        isLeader = false;
        
        Initialized = true;
        InitialiseCards();
    }

    public void UpdateFromMessage(GameStateMessage message) {
        CheckInitialised();
        throw new NotImplementedException();
    }

    public void Reset() {
        RoomCode = "";
        MyPlayer = null;
        OtherPlayers = new();
        PlayersDetails = new();
        IsInEncounter = false;
        Score = 0;
        InitialiseCards();
        Initialized = false;
    }

    private void CheckInitialised() {
        if (!Initialized) {
            throw new InvalidOperationException("GameState not initialized.");
        }
    }

    private void CheckNotInitialised() {
        if (Initialized) {
            throw new InvalidOperationException("GameState already initialized.");
        }
    }


    // ------------------------------- ENCOUNTER -------------------------------
    public void StartEncounter(List<Monster> monsters, Dictionary<string, string> partyMembers) {
        CheckInitialised();
        if (IsInEncounter) {
            return;
        }
        encounterMonsters = monsters;
        this.partyMembers = partyMembers;
        IsInEncounter = true;
    }

    public void ExitEncounter() {
        IsInEncounter = false;
    }


    // --------------------------------  BATTLE --------------------------------
    public void ApplyBattleLossPenalty() {
        // Get all available card ids
        List<int> cardIds = MyCards.Keys.ToList();
        
        // randomly select half of the ids in cardsIds
        int halfCount = cardIds.Count / 2;
        List<int> selectedIds = new();
        System.Random random = new();
        for (int i = 0; i < halfCount; i++)
        {
            int randomIndex = random.Next(cardIds.Count);
            selectedIds.Add(cardIds[randomIndex]);
            cardIds.RemoveAt(randomIndex);
        }

        foreach (int id in selectedIds) {
            RemoveCard(id);
        }
    }

    // --------------------------------  QUESTS --------------------------------
    public async Task InitialiseQuests() {
        Debug.Log("Initialising quests.");
        // Initialise basic quests and set them in the GameState
        basicQuests = QuestFactory.CreateInitialBasicQuests();

        // Initialise location quests and set them in the GameState
        locationQuests = await QuestFactory.CreateInitialLocationQuests();
        QuestManager.Instance.GetNextLocationQuest();
    }
}

public class GameStateMessage : MessageInfo {
    public Dictionary<string, string> playerRoles;
    public Dictionary<string, string> playerNames;
    public MessageType messageType { get; set; }

    public GameStateMessage(Dictionary<string, string> playerRoles, Dictionary<string, string> playerNames) {
        this.playerRoles = playerRoles;
        this.playerNames = playerNames;
        this.messageType = MessageType.GAMESTATE;
    }

    public static GameStateMessage fromJson(string json) {
        return JsonConvert.DeserializeObject<GameStateMessage>(json);
    }

    public string toJson() {
        return JsonConvert.SerializeObject(this);
    }
}