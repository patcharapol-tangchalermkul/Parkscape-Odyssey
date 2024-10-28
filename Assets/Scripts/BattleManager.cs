using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Newtonsoft.Json;
using TMPro;
using System.Data.Common;

public class BattleManager : MonoBehaviour {
    public static BattleManager selfReference;

    public const int HAND_SIZE = 5;
    
    private GameManager gameManager;
    private GameInterfaceManager gameInterfaceManager;
    private BattleUIManager battleUIManager;
    private MonsterController monsterController;
    private CardsUIManager cardsUIManager;
    private List<CardName> allCards;
    private List<CardName> hand;

    [SerializeField]
    private GameObject playerCurrentHealth;

    [SerializeField]
    private GameObject playerCurrentMana;

    [SerializeField]
    private GameObject playerMaxMana;

    [SerializeField]
    private GameObject playerDrawPileNumber;

    [SerializeField]
    private GameObject playerDiscardPileNumber;
    
    [SerializeField]
    private GameObject endTurnButton;

    [SerializeField]
    private GameObject lootOverlay;

    private List<GameObject> otherPlayerStat = new List<GameObject>();

    // monster
    private List<Monster> monsters;
    private (SkillName, List<string>) monsterAttack;


    // p2p networking
    private NetworkUtils network;
    private bool isLeader = false;
    private bool AcceptMessages = false;
    private int turn = 0;
    

    public List<CardName> Hand {
        get { return hand; }
        private set {}
    }
    private Queue<CardName> drawPile;
    private Queue<CardName> discardPile;

    private Monster monster;

    private List<string> playerOrderIds;

    private List<CardName> cardsToPlay;

    private Dictionary<string, List<CardName>> othersCardsToPlay = new();
    private Dictionary<string, string> partyMembers;
    private Dictionary<string, Player> partyMembersInfo = new();
    private BattleStatus battleStatus = BattleStatus.TURN_IN_PROGRESS;
    private AudioSource backgroundAudioSource;

    void Awake() {
        if (!selfReference) {
            selfReference = this;
            gameManager = FindObjectOfType<GameManager>();
            gameInterfaceManager = (GameInterfaceManager) FindObjectOfType(typeof(GameInterfaceManager));
            backgroundAudioSource = (AudioSource) GameObject.FindWithTag("BackgroundAudioSource").GetComponent(typeof(AudioSource));
            battleUIManager = (BattleUIManager) GetComponent(typeof(BattleUIManager));
            monsterController = (MonsterController) GetComponent(typeof(MonsterController));
            network = NetworkManager.Instance.NetworkUtils;
        } else {
            Destroy(gameObject);
        }

        // Stop playing background music
        backgroundAudioSource.Stop();


        // Setup p2p network
        isLeader = GameState.Instance.isLeader;
    }

    void Start() {
        AcceptMessages = true;
        turn = 1;

        // Get party members and monsters info
        Debug.Log(GameState.Instance.encounterMonsters[0].name);
        Debug.Log(GameState.Instance.encounterMonsters[0].Health);
        monsters = GameState.Instance.encounterMonsters;
        // skillsSequences = GameState.Instance.skillSequences;
        partyMembers = GameState.Instance.partyMembers;

        // Search for the CardsUIManager here because in Awake() it is not initialised yet
        cardsUIManager = (CardsUIManager) FindObjectOfType(typeof(CardsUIManager));

        // Initialise the list of selected cards
        cardsToPlay = new List<CardName>();
        
        // Set the encounter to be the active scene
        // Cannot be run in Awake() as the scene is not loaded at that point
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("Battle"));

        // Delegate OnSceneUnloaded() to run when this scene unloads
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // Shuffle the player's cards
        allCards = new List<CardName>(GameState.Instance.GetCards());
        Shuffle(this.allCards);

        // Add the shuffled cards to a queue to draw from
        drawPile = new Queue<CardName>(this.allCards);
        discardPile = new Queue<CardName>();

        // Select a random monster to fight
        // MonsterName monsterName = monsterController.GetRandomMonster();
        // monster = monsterController.createMonster(monsterName);

        // Print which monster the player is fighting
        string monsterName = monsters[0].name.ToString();
        Debug.Log(string.Format("Fighting {0}.", monsterName));

        // Start the turn
        StartTurn();

        // Display the hand and monster
        battleUIManager.DisplayHand(hand); 
        battleUIManager.DisplayMonster(monsters[0]);

        // Initializes Player's health, current mana and max mana
        UpdatesPlayerStats();

        // Initializes Player's card number
        UpdateCardNumber();

        // Initializes the Other Players panel and Speed panel
        GetAllOtherPartyMembersInfo();
        otherPlayerStat = battleUIManager.DisplayOtherPlayers(partyMembersInfo.Values.ToList());
        List<Player> allPlayers = new List<Player>(partyMembersInfo.Values.ToList());
        allPlayers.Add(GameState.Instance.MyPlayer);
        battleUIManager.DisplaySpeedPanel(allPlayers);

        Debug.Log("Length of otherPlayerStat: " + otherPlayerStat.Count);

        // Initializes Player's Stat
        InitializesPlayerStat();

        // StartCoroutine(UnloadTheScene());
        UpdatePlayerOrder();
    }

    void Update() {
        if (battleStatus != BattleStatus.TURN_IN_PROGRESS) {
            endTurnButton.SetActive(false);
        } else {
            endTurnButton.SetActive(true);
        }
    }

    private void GetAllOtherPartyMembersInfo() {
        foreach (string id in partyMembers.Keys) {
            if (id == GameState.Instance.myID)
                continue;
            Player player = GameState.Instance.OtherPlayers.Find((player) => player.Id == id);
            partyMembersInfo.Add(id, player);
        }
    }

    // TODO: Implement the logic for playing a card, including mana checking
    public void PlayCard(int cardIndex) {
        CardName card = hand[cardIndex];

        // Play the card with the given index, if possible
        if (GameState.Instance.MyPlayer.Mana < cardsUIManager.findCardDetails(card).cost) {
            // The card is too expensive
            Debug.Log("Too expensive");
            battleUIManager.RepositionCards();
            return;
        }

        Debug.Log(string.Format("Playing card: {0}.", card));
        cardsToPlay.Add(card);

        // Add the card to the discard pile
        discardPile.Enqueue(hand[cardIndex]);

        // Remove the card from the hand
        hand.RemoveAt(cardIndex);

        // Update the hand on-screen
        battleUIManager.RemoveCardFromHand(cardIndex);

        // Reduce the player's mana by the card cost
        GameState.Instance.MyPlayer.PlayCard(cardsUIManager.findCardDetails(card));

        // Update the player's stats in the UI
        UpdatesPlayerStats();

        // Update the player's card number UI
        UpdateCardNumber();

        // Update the other players' stats UI
        UpdateOtherPlayerStats();

        // Update the player order UI
        UpdatePlayerOrder();

        // // Draw 5 cards if the hand is empty
        // if (hand.Count == 0) {
        //     for (int i = 0; i < HAND_SIZE; i++) {
        //         DrawCard();
        //     }
        // }
    }

    public void StartTurn() {
        Debug.Log("Started turn");

        if (GameState.Instance.MyPlayer.IsDead()) {
            battleStatus = BattleStatus.DEAD;
        } else {
            battleStatus = BattleStatus.TURN_IN_PROGRESS;
        }

        Player myPlayer = GameState.Instance.MyPlayer;

        myPlayer.ResetMana();
        UpdatesPlayerStats();
    
        GenerateHand();
        // Debug.Log(string.Format("Generated hand: ({0}).", string.Join(", ", this.hand)));
    }


    // Turn end
    public void EndTurn() {
        battleStatus = BattleStatus.TURN_ENDED;
        

        if (!isLeader) {
            // Send my played cards to leader if I'm not the leader
            BroadcastCardsPlayed();
        } else {
            Debug.Log("Generating monster attack...");
            monsterAttack = GenerateMonsterAttack();

            Debug.Log("Generated monster attack!");
            BroadcastAllPhaseInfo();
            Debug.Log("Broadcasted phase info!");
            CheckProceedToResolve();
        }
    }

    // Resolve end of turn actions
    public void EndOfTurnActions() {
        ResolvePlayedCardsOrder();
        MonsterAttack();
        int battleStatus = BattleEnded();
        if (battleStatus == -1) {
            // Battle hasn't ended, start next turn
            StartTurn();
            UpdateCardNumber();

            if (GameState.Instance.MyPlayer.IsDead()) {
                // Player is dead, don't display hand and display dead panel
                battleUIManager.PlayerDeadUI();
            } else {
                // Player is alive, display hand
                battleUIManager.DisplayHand(hand);
            }
        } else {
            // End the encounter
            ResetAllPartyMemberStats();

            // Reset variables in various classes
            GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterController>().OnFinishEncounter(battleStatus == 0);
            battleUIManager.ResetUI();
            GameState.Instance.ExitEncounter();

            // Unload the battle scene
            SceneManager.UnloadSceneAsync("Battle");

            if (battleStatus == 0 && !GameState.Instance.MyPlayer.IsDead()) {
                // Party victory and player not dead, show loot overlay
                Instantiate(lootOverlay);
            } else if (battleStatus == 1) {
                // Party wiped out, apply penalty
                MapManager.Instance.EnableMapInteraction();
                GameState.Instance.ApplyBattleLossPenalty();
            }
        }
    }

    // Checks whether the game has ended and returns the status of battle
    // -1 : Game hasn't ended
    //  0 : Players won
    //  1 : Players lost
    private int BattleEnded() {
        if (monsters[0].Health <= 0) {
            Debug.Log("Battle ended with monster dead, party victory");
            return 0;
        }
        // Check if all players have died
        foreach (string id in partyMembers.Keys) {
            if (GameState.Instance.PlayersDetails[id].CurrentHealth > 0) {
                Debug.Log("Battle hasn't ended");
                return -1;
            }
        }
        Debug.Log("Battle ended with party wiped out");
        return 1;
    }

    private void ResetAllPartyMemberStats() {
        // Reset own mana
        GameState.Instance.MyPlayer.ResetMana();
        if (GameState.Instance.MyPlayer.IsDead()) {
            GameState.Instance.MyPlayer.Revive();
        }

        // Reset other players mana
        foreach (Player player in partyMembersInfo.Values) {
            player.ResetMana();
            if (player.IsDead()) {
                player.Revive();
            }
        }
    }

    private void DrawCard() {
        // Check whether the draw pile is empty, and reshuffle if so
        if (drawPile.Count == 0) {
            Shuffle(allCards);
            foreach (CardName card in allCards) {
                drawPile.Enqueue(card);
            }
        }

        // Add a card to the hand
        CardName drawnCard = drawPile.Dequeue();
        hand.Add(drawnCard);
        battleUIManager.AddToHand(drawnCard);
    }

    // Resolve played cards order based on player's speed stats
    private void ResolvePlayedCardsOrder() {
        Debug.Log("Resolving cards");
        List<Player> players = new();
        foreach (string id in partyMembers.Keys) {
            players.Add(GameState.Instance.PlayersDetails[id]);
        } 
        // order players based on their speed stat
        players = players.OrderByDescending(player => player.Speed).ToList();
        playerOrderIds = players.Select(player => player.Id).ToList();
        foreach (string id in playerOrderIds) {
            List<CardName> cards  = new();
            if (id == GameState.Instance.MyPlayer.Id) {
                cards = cardsToPlay;
            } else {
                cards = othersCardsToPlay[id];
            }
            foreach (CardName card in cards) {
                // Resolve Played card
                Debug.Log("Resolving played card: " + card);
                Card cardPlayed = cardsUIManager.findCardDetails(card);
                cardPlayed.UseCard(players, monsters, id);
                UpdatesPlayerStats();
                UpdateOtherPlayerStats();
                UpdateMonsterStats();
            }
        }


        // Clear everyones card played
        turn ++;
        othersCardsToPlay.Clear();
        cardsToPlay = new();
    }


    // -------------------------------- MONSTER --------------------------------
    private (SkillName, List<string>) GenerateMonsterAttack() {
        Monster attackingMonster = monsters[0];

        // Get a random skill from the monsters available skills
        System.Random random = new();
        int skillIndex = random.Next(attackingMonster.skills.Count);
        Skill skill = attackingMonster.skills[skillIndex];

        Debug.Log($"Skill is of type {skill.SkillType}");
        Debug.Log(partyMembersInfo.Values.ToList().Count);

        // Select player targets based on skill chosen
        List<Player> partyMembersLs = partyMembersInfo.Values.ToList();
        partyMembersLs.Add(GameState.Instance.MyPlayer);
        List<Player> targets = MonsterFactory.skillsController.SelectTargets(skill, partyMembersLs);

        // Get the ids of the target players
        List<string> targetPlayerIds = targets.Select(player => player.Id).ToList();
        
        return (skill.Name, targetPlayerIds);
    }

    // Monster Attacks
    public void MonsterAttack() {
        SkillName skillName = monsterAttack.Item1;

        List<Player> targets = new();
        foreach (string id in monsterAttack.Item2) {
            targets.Add(GameState.Instance.PlayersDetails[id]);
        } 
        
        MonsterFactory.skillsController.Get(skillName).Perform(monsters[0], targets);
        Debug.Log("Monster attacking with " + skillName);
        UpdatesPlayerStats();
        UpdateOtherPlayerStats();
        UpdateMonsterStats();
    }

    // ------------------------------ UI UPDATES ------------------------------

    // Update monster stats based on played cards
    private void UpdateMonsterStats() {
        battleUIManager.UpdateMonsterStats(monsters);
    }

    // Updates Player's health, current mana and max mana
    private void UpdatesPlayerStats() {
        Player myPlayer = GameState.Instance.MyPlayer;
        playerCurrentHealth.GetComponent<TextMeshProUGUI>().text = myPlayer.CurrentHealth.ToString();
        playerCurrentMana.GetComponent<TextMeshProUGUI>().text = myPlayer.Mana.ToString();
        playerMaxMana.GetComponent<TextMeshProUGUI>().text = myPlayer.MaxMana.ToString();
    }

    private void UpdateCardNumber() {
        playerDrawPileNumber.GetComponent<TextMeshProUGUI>().text = (drawPile.Count).ToString();
        playerDiscardPileNumber.GetComponent<TextMeshProUGUI>().text = (discardPile.Count).ToString();
    }

    // Initializes other Player's health and icon.
    private void InitializesPlayerStat() {
        List<Player> otherPlayers = partyMembersInfo.Values.ToList();
        Debug.Log(otherPlayers);
        for (int i = 0; i < GameState.Instance.maxPlayerCount - 1; i++) {
            if (i < otherPlayers.Count) {
                if (otherPlayers[i].Icon == null) {
                    Debug.Log(gameInterfaceManager);
                    otherPlayers[i].Icon = gameInterfaceManager.GetIcon(otherPlayers[i].Role);
                }
                otherPlayerStat[i].transform.GetChild(0).GetComponent<Image>().sprite = otherPlayers[i].Icon;
                otherPlayerStat[i].transform.GetChild(1)
                                  .transform.GetChild(0)
                                  .GetComponent<TextMeshProUGUI>().text = otherPlayers[i].CurrentHealth.ToString();
            }
        }
    }

    // Updates other Player's health.
    private void UpdateOtherPlayerStats() {
        List<Player> otherPlayers = partyMembersInfo.Values.ToList();
        for (int i = 0; i < otherPlayers.Count; i++) {
            otherPlayerStat[i].transform.GetChild(1)
                                .transform.GetChild(0)
                                .GetComponent<TextMeshProUGUI>().text = otherPlayers[i].CurrentHealth.ToString();
        }
    }

    // Update the order of players based on their speed
    private void UpdatePlayerOrder() {
        List<Player> players = new List<Player>(partyMembersInfo.Values.ToList());
        players.Add(GameState.Instance.MyPlayer);
        players = players.OrderByDescending(player => player.Speed).ToList();
        playerOrderIds = players.Select(player => player.Id).ToList();
        
        // rearrange the order of otherPlayerStats based on the playerOrderIds, exluding the current player
        List<string> otherPlayerIds = playerOrderIds.Where(id => id != GameState.Instance.MyPlayer.Id).ToList();
        battleUIManager.arrangeOtherPlayersInOrder(otherPlayerIds);
        battleUIManager.arrangeSpeedPanelInOrder(playerOrderIds);
    }

    // Shuffle the list of cards from back to front 
    private static void Shuffle(List<CardName> cards) {
        int n = cards.Count;
        while (n > 1) {
            // Select a random card from the front of the deck
            // (up to the current position to shuffle) to swap
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);  
            
            // Swap cards[n] with cards[k]
            CardName toSwap = cards[k];  
            cards[k] = cards[n];  
            cards[n] = toSwap;  
        }
    }

    private void GenerateHand() {
        // Initialise the hand as an empty list if not already done
        // If this has been done, just clear the list
        if (hand is null) {
            hand = new List<CardName>();
        } else {
            foreach (CardName card in hand) {
                discardPile.Enqueue(card);
            }
            for (int i = 0; i < hand.Count; i++) {
                battleUIManager.RemoveCardFromHand(0);
            }
            hand = new();
        }

        // Do not draw a hand if the player is dead
        if (GameState.Instance.MyPlayer.IsDead()) {
            return;
        }

        while (hand.Count < HAND_SIZE) {
            // Check whether the draw pile is empty, and reshuffle if so
            if (drawPile.Count == 0) {
                List<CardName> discardPileLs = discardPile.ToList();
                Shuffle(discardPileLs);
                foreach (CardName card in discardPileLs) {
                    drawPile.Enqueue(card);
                }
                discardPile = new();
            }

            // Add a card to the hand
            // DrawCard();
            hand.Add(drawPile.Dequeue());
            // Debug.Log($"Hand count after draw one: {hand.Count}");
        }
        // battleUIManager.RepositionCards();
    }

    private void CheckProceedToResolve() {
        Debug.Log("Checking to proceed to resolve");
        int notReceived = 0;
        foreach (string id in partyMembers.Keys) {
            if (id == GameState.Instance.myID || othersCardsToPlay.ContainsKey(id)) {
                Debug.Log("Already received cards from " + id);
                continue;
            }
            Debug.Log("Not received cards from " + id);
            notReceived++;
        }

        if (notReceived == 0) {
            battleStatus = BattleStatus.RESOLVING_PLAYED_CARDS;
            EndOfTurnActions();
        }
    }

    private void OnSceneUnloaded(Scene current) {
        Debug.Log("Battle scene unloaded.");

        // Remove this delegated function ref, or it will accumulate and run
        // multiple times the next time this scene unloads
        SceneManager.sceneUnloaded -= OnSceneUnloaded;


        // Inform the game manager the encounter has ended
        gameManager.EndEncounter(5);
    }

    IEnumerator UnloadTheScene() {
        float secondsToWait = 10;
        yield return new WaitForSeconds(secondsToWait);
        Debug.Log("Waited 10s to end battle.");

        SceneManager.UnloadSceneAsync("Battle");
    }


    // ------------------------------ P2P NETWORK ------------------------------
    public void SendMessages(Dictionary<string, string> connectedPlayers, List<string> disconnectedPlayers) {
        if (network == null)
            return;

        if (!AcceptMessages) {
            Debug.Log("Not accepting messages.");
            return;
        }
        
        if (battleStatus == BattleStatus.TURN_ENDED && isLeader) {
            // Request played card from players that we have not received cards from
            SendRequestForCardsPlayed();
        }

    }
    
    public CallbackStatus HandleMessage(Message message) {    
        BattleMessage battleMessage = (BattleMessage) message.messageInfo;

        List<string> sendTos = battleMessage.SendTos;
        if (sendTos.Count() != 0 && !sendTos.Contains(GameState.Instance.myID)) {
            return CallbackStatus.DORMANT;
        }
        if (battleMessage.Type == BattleMessageType.REQUEST_PLAYED_CARDS) {
            // If I am still playing the turn, ignore request
            if (battleStatus == BattleStatus.TURN_IN_PROGRESS) {
                return CallbackStatus.DORMANT;
            }

            // Ignore message if its not the correct turn
            if (battleMessage.Turn != turn) {
                return CallbackStatus.DORMANT;
            }

            Debug.Log("Received request for my played cards.");

            // Send my played cards
            BroadcastCardsPlayed();
            return CallbackStatus.PROCESSED;
        } else if (battleMessage.Type == BattleMessageType.PLAYED_CARDS) {
            Debug.Log("Received other player played cards.");
            // Ignore message if I am not the leader
            if (!isLeader) {
                return CallbackStatus.DORMANT;
            }

            // Ignore message if its not the correct turn
            if (battleMessage.Turn != turn) {
                return CallbackStatus.DORMANT;
            }

            // Process played cards
            if (!othersCardsToPlay.ContainsKey(battleMessage.SendFrom)) {
                othersCardsToPlay.Add(battleMessage.SendFrom,  battleMessage.CardsPlayed);
            }


            if (battleStatus != BattleStatus.TURN_IN_PROGRESS) {
                // My turn has ended, checking if I can proceed to resolve phase
                BroadcastAllPhaseInfo();
                CheckProceedToResolve();
            }
            return CallbackStatus.PROCESSED;
        } else if (battleMessage.Type == BattleMessageType.ALL_PLAYED_CARDS) {
            // Ignore message if I am leader
            if (isLeader) {
                return CallbackStatus.DORMANT;
            }

            // Ignore message if its not the correct turn
            if (battleMessage.Turn != turn) {
                return CallbackStatus.DORMANT;
            }

            // Ignore message if already in resolving cards phase
            if (battleStatus == BattleStatus.RESOLVING_PLAYED_CARDS || battleStatus == BattleStatus.TURN_IN_PROGRESS) {
                return CallbackStatus.DORMANT;
            }

            // Process all players played cards
            foreach (string id in battleMessage.AllCardsPlayed.Keys) {
                // Ignore own cards played and processed players cards played
                if (id == GameState.Instance.myID || othersCardsToPlay.ContainsKey(id)) {
                    continue;
                }
                othersCardsToPlay.Add(id,  battleMessage.AllCardsPlayed[id]);
            }

            // Load monster attack
            monsterAttack = battleMessage.MonsterAttack;

            if (battleStatus != BattleStatus.TURN_IN_PROGRESS) { 
                // My turn has ended, checking if I can proceed to resolve phase
                CheckProceedToResolve();
            }
        }
        Debug.Log("Unhandled battle message type");
        return CallbackStatus.DORMANT;
    }

    private void BroadcastCardsPlayed() {
        BattleMessage cardsPlayedMessage = new(BattleMessageType.PLAYED_CARDS, turn, cardsToPlay, sendTos : new());
        network.broadcast(cardsPlayedMessage.toJson());
    }

    private void BroadcastAllPhaseInfo() {
        Dictionary<string, List<CardName>> toSendLs = new(othersCardsToPlay)
        {
            { GameState.Instance.myID, cardsToPlay }
        };

        BattleMessage allPlayersCardsPlayedMessage = new(BattleMessageType.ALL_PLAYED_CARDS, turn, toSendLs, monsterAttack, sendTos : new());
        network.broadcast(allPlayersCardsPlayedMessage.toJson());
    }

    private void SendRequestForCardsPlayed() {
        List<string> sendTos = new();
        foreach (string id in partyMembers.Keys) {
            if (id == GameState.Instance.myID || othersCardsToPlay.ContainsKey(id)) {
                continue;
            }
            sendTos.Add(id);
        }
        if (sendTos.Count() > 0) {
            BattleMessage battleMessageRequest = new(BattleMessageType.REQUEST_PLAYED_CARDS, turn, sendTos: sendTos);
            network.broadcast(battleMessageRequest.toJson());
        }
    }
}

public enum BattleMessageType {
    REQUEST_PLAYED_CARDS,
    PLAYED_CARDS,
    ALL_PLAYED_CARDS,
}

public enum BattleStatus {
    TURN_IN_PROGRESS,
    TURN_ENDED,
    RESOLVING_PLAYED_CARDS,
    DEAD
}

public class BattleMessage : MessageInfo
{
    public MessageType messageType {get; set;}
    public BattleMessageType Type {get; set;}
    public int Turn;
    public List<CardName> CardsPlayed {get; set;}
    public Dictionary<string, List<CardName>> AllCardsPlayed {get; set;}
    public (SkillName, List<string>) MonsterAttack {get; set;}
    public string SendFrom {get; set;}
    public List<string> SendTos {get; set;}

    public BattleMessage(BattleMessageType type, int turn, List<string> sendTos, string sendFrom = "") {
        messageType = MessageType.BATTLEMESSAGE;
        Type = type;
        Turn = turn;
        CardsPlayed = new();
        AllCardsPlayed = new();
        SendTos = sendTos == null ? new() : sendTos;
        SendFrom = sendFrom == "" ? GameState.Instance.myID : sendFrom;
    }

    public BattleMessage(BattleMessageType type, int turn, List<CardName> cardsPlayed, List<string> sendTos, string sendFrom = "") {
        messageType = MessageType.BATTLEMESSAGE;
        Type = type;
        Turn = turn;
        CardsPlayed = cardsPlayed == null ? new() : cardsPlayed;
        AllCardsPlayed = new();
        SendTos = sendTos == null ? new() : sendTos;
        SendFrom = sendFrom == "" ? GameState.Instance.myID : sendFrom;
    }

    public BattleMessage(BattleMessageType type, int turn, Dictionary<string, List<CardName>> allCardsPlayed, (SkillName, List<string>) monsterAttack, List<string> sendTos, string sendFrom = "") {
        messageType = MessageType.BATTLEMESSAGE;
        Type = type;
        Turn = turn;
        CardsPlayed = new();
        AllCardsPlayed = allCardsPlayed == null ? new() : allCardsPlayed;
        MonsterAttack = monsterAttack;
        SendTos = sendTos == null ? new() : sendTos;
        SendFrom = sendFrom == "" ? GameState.Instance.myID : sendFrom;
    }

    [JsonConstructor]
    public BattleMessage(BattleMessageType type, int turn, List<CardName> cardsPlayed, Dictionary<string, 
                            List<CardName>> allCardsPlayed, (SkillName, List<string>) monsterAttack, List<string> sendTos, string sendFrom = "") {
        messageType = MessageType.BATTLEMESSAGE;
        Type = type;
        Turn = turn;
        CardsPlayed = cardsPlayed == null ? new() : cardsPlayed;
        AllCardsPlayed = allCardsPlayed == null ? new() : allCardsPlayed;
        MonsterAttack = monsterAttack;
        SendTos = sendTos == null ? new() : sendTos;
        SendFrom = sendFrom == "" ? GameState.Instance.myID : sendFrom;
    }

    public string toJson() {
        return JsonConvert.SerializeObject(this);
    }
}