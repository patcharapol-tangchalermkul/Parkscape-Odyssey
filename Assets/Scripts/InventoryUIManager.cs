using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using TMPro;

[assembly:InternalsVisibleTo("EditMode")]


public class InventoryUIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject cardsManagerPrefab;

    [SerializeField]
    private GameObject cardDisplayPrefab;

    [SerializeField]
    private GameObject cardsInventoryContent;

    [SerializeField]
    private GameObject popUpPanel;

    [SerializeField]
    private GameObject nearbyPlayersGrid;

    [SerializeField]
    private GameObject nearbyPlayerPrefab;
    private int updateCount = 0;
    private const int updateCountMax = 50;

    [SerializeField]
    private TMP_Text tradeMessage;

    private GameObject cardsManager;

    CardsUIManager cardsUIManager;
    InventoryController inventoryController;

    public List<GameObject> cardsDisplaying = new();

    // Start is called before the first frame update
    void Start()
    {
        closeCardTradePopUp();
        closeInventory();

        if (GameObject.FindGameObjectsWithTag("CardsManager").Length <= 0) {
            cardsManager = Instantiate(cardsManagerPrefab);
            cardsManager.tag = "CardsManager";
        } else {
            cardsManager = GameObject.FindGameObjectWithTag("CardsManager");
        }
        cardsUIManager = cardsManager.GetComponent<CardsUIManager>();
        inventoryController = GetComponent<InventoryController>();
        SetUpNearbyPlayers();
    }

    void Update() {
        UpdateNearbyPlayers();
    }

    public void OpenInventory() {
        List<CardName> cards = GameState.Instance.GetCards();
        inventoryController.inventoryCards = cards;

        // Clear text from trade message.
        tradeMessage.text = "";

        // Clear all cards from the inventory.
        foreach (GameObject card in cardsDisplaying) {
            Destroy(card);
        }
        cardsDisplaying.Clear();

        displayAllCards();
        addListenerForCards();

        closeCardTradePopUp();
        gameObject.SetActive(true);

        // Disable map
        MapManager.Instance.DisableMapInteraction();
    }

    private void displayAllCards() {
        int i = 0;
        foreach (int cardID in GameState.Instance.GetCardIDs()) {
            GameObject newCard = displayCardAndApplyIndex(GameState.Instance.GetCard(cardID), i);
            newCard.name = cardID.ToString();
            if (newCard != null) {
                cardsDisplaying.Add(newCard);
                i ++;
            }
        }
    }

    private GameObject displayCardAndApplyIndex(CardName cardName, int i) {
        Card cardDetails = cardsUIManager.findCardDetails(cardName);
        if (cardDetails == null) {
            Debug.LogWarning($"InventoryUIManager: Card not found in CardsManager - {cardName}");
            return null;
        }

        GameObject newCard = Instantiate(cardDisplayPrefab);
        CardRenderer cardRenderer = newCard.GetComponentInChildren<CardRenderer>();
        cardRenderer.cardIndex = i;
        cardRenderer.RenderCard(cardDetails);
        newCard.transform.parent = cardsInventoryContent.transform;
        cardRenderer.HardAdjustCardDetailsSize();
        cardRenderer.ScaleCardSize(1);
        return newCard;
    }

    private void addListenerForCards() {
        foreach (GameObject card in cardsDisplaying) {
            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() => { openCardTradePopUp(card); });
        }
    }

    private void openCardTradePopUp(GameObject card) {
        Card cardDetails = card.GetComponent<CardRenderer>().GetCardDetails();

        GameObject popUpCardDisplayPanel = popUpPanel.transform.GetChild(1).gameObject;
        GameObject focusedCard = Instantiate(cardDisplayPrefab, popUpCardDisplayPanel.transform);
        focusedCard.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
        CardRenderer cardRenderer = focusedCard.GetComponentInChildren<CardRenderer>();
        cardRenderer.RenderCard(cardDetails);
        
        focusedCard.name = card.name;
        focusedCard.tag = "CardsInventoryFocusedCard";
        cardRenderer.ScaleCardSize(10f);

        popUpPanel.SetActive(true);
    }

    public void closeCardTradePopUp() {
        GameObject focusedCard = getFocusedCard();
        if (focusedCard != null) {
            Destroy(focusedCard);
        }

        popUpPanel.SetActive(false);

        // Clear text from trade message.
        tradeMessage.text = "";
    }

    private GameObject getFocusedCard() {
        GameObject[] focusedcard = GameObject.FindGameObjectsWithTag("CardsInventoryFocusedCard");
        if (focusedcard.Length != 0) {
            return focusedcard[0];
        }
        return null;
    }

    private void SetUpNearbyPlayers() {
        foreach (Player p in GameState.Instance.OtherPlayers) {
            GameObject playerButton = Instantiate(nearbyPlayerPrefab, nearbyPlayersGrid.transform);
            playerButton.name = p.Id;
            playerButton.GetComponentInChildren<TMP_Text>().text = p.Name;
            ((Image) playerButton.GetComponentInChildren(typeof(Image))).sprite = p.Icon;
            playerButton.GetComponent<Button>().onClick.AddListener(() => {
                GameObject focusedCard = getFocusedCard();
                if (focusedCard == null)
                    return;

                string cardObjectName = focusedCard.name;
                TradeManager.selfReference.StartTrade(p, int.Parse(cardObjectName));
            });
        }
    }

    // Update the nearby players list
    private void UpdateNearbyPlayers() {
        // Perform this if panel is open.
        if (!gameObject.activeSelf) {
            return;
        }

        // Perform this only every updateCountMax 
        updateCount++;
        if (updateCount < updateCountMax) {
            return;
        }
        updateCount = 0;
        
        // Grey out the players that are not nearby
        foreach (Transform child in nearbyPlayersGrid.transform) {
            if (NetworkManager.Instance.connectedPlayers.ContainsKey(child.name)) {
                child.GetComponent<Button>().interactable = true;
            } else {
                child.GetComponent<Button>().interactable = false;
            }
        }
    }

    public void closeInventory() {
        MapManager.Instance.EnableMapInteraction();
        gameObject.SetActive(false);
    }
}
