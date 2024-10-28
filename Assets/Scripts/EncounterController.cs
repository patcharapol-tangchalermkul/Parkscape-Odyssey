using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using Microsoft.Geospatial;

public enum EncounterType {
    RANDOM_ENCOUNTER,
    MEDIUM_BOSS
}


public class EncounterController : MonoBehaviour
{
    [SerializeField]
    private GameObject encounterLobbyOverlay;
    private EncounterUIManager encounterUIManager;
    private EncounterLobbyUIManager encounterLobbyUIManager;

    [SerializeField]
    private GameObject monsterManager;
    private MonsterController monsterController;

    [SerializeField]
    private GameObject encounterSpawn;

    [SerializeField]
    private GameObject interfacePanel;

    [SerializeField]
    private GameObject encounterFoundPopup;

    [SerializeField]
    private GameObject randomEncounterFoundPopup;

    [SerializeField]
    private GameObject gameplayCanvas;

    [SerializeField]
    private GameObject mapRenderer;

    private string leaderId;

    private EncounterStatus encounterStatus = EncounterStatus.IDLE;

    // current encounter monster details
    private List<Monster> monsters;

    private string encounterId = "";
    private EncounterType encounterType;
    private bool isLeader = false;

    // p2p network
    private NetworkUtils network;
    private bool AcceptMessages = true;
    private readonly float baseFreq = 0.1f;
    private int msgFreq = 0;
    private int msgFreqCounter = 0;

    private bool receivedEncounter = false;
    private bool inEncounterLobby = false;

    private readonly Dictionary<string, string> partyMembers = new();

    public static EncounterController selfReference;

    private GameObject encounterFoundPopupInstance;

    private List<GameObject> encountersSpawned = new List<GameObject>();
    private Queue<string> encountersFoundQueue = new Queue<string>();

    private float MAX_SPAWN_AREA_X = 350f;
    private float MAX_SPAWN_AREA_Y = 700f;

    // Medium encounter message popup
    private MediumEncounterMsgPopUp mediumEncounterMsgPopUp;
    // FOR DEBUGGING ONLY
    [SerializeField]
    private bool debugIsLeader;

    void Awake() {
        if (!selfReference) {
			selfReference = this;
			DontDestroyOnLoad(gameObject);
		} else 
            Destroy(gameObject);
    }
    
    void Start() {
        // Setup p2p network
        msgFreq = GameState.Instance.maxPlayerCount;
        network = NetworkManager.Instance.NetworkUtils;
        encounterUIManager = GetComponent<EncounterUIManager>();
        monsterController = monsterManager.GetComponent<MonsterController>();
        mediumEncounterMsgPopUp = MediumEncounterMsgPopUp.selfReference;
    }

    // Location dependent encounter spawn, so need to have location initialized, or provide one.
    public void CreateMonsterSpawn(string encounterId, LatLon location, EncounterType type) {
        // Generate monsters for the encounter.
        List<Monster> monsters = GenerateEncounterMonsters(type);

        // Generates skill sequences for the monsters.
        List<List<SkillName>> skillSequences = GenerateMonsterSkillSequences(monsters);

        // Generate unique id for the encounter. (not needed for medium encounters)
        // string encounterId = Guid.NewGuid().ToString();
        GameObject monsterSpawn = null;
        Debug.Log("Location: (" + location.LatitudeInDegrees + ", " + location.LongitudeInDegrees + ")");
        if (type == EncounterType.RANDOM_ENCOUNTER) {
            monsterSpawn = mapRenderer.GetComponent<MapManager>().AddPinNearLocation(encounterSpawn, 50, 20, latitude: location.LatitudeInDegrees, longitude: location.LongitudeInDegrees); 
        } else {
            monsterSpawn = mapRenderer.GetComponent<MapManager>().AddPinNearLocation(encounterSpawn, 0, latitude: location.LatitudeInDegrees, longitude: location.LongitudeInDegrees);
            monsterSpawn.GetComponent<SpriteButtonLocationBounded>().onFound = () => {
                GameState.Instance.AddFoundMediumEncounter(encounterId);
            };
        }

        // Set encounter information to the pin
        monsterSpawn.GetComponent<SpriteButtonLocationBounded>().encounterId = encounterId;
        monsterSpawn.GetComponent<SpriteButtonLocationBounded>().encounterType = type;

        monsterSpawn.GetComponent<EncounterIconChanger>().SetEncounterType(type);
        EncounterSpawnManager encounterSpawnManager = monsterSpawn.GetComponent<EncounterSpawnManager>();
        encounterSpawnManager.EncounterSpawnInit(encounterId, monsters, type);

        encountersSpawned.Add(monsterSpawn);
        Debug.Log($"Created encounter spawn: {encounterId}");
    }

    private List<Monster> GenerateEncounterMonsters(EncounterType type) {
        List<Monster> monsters = new List<Monster>();

        // Select a random number between 1 and maxPlayerCount to determine the number of monsters in the encounter.
        int numMonsters = UnityEngine.Random.Range(1, GameState.Instance.maxPlayerCount);

        MonsterName monsterName = MonsterName.DRAGON;
        if (type == EncounterType.RANDOM_ENCOUNTER) {
            monsterName = MonsterName.GOBLIN;
        }
        // Select a type of monster for the encounter.
        // MonsterName monsterName = (MonsterName) UnityEngine.Random.Range(0, Enum.GetValues(typeof(MonsterName)).Length);

        // Create the monsters.
        for (int i = 0; i < numMonsters; i++) {
            Monster monster = monsterController.createMonster(monsterName);
            monsters.Add(monster);
        }

        return monsters;
    }

    private List<List<SkillName>> GenerateMonsterSkillSequences(List<Monster> monsters) {
        List<List<SkillName>> skillNameSequences = new List<List<SkillName>>();

        foreach (Monster monster in monsters) {
            List<SkillName> shuffledSkillNames = new List<SkillName>();
            List<int> shuffledIndex = Shuffle(monster.skills.Count - 1);
            foreach (int index in shuffledIndex) {
                shuffledSkillNames.Add(monster.skills[index].Name);
            }
            skillNameSequences.Add(shuffledSkillNames);
        }

        return skillNameSequences;
    }

    // Shows encounter lobby on the UI
    public void SpawnEncounterLobby(string encounterId, List<Monster> monsters) {
        this.monsters = monsters;
        this.encounterId = encounterId;

        GameObject encounterLobby = Instantiate(encounterLobbyOverlay);
        
        encounterLobbyUIManager = encounterLobby.GetComponent<EncounterLobbyUIManager>();
        encounterLobbyUIManager.EncounterLobbyInit(encounterId, monsters, isLeader);
    }

    // shuffle monster skill indexes
    private List<int> Shuffle(int listSize) {  
        List<int> indexes = Enumerable.Range(0, listSize + 1).ToList();

        while (listSize > 1) {
            // Select a random card from the front of the deck
            // (up to the current position to shuffle) to swap
            listSize--;
            int k = UnityEngine.Random.Range(0, listSize + 1);  
            
            // Swap cards[n] with cards[k]
            int toSwap = indexes[k];  
            indexes[k] = indexes[listSize];  
            indexes[listSize] = toSwap;  
        }
        return indexes;
    }

    // Called from encounter spawn manager when leader initiates the encounter lobby
    public void CreateEncounterLobby(string encounterId, List<Monster> monsters) {
        // only leader can start a medium encounter
        if (IsMediumEncounter(encounterId)) {
            // show pop up
            encounterType = EncounterType.MEDIUM_BOSS;
            bool isLeader = GameState.Instance.isLeader;
            if (GameState.MAPDEBUGMODE) {
                isLeader = debugIsLeader;
            }
            if (!isLeader) {
                mediumEncounterMsgPopUp.ShowMediumEncounterMessagePopup();
                return;
            }
        }
        isLeader = true;
        Debug.Log($"Spawning encounter lobby: {monsters[0].Health}");
        SpawnEncounterLobby(encounterId, monsters);

        // Add self as a member of the party in the encounter lobby
        partyMembers.Add(GameState.Instance.myID, GameState.Instance.PlayersDetails[GameState.Instance.myID].Name);
        
        // Broadcast to all players that an encounter has been found.
        BroadcastFoundEncounterMessage();

        // Disable map interactions
        MapManager.Instance.DisableMapInteraction();

        inEncounterLobby = true;
    }

    // Member accept join encounter
    public void AcceptJoinEncounter() {
        // AcceptMessages = true;
        Debug.Log("Accepting join encounter");
        CloseEncounterFoundPopup(accept:true);

        // Disable map interactions
        MapManager.Instance.DisableMapInteraction();

        // Send a message to the leader to request to join encounter.
        SendJoinEncounterMessage();
    }

    IEnumerator DelayShowEncounterFoundPopup(string encounterId) {
        yield return new WaitForSeconds(1);
        ShowEncounterFoundPopup(encounterId);
    }

    // Show encounter found popup
    private void ShowEncounterFoundPopup(string encounterId) {
        if (encounterFoundPopupInstance != null) {
            encountersFoundQueue.Enqueue(encounterId);
            return;
        }
        this.encounterId = encounterId;
        GameObject popup = Instantiate(encounterFoundPopup, gameplayCanvas.transform);
        foreach (Transform child in popup.transform) {
            if (child.name == "YesButton") {
                child.GetComponent<Button>().onClick.AddListener(AcceptJoinEncounter);
            }
        }
        encounterFoundPopupInstance = popup;
    }

    // Close encounter found popup
    public void CloseEncounterFoundPopup(bool accept = false) {
        GameObject popUpInstance = encounterFoundPopupInstance;
        Destroy(popUpInstance);
        encounterFoundPopupInstance = null;
        if (!accept) {
            if (encountersFoundQueue.Count > 0) {
                string encounterId = encountersFoundQueue.Dequeue();
                StartCoroutine(DelayShowEncounterFoundPopup(encounterId));
            } else {
                encounterId = "";
            }
        }
    }

    public void ShowRandomEncounterPopup(string encounterId, List<Monster> monsters) {
        randomEncounterFoundPopup.SetActive(true);

        this.monsters = monsters;
        this.encounterId = encounterId;
        this.encounterType = EncounterType.RANDOM_ENCOUNTER;

        // Add self as a member of the party in the encounter lobby
        partyMembers.Add(GameState.Instance.myID, GameState.Instance.PlayersDetails[GameState.Instance.myID].Name);

        // Disable map interactions
        MapManager.Instance.DisableMapInteraction();
    }

    public void CloseRandomEncounterPopup() {
        randomEncounterFoundPopup.SetActive(false);

        this.encounterId = "";
        partyMembers.Clear();

        // Enable map interactions
        MapManager.Instance.EnableMapInteraction();

        // Enable AR interaction.
        GameObject[] gameObjects;
        gameObjects = GameObject.FindGameObjectsWithTag("xrOrigin");
        if (gameObjects.Length == 0) {
            Debug.Log("No xrOrigin found");
        } else {
            GameObject xrOrigin = gameObjects[0];
            if (xrOrigin.activeInHierarchy) {
                xrOrigin.GetComponent<Depth_ScreenToWorldPosition>().EnableARInteraction();
            }
        }
    }

    public void LeaderStartEncounter() {
        // Save monster details for entering encounter
        Debug.Log($"isInEncounter: {GameState.Instance.IsInEncounter}");
        GameState.Instance.StartEncounter(monsters, partyMembers);
        Debug.Log("Leader Starting encounter with monsters: " + monsters[0].name + " " + monsters[0].Health);

        SendStartEncounterMessage();
        inEncounterLobby = false;
        AcceptMessages = false;

        if (encounterType == EncounterType.RANDOM_ENCOUNTER) {
            SceneManager.LoadScene("Battle", LoadSceneMode.Additive);
            randomEncounterFoundPopup.SetActive(false);
        }
    }

    private void MemberStartEncounter() {
        GameState.Instance.StartEncounter(monsters, partyMembers);
        Debug.Log("Member Starting encounter");
        GameObject.FindGameObjectWithTag("EncounterLobby").GetComponent<EncounterLobbyUIManager>().StartEncounter();
        inEncounterLobby = false;
        AcceptMessages = false;
    }

    public void KillMonster() {
        GameObject encounter = encountersSpawned.Find((spawn) => spawn.GetComponent<EncounterSpawnManager>().GetEncounterId() == encounterId);
        if (encounter != null) {
            encountersSpawned.Remove(encounter);
            // Instead of destroying the encounter, we can just disable it.
            encounter.GetComponent<EncounterIconChanger>().KillSprite();
        }

        // Remove ar encounter spawn from spawn queue
        EncounterObjectManager.Instance.RemoveEncounterToSpawnFromQueue(encounterId);
    }

    public void ExitEncounterLobby() {
        // Enable map interactions.
        MapManager.Instance.EnableMapInteraction();
        inEncounterLobby = false;
        encounterId = "";
        partyMembers.Clear();
    }

    public void OnFinishEncounter(bool win) {
        if (win)
            KillMonster();
        partyMembers.Clear();
        encounterId = "";
        AcceptMessages = true;
    }

    public bool IsMediumEncounter(string encounterId) {
        return GameState.Instance.mediumEncounterLocations.ContainsKey(encounterId);
    }

    // ------------------------------ P2P NETWORK ------------------------------
    public CallbackStatus HandleMessage(Message message) {
        // Ignore if no longer accepting messages.
        if (!AcceptMessages)
            return CallbackStatus.DORMANT;

        EncounterMessage encounterMessage = (EncounterMessage) message.messageInfo;

        switch (encounterMessage.Type) {
            case EncounterMessageType.FOUND_ENCOUNTER:
                // Encounter found by another player, show encounter found pop up.
                if (!isLeader) {
                    ShowEncounterFoundPopup(encounterMessage.encounterId);
                }
                break;
            case EncounterMessageType.JOIN_ENCOUNTER:
                if (isLeader && encounterId == encounterMessage.encounterId) {
                    // Leader adds player to the encounter lobby
                    Debug.Log("adding player to encounter lobby: " + encounterMessage.playerId);
                    string playerName = GameState.Instance.PlayersDetails[encounterMessage.playerId].Name;
                    partyMembers.Add(encounterMessage.playerId, playerName);
                    encounterLobbyUIManager.MemberJoinedParty(playerName);
                    // sends monster info to other players 
                    SendJoinedEncounterConfirmationMessage(encounterMessage.sendFrom);
                }
                break;
            case EncounterMessageType.JOINED_ENCOUNTER_CONFIRMATION:
                if (!isLeader && encounterId == encounterMessage.encounterId) {
                    // Players stop sending join encounter messages to leader and processes monster info
                    StopSendingJoinEncounterMessagesAndShowLobby(encounterMessage);
                }
                break;
            case EncounterMessageType.START_ENCOUNTER:
                // member receives notification from encounter leader to start encounter
                if (!isLeader && encounterId == encounterMessage.encounterId) {
                    MemberStartEncounter();
                }
                break;
        }

        return CallbackStatus.PROCESSED;
    }

    private void BroadcastFoundEncounterMessage() {
        EncounterMessage encounterMessage = new EncounterMessage(EncounterMessageType.FOUND_ENCOUNTER, encounterId : encounterId);
        network.broadcast(encounterMessage.toJson());
    }

    public void SendJoinEncounterMessage() {
        EncounterMessage encounterMessage = new EncounterMessage(EncounterMessageType.JOIN_ENCOUNTER, encounterId : encounterId, sendTo:leaderId);
        Debug.Log("Sending join encounter message to leader: " + leaderId);
        network.broadcast(encounterMessage.toJson());
    }

    private void SendStartEncounterMessage() {
        EncounterMessage encounterMessage = new EncounterMessage(EncounterMessageType.START_ENCOUNTER, encounterId: encounterId);
        network.broadcast(encounterMessage.toJson());
    }

    // Sends confirmation to player for joining encounter lobby together with monster details
    private void SendJoinedEncounterConfirmationMessage(string toSend) {
        List<MonsterName> monsterNames = new List<MonsterName>();
        List<int> health = new List<int>();
        List<int> defense = new List<int>();
        List<int> defenseAmount = new List<int>();
        List<int> baseDamage = new List<int>();
        List<EnemyLevel> levels = new List<EnemyLevel>();
        List<List<SkillName>> skills = new();
        foreach (Monster monster in monsters) {
            monsterNames.Add(monster.name);
            health.Add(monster.Health);
            defense.Add(monster.Defense);
            defenseAmount.Add(monster.defenseAmount);
            baseDamage.Add(monster.BaseDamage);
            levels.Add(monster.level);
            skills.Add(monster.skills.Select(skill => skill.Name).ToList());
        }
        Debug.Log($"Responding to: {toSend}");
        EncounterMessage encounterMessage 
          = new EncounterMessage(EncounterMessageType.JOINED_ENCOUNTER_CONFIRMATION, partyMembers,
                                monsterNames, health, defense, defenseAmount, baseDamage, skills, levels, encounterId : encounterId, sendTo:toSend);
        network.broadcast(encounterMessage.toJson());
    }

    private void StopSendingJoinEncounterMessagesAndShowLobby(EncounterMessage encounterMessage) {
        if (encounterMessage.sendTo == GameState.Instance.myID && !inEncounterLobby) {
            List<Monster> monsters = ProcessEncounterMessageWithMonsterInfo(encounterMessage);
            SpawnEncounterLobby(encounterMessage.encounterId, monsters);
            inEncounterLobby = true;
            encounterType = EncounterType.MEDIUM_BOSS;
            ListPartyMembers(encounterMessage.members);
        } else if (encounterMessage.sendTo != GameState.Instance.myID && inEncounterLobby) {
            ListPartyMembers(encounterMessage.members);
        }
    }

    // Process List of monster info to make a list of monsters
    private List<Monster> ProcessEncounterMessageWithMonsterInfo(EncounterMessage encounterMessage) {
        List<Monster> monsters = new List<Monster>();
        List<MonsterName> monsterNames = encounterMessage.names;
        List<int> health = encounterMessage.Health;
        List<int> defense = encounterMessage.Defense;
        List<int> defenseAmount = encounterMessage.defenseAmount;
        List<int> baseDamage = encounterMessage.BaseDamage;
        List<EnemyLevel> levels = encounterMessage.level;
        for (int i = 0; i < monsterNames.Count; i++) {
            List<Skill> skills = GetSkillsFromSkillNames(encounterMessage.skills[i]);
            Sprite img = monsterController.GetMonsterSprite(monsterNames[i]);
            Monster monster = MonsterFactory.CreateMonsterWithValues(
                monsterNames[i], img, health[i], defense[i], defenseAmount[i], 
                baseDamage[i], skills, levels[i]);
            monsters.Add(monster);
        }
        return monsters;
    }

    // get corresponding skills from skillname list
    private List<Skill> GetSkillsFromSkillNames(List<SkillName> skillNames) {
        List<Skill> skills = new List<Skill>();
        foreach (SkillName skillName in skillNames) {
            skills.Add(MonsterFactory.skillsController.Get(skillName));
        }
        return skills;
    }

    private void ListPartyMembers(Dictionary<string, string> members) {
        foreach (string id in members.Keys) {
            if (partyMembers.ContainsKey(id))
                continue;
            partyMembers.Add(id, members[id]);
            string playerName = GameState.Instance.PlayersDetails[id].Name;
            encounterLobbyUIManager.MemberJoinedParty(playerName);
        }
    }
}


public enum EncounterMessageType {
    FOUND_ENCOUNTER,
    JOIN_ENCOUNTER,
    JOINED_ENCOUNTER_CONFIRMATION,
    START_ENCOUNTER,
}

enum EncounterStatus {
    IDLE,
    START_LOBBY,
    JOINING_LOBBY,
    JOINED_LOBBY,
    RECEIVED_ENCOUNTER_POPUP,
}


public class EncounterMessage : MessageInfo
{
    public MessageType messageType {get; set;}
    public EncounterMessageType Type {get; set;}
    public Dictionary<string, string> members;
    public string playerId {get; set;}
    public List<MonsterName> names;

    public List<int> Health { get; private set; } = new List<int>();

    public List<int> Defense { get; private set; } = new List<int>();

    public List<int> defenseAmount = new List<int>();

    public List<int> BaseDamage { get; private set; } = new List<int>();

    public List<List<SkillName>> skills = new List<List<SkillName>>();

    public List<EnemyLevel> level = new List<EnemyLevel>();
    public string encounterId = "";

    public string sendTo {get; set;}
    public string sendFrom {get; set;}

    public EncounterMessage(EncounterMessageType type, string encounterId = "", string sendTo = "", string sendFrom = "") {
        messageType = MessageType.ENCOUNTERMESSAGE;
        playerId = GameState.Instance.myID;
        Type = type;
        this.encounterId = encounterId;
        this.sendTo = sendTo == null ? "" : sendTo;
        this.sendFrom = sendFrom == "" ? GameState.Instance.myID : sendFrom;
    }

    [JsonConstructor]
    public EncounterMessage(EncounterMessageType type, Dictionary<string, string> members, List<MonsterName> names, List<int> health, List<int> defense, List<int> defenseAmount, 
        List<int> baseDamage, List<List<SkillName>> skills, List<EnemyLevel> level, string encounterId = "", string sendTo = "", string sendFrom = "") {
        messageType = MessageType.ENCOUNTERMESSAGE;
        Type = type;
        this.members = members;
        this.names = names;
        Health = health;
        Defense = defense;
        this.defenseAmount = defenseAmount;
        BaseDamage = baseDamage;
        this.skills = skills;
        this.level = level;
        this.encounterId = encounterId;
        this.sendTo = sendTo == null ? "" : sendTo;
        this.sendFrom = sendFrom == "" ? GameState.Instance.myID : sendFrom;
    }

    public string toJson() {
        return JsonConvert.SerializeObject(this);
    }
}