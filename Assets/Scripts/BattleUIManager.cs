using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BattleUIManager : MonoBehaviour {
    private BattleManager battleManager;

    [SerializeField]
    private GameObject cardsManagerPrefab;

    [SerializeField]
    private GameObject cardDisplayPrefab;

    [SerializeField]
    private GameObject monsterDisplayPrefab;

    [SerializeField]
    private GameObject cardDescriptionPanel;

    [SerializeField]
    private GameObject playerStatPrefab;

    [SerializeField]
    private GameObject playerSpeedPrefab;

    [SerializeField]
    private List<GameObject> displayCards = new List<GameObject>();

    [SerializeField]
    private GameObject enemyPanel;
    [SerializeField]
    private GameObject otherPlayersPanel;
    
    [SerializeField]
    private GameObject handPanel;

    [SerializeField]
    private GameObject speedPanel;

    [SerializeField]
    private GameObject deadPanel;

    private GameObject cardsManager;
    private CardsUIManager cardsUIManager;
    private Dictionary<string, GameObject> playerStats = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> playerSpeeds = new Dictionary<string, GameObject>();

    private PopUpManager popUpManager;

    void Awake () {
        // Find the BattleManager
        battleManager = (BattleManager) GetComponent(typeof(BattleManager));

        // Find an existing CardManager or instantiate one if not found
        if (GameObject.FindGameObjectsWithTag("CardsManager").Length <= 0) {
            cardsManager = Instantiate(cardsManagerPrefab);
            cardsManager.tag = "CardsManager";
        } else {
            cardsManager = GameObject.FindGameObjectWithTag("CardsManager");
        }

        // Extract the CardsUIManager from the CardManager
        cardsUIManager = cardsManager.GetComponent<CardsUIManager>();

        // Find the PopUpManager for the card description and initialise it to closed
        popUpManager = (PopUpManager) GameObject.FindGameObjectWithTag("CardDescription").GetComponent(typeof(PopUpManager));
        popUpManager.closePopUp();
    }

    public List<GameObject> DisplayOtherPlayers(List<Player> otherPlayers) {
        // Instantiate the playerStatPrefab for each player in otherPlayers
        for (int i = 0; i < GameState.Instance.maxPlayerCount - 1; i++) {
            // Initialise an empty playerStat prefab whether there will be a player there or not
            GameObject playerStat = Instantiate(playerStatPrefab, otherPlayersPanel.transform, false);
            if (i < otherPlayers.Count) {
                // For valid other players, set the icon and add to the list
                Player player = otherPlayers[i];
                playerStat.transform.GetChild(0).GetComponent<Image>().sprite = player.Icon;
                playerStats.Add(player.Id, playerStat);
            } else {
                // Disable the icon and health info for nonexistent players
                // These playerStat prefabs do not belong to any player, so
                // do not append them to the list that we return.

                // make the player's sprite invisible but occupy the space
                playerStat.transform.GetChild(0).GetComponent<Image>().color = new Color(0, 0, 0, 0);

                // make the player's health info invisible
                playerStat.transform.GetChild(1).GetComponent<Image>().color = new Color(0, 0, 0, 0);
                playerStat.transform.GetChild(1).transform.GetChild(0).GetComponent<TextMeshProUGUI>().enabled = false;
            }
        }
        return new List<GameObject>(playerStats.Values);
    }

    public void DisplaySpeedPanel(List<Player> players) {
        // Instantiate the playerStatPrefab for each player in players
        for (int i = 0; i < players.Count; i++) {
            GameObject playerSpeed = Instantiate(playerSpeedPrefab, speedPanel.transform, false);
            playerSpeed.transform.GetComponent<Image>().sprite = players[i].Icon;
            playerSpeeds.Add(players[i].Id, playerSpeed);
        }
    }

    public void arrangeOtherPlayersInOrder(List<string> sortedPlayerIds) {
        // Rearrange the playerStats in the order of sortedPlayerIds
        for (int i = 0; i < sortedPlayerIds.Count; i++) {
            GameObject playerStat = playerStats[sortedPlayerIds[i]];
            playerStat.transform.SetSiblingIndex(i);
        }
    }

    public void arrangeSpeedPanelInOrder(List<string> sortedPlayerIds) {
        // Rearrange the speedPanel in the order of sortedPlayerIds
        for (int i = 0; i < sortedPlayerIds.Count; i++) {
            GameObject playerSpeed = playerSpeeds[sortedPlayerIds[i]];
            playerSpeed.transform.SetSiblingIndex(i);
        }
    }

    public void RepositionCards() {
        for (int i = 0; i < displayCards.Count; i++) {
            // GameObject cardInstance = displayCards[i];
            // (Vector3 cardPosition, Quaternion cardRotation) = getCardPositionAtIndex(i);
            // cardInstance.transform.localPosition = cardPosition;
            // cardInstance.transform.rotation = cardRotation;
            StartCoroutine(displayCards[i].GetComponentInChildren<BattleCardRenderer>().ResetCardPosition(0.1f));
        }
    }

    public void DisplayHand(List<CardName> cards) {
        // Instantiate the hand and add the card objects to displayCards
        for (int i = 0; i < cards.Count; i++) {
            CardName card = cards[i];
            GameObject newCard = displayCardAndApplyIndex(card, i);
            if (newCard != null) {
                displayCards.Add(newCard);
            }
        }

        // The positions of the cards must be instantiated *after* displayCards is fully populated
        // This is because the card's index is used to calculate its position
        RepositionCards();
    }

    public void AddToHand(CardName card) {
        // Add the card to the displayCards list and instantiate it
        int i = displayCards.Count;
        GameObject newCard = displayCardAndApplyIndex(card, i);
        if (newCard != null) {
            displayCards.Add(newCard);
        }
        RepositionCards();
    }

    // This is called only when displaying the hand for the first time
    private GameObject displayCardAndApplyIndex(CardName cardName, int i) {
        Card card = cardsUIManager.findCardDetails(cardName);

        // (Vector3 cardPosition, Quaternion cardRotation) = getCardPositionAtIndex(i);
        
        // Create a new instance of the card prefab in the correct world position, with parent handPanel
        GameObject cardInstance = Instantiate(cardDisplayPrefab, handPanel.transform, true);

        // Set the rendered card's index
        BattleCardRenderer cardRenderer = cardInstance.GetComponentInChildren<BattleCardRenderer>();
        cardRenderer.cardIndex = i;


        // Scale and render the card
        cardRenderer.RenderCard(card);
        cardRenderer.ScaleCardSize(6);
        
        return cardInstance;
    }

    public void RemoveCardFromHand(int index) {
        // Remove the card from the displayCards list and destroy it
        GameObject card = displayCards[index];
        displayCards.RemoveAt(index);

        // Update the card index of the remaining cards
        for (int i = 0; i < displayCards.Count; i++) {
            GameObject cardInstance = displayCards[i];
            CardRenderer cardRenderer = cardInstance.GetComponentInChildren<CardRenderer>();
            cardRenderer.cardIndex = i;
        }

        // Call the repositioning coroutine in each card's renderer
        for (int i = 0; i < displayCards.Count; i++) {
            GameObject cardInstance = displayCards[i];
            BattleCardRenderer cardRenderer = cardInstance.GetComponentInChildren<BattleCardRenderer>();
            StartCoroutine(cardRenderer.ResetCardPosition(0.2f));
        }

        Destroy(card);
    }

    public void DisplayMonster(Monster monster) {
        GameObject monsterInstance = Instantiate(monsterDisplayPrefab, enemyPanel.transform, false);
        MonsterRenderer monsterRenderer = monsterInstance.GetComponentInChildren<MonsterRenderer>();
        monsterRenderer.renderMonster(monster);
    }

    public (Vector3, Quaternion) getCardPositionAtIndex(int i) {
        int handSize = displayCards.Count;
        float zRot = 1.5f;

        // For 5+ cards, make the x-offset such that they all fit on the screen
        // For fewer, fix the x-offset so they look nice in the middle
        float xOffset = handSize >= 5
            ? ((Screen.width / (1.5f * handSize)) - 80)
            : 100.0f;
        float yOffset = 5.0f;

        // Calculate "how far in" the card is, and use that to interpolate
        float align = handSize > 1 ? (i / (handSize - 1.0f)) : 0.5f;
        float rotZ = Mathf.Lerp(handSize * zRot, handSize * -zRot, align);
        float xPos = Mathf.Lerp(handSize * -xOffset, handSize * xOffset, align);
        float yPos = -Mathf.Abs(Mathf.Lerp(handSize * -yOffset, handSize * yOffset, align));
        return (
            new Vector3((Screen.width / 2) + xPos, yPos + 325, 0),
            Quaternion.Euler(0, 0, rotZ)
        );
    }
    
    public void DisplayCardDescription(Card card) {
        Debug.Log(popUpManager);
        // Instantiate an inventory card prefab for the description
        // GameObject cardDescription = Instantiate(cardDescriptionPrefab, GameObject.FindGameObjectWithTag("BattleCanvas").transform, false);
        Transform cardDescription = cardDescriptionPanel.transform.GetChild(0);
        
        cardDescription.GetChild(1).gameObject.GetComponent<Image>().sprite = card.img;
        cardDescription.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = card.stats;
        popUpManager.openPopUp();
        // CardRenderer renderer = enlargedCard.GetComponentInChildren<CardRenderer>();
        // renderer.RenderCard(card);
    }

    public void CloseCardDescriptionPopUp() {
        popUpManager.closePopUp();
    }

    public void UpdateMonsterStats(List<Monster> mosnters) {
        Monster monsterToUpdate = mosnters[0];
        GameObject enemyToUpdate = enemyPanel.transform.GetChild(0).gameObject;
        MonsterRenderer monsterRenderer = enemyToUpdate.GetComponentInChildren<MonsterRenderer>();
        monsterRenderer.UpdateMonsterHealth(monsterToUpdate);
    }

    public void PlayerDeadUI() {
        deadPanel.SetActive(true);
    }

    public void ResetUI() {
        deadPanel.SetActive(false);
    }
}
