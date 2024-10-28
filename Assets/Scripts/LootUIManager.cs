using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LootUIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject cardDisplayPrefab;

    [SerializeField]
    private GameObject cardsSelectionPanel;

    [SerializeField]
    private GameObject popUpPanel;

    private List<GameObject> cardsDisplaying = new();

    private LootController lootController;

    // Start is called before the first frame update
    void Start()
    {
        lootController = GetComponent<LootController>();
        closeCardConfirmationPopUp();
    }

   
   // Display cards loot
    public void displayCardsLoot(Card[] cards) {
        foreach(Card card in cards) {
            displayCard(card);
        }

        addListenerForCards();
    }

    private void displayCard(Card card) {
        GameObject newCard = Instantiate(cardDisplayPrefab, cardsSelectionPanel.transform);
        CardRenderer cardRenderer = newCard.GetComponent<CardRenderer>();
        cardRenderer.RenderCard(card);
        cardRenderer.ScaleCardSize(5);
        cardsDisplaying.Add(newCard);
    }

    private void addListenerForCards() {
        foreach (GameObject card in cardsDisplaying) {
            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() => { openCardConfirmationPopUp(card); });
        }
    }

    private void openCardConfirmationPopUp(GameObject card) { 
        Card cardDetails = card.GetComponent<CardRenderer>().GetCardDetails();

        GameObject focusedCard = Instantiate(cardDisplayPrefab);
        CardRenderer cardRenderer = focusedCard.GetComponentInChildren<CardRenderer>();
        cardRenderer.RenderCard(cardDetails);
        
        GameObject popUpCardDisplayPanel = popUpPanel.transform.GetChild(1).gameObject;
        focusedCard.transform.parent = popUpCardDisplayPanel.transform;
        focusedCard.tag = "CardsLootFocusedCard";
        cardRenderer.ScaleCardSize(7.5f);
        focusedCard.GetComponent<RectTransform>().localPosition = new Vector3(0, 120, 0);

        popUpPanel.SetActive(true);

        lootController.setFocusedCard(cardDetails);
    }

    public void closeCardConfirmationPopUp() {
        GameObject[] focusedcard = GameObject.FindGameObjectsWithTag("CardsLootFocusedCard");
        if (focusedcard.Length != 0) {
            Destroy(focusedcard[0]);
        }

        popUpPanel.SetActive(false);

        lootController.setFocusedCard(null);
    }
}
