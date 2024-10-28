using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.UI;

public class TradeManager : MonoBehaviour {
    public static TradeManager selfReference;

    // UI
    [SerializeField]
    private GameObject playerIcon;
    [SerializeField]
    private TMP_Text playerName;

    [SerializeField]
    private GameObject cardObject;

    [SerializeField]
    private TMP_Text inventoryMessage;

    [SerializeField]
    private TMP_Text tradeMessageObject;

    [SerializeField]
    private GameObject interfaceParent;
    
    [SerializeField]
    private GameObject cardsManagerPrefab;

    [SerializeField]
    private GameObject mapBlocker;
    private GameObject cardsManager;

    private const string tradeDeclinedMessage = "Card was declined...";
    private const string tradeAcceptedMessage = "Card was accepted!";
    private const string tradeLostMessage = "The card was lost on the way...";
    private const string tradeCompleteMessage = "You received the card!";

    // Both player fields.
    private bool tradeInProgress = false;
    private bool acceptTrades = true;
    private bool acceptTrade = false;

    private NetworkUtils network;

    // Sender fields.
    private Player tradeTo;
    private int cardID = -1;

    // Receiver fields.
    private string tradeFromID = "";
    private CardName cardName;
    
    // Start is called before the first frame update
    void Awake() {
        if (!selfReference) {
            selfReference = this;
            network = NetworkManager.Instance.NetworkUtils;
            cardsManager = Instantiate(cardsManagerPrefab);
            gameObject.SetActive(false);
            mapBlocker.SetActive(false);
        } else {
            Destroy(gameObject);
        }
    }

    // Stop accepting trades.
    public void DisallowTrades() {
        acceptTrades = false;
    }

    // Allow trades to be accepted.
    public void AllowTrades() {
        acceptTrades = true;
    }

    public void CancelTrade() {
        if (tradeInProgress) {
            TradeMessage completeMessage = TradeMessage.CompleteMessage(cardName, GameState.Instance.MyPlayer.Id, tradeTo.Id);
            network.broadcast(completeMessage.toJson());
        }
    }

    // Start the trade. 1st player sends a card to 2nd player.
    public void StartTrade(Player player, int cardID) {
        // Can't start trade if cardID is invalid.
        if (!GameState.Instance.HasCard(cardID)) {
            inventoryMessage.text = "You don't have this card anymore!";
            return;
        }

        // Can't start trade if one is already in progress.
        if (tradeInProgress) {
            inventoryMessage.text = "Trade with "+ player.Name +" in progress!";
            return;
        }

        tradeTo = player;
        inventoryMessage.text = "";
        this.cardID = cardID; 
        cardName = GameState.Instance.GetCard(cardID);
        TradeMessage message = TradeMessage.SendMessage(cardName, GameState.Instance.MyPlayer.Id, player.Id);
        network.broadcast(message.toJson());
        tradeInProgress = true;
    }

    // Happens at end of trade or when trade is cancelled.
    public void CloseTrade() {
        // Decline if trade was not accepted.
        if (!acceptTrade && tradeInProgress)
            DeclineTrade();
        else
            Reset();
    }

    public void Reset() {
        // Don't reset if player is receiving a trade.
        if (tradeFromID != "" && tradeInProgress)
            return;

        CancelTrade();
        // Close trade UI
        gameObject.SetActive(false);
        mapBlocker.SetActive(false);
        tradeTo = null;
        tradeFromID = null;
        acceptTrade = false;
        tradeInProgress = false;
        tradeFromID = "";
        cardID = -1;
    }

    // For the second player to accept the trade with their card.
    public void AcceptTrade() {
        acceptTrade = true;

        TradeMessage acceptMessage = TradeMessage.AcceptMessage(cardName, GameState.Instance.MyPlayer.Id, tradeFromID);
        network.broadcast(acceptMessage.toJson());

        CloseInterface();
    }

    // For the second player to decline the trade. 
    public void DeclineTrade() {
        acceptTrade = false;

        TradeMessage declineMessage = TradeMessage.DeclineMessage(cardName, GameState.Instance.MyPlayer.Id, tradeFromID);
        network.broadcast(declineMessage.toJson());

        Reset();
    }

    public void CloseInterface() {
        interfaceParent.SetActive(false);
    }

    public void OpenInterface() {
        interfaceParent.SetActive(true);
    }

    public CallbackStatus HandleMessage(Message message) {
        if (message.messageInfo.messageType != MessageType.TRADE) {
            return CallbackStatus.NOT_PROCESSED;
        }

        if (!acceptTrades) {
            return CallbackStatus.DORMANT;
        }

        TradeMessage tradeMessage = (TradeMessage) message.messageInfo;

        // If the message is not for the current player, ignore it. 
        if (tradeMessage.sendTo != GameState.Instance.MyPlayer.Id) {
            Debug.Log("Trade message not for me:" + tradeMessage.toJson());
            Debug.Log("My ID: " + GameState.Instance.MyPlayer.Id);
            return CallbackStatus.PROCESSED;
        }

        // If a trade is in progress ignore other trade requests, post-TRADE_SEND message.
        if (tradeInProgress) {
            // If I am sender but message not from my chosen player.
            if (tradeTo != null && tradeMessage.sentFrom != tradeTo.Id) {
                Debug.Log("Completely shouldn't have gotten this message:" + tradeMessage.toJson());
                return CallbackStatus.PROCESSED;
            } else if (tradeFromID != "" && tradeMessage.sentFrom != tradeFromID) {
                // If I am receiver but message not from the original sender.
                // Send a decline message.
                TradeMessage declineMessage = TradeMessage.DeclineMessage(tradeMessage.cardName, tradeMessage.sentFrom, GameState.Instance.MyPlayer.Id);
                network.broadcast(declineMessage.toJson());
                return CallbackStatus.PROCESSED;
            }
        }

        switch (tradeMessage.type) {
            case TradeMessageType.TRADE_SEND:
                // Set up trade UI
                Player sender = GameState.Instance.GetPlayerByID(tradeMessage.sentFrom);
                playerName.text = sender.Name;
                ((Image) playerIcon.GetComponentInChildren(typeof(Image))).sprite = sender.Icon;

                // Get the card to be displayed in the trade UI
                Card card = cardsManager.GetComponent<CardsUIManager>().findCardDetails(tradeMessage.cardName);
                cardObject.GetComponent<CardRenderer>().RenderCard(card);

                // Open trade UI
                tradeFromID = tradeMessage.sentFrom;
                cardName = tradeMessage.cardName;
                
                tradeMessageObject.text = "";
                OpenInterface();
                gameObject.SetActive(true);
                mapBlocker.SetActive(true);

                tradeInProgress = true;
                break;
            case TradeMessageType.TRADE_ACCEPT:
                if (tradeMessage.cardName != cardName) {
                    Debug.Log("Trade message card name does not match:" + tradeMessage.toJson());
                    return CallbackStatus.PROCESSED;
                }

                Debug.Log("Got trade response message:" + tradeMessage.toJson());
                Debug.Log("Trade to ID:" + tradeTo.Id);

                inventoryMessage.text = tradeAcceptedMessage;
                GameState.Instance.RemoveCard(cardID);

                TradeMessage completeMessage = TradeMessage.CompleteMessage(tradeMessage.cardName, GameState.Instance.MyPlayer.Id, tradeTo.Id);
                network.broadcast(completeMessage.toJson());
                tradeInProgress = false;
                break;
            case TradeMessageType.TRADE_DECLINE:
                inventoryMessage.text = tradeDeclinedMessage;
                tradeInProgress = false;
                break;
            case TradeMessageType.TRADE_COMPLETE:
                // Add card to inventory
                if (acceptTrade && tradeMessage.sendTo == GameState.Instance.MyPlayer.Id) {
                    GameState.Instance.AddCard(tradeMessage.cardName);
                    tradeMessageObject.text = tradeCompleteMessage;
                } else {
                    tradeMessageObject.text = tradeLostMessage;
                }

                tradeInProgress = false;
                CloseInterface();
                break;
            default:
                break;
        }
        return CallbackStatus.PROCESSED;
    }
}

public enum TradeMessageType {
    TRADE_SEND,     // Sent when initiating a trade from 1st player
    TRADE_ACCEPT,   // Sent when accepting a trade from 2nd player
    TRADE_DECLINE,  // Sent when declining a trade from 2nd player
    TRADE_COMPLETE, // Sent when trade is complete from 1st player
}

public class TradeMessage : MessageInfo {
    public MessageType messageType { get; set; }
    public TradeMessageType type { get; set; }

    public CardName cardName { get; set; }

    public string sendTo { get; set; }
    public string sentFrom { get; set; }
    
    [JsonConstructor]
    public TradeMessage(TradeMessageType type, CardName cardName, string sendTo, string sentFrom) {
        messageType = MessageType.TRADE;
        this.type = type;
        this.cardName = cardName;
        this.sendTo = sendTo;
        this.sentFrom = sentFrom;
    }

    public static TradeMessage SendMessage(CardName cardName, string sentFrom, string sendTo) {
        return new TradeMessage(TradeMessageType.TRADE_SEND, cardName, sendTo, sentFrom);
    }

    public static TradeMessage AcceptMessage(CardName cardName, string sentFrom, string sendTo) {
        return new TradeMessage(TradeMessageType.TRADE_ACCEPT, cardName, sendTo, sentFrom);
    }

    public static TradeMessage DeclineMessage(CardName cardName, string sentFrom, string sendTo) {
        return new TradeMessage(TradeMessageType.TRADE_DECLINE, cardName, sendTo, sentFrom);
    }

    public static TradeMessage CompleteMessage(CardName cardName, string sentFrom, string sendTo) {
        return new TradeMessage(TradeMessageType.TRADE_COMPLETE, cardName, sendTo, sentFrom);
    }

    public string toJson() {
        return JsonConvert.SerializeObject(this);
    }
}
