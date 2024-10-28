using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class CardsUIManager : MonoBehaviour
{

    private List<Card> cards = new List<Card>();

    private Dictionary<CardRarity, List<Card>> cardsByRarity = new();

    [SerializeField]
    private GameObject cardPrefab;

    [SerializeField]
    private List<CardName> cardNames = new List<CardName>();

    [SerializeField]
    private List<Sprite> cardImgs = new List<Sprite>();
    
    [SerializeField]
    private List<string> cardStats = new List<string>();

    [SerializeField]
    private List<CardRarity> cardRarities = new List<CardRarity>();

    [SerializeField]
    private int cardsToCreate = 0;

    void Awake() {
        if (cardNames.Count == cardImgs.Count 
                && cardNames.Count == cardStats.Count 
                && cardNames.Count == cardRarities.Count) {
            int i;
            for (i = 0; i < Math.Min(cardNames.Count, cardImgs.Count); i++) {
                // cards.Add(cardNames[i], (cardImgs[i], cardStats[i]));
                Card newCard = new Card(cardNames[i], cardImgs[i], cardStats[i], 1, "none", cardRarities[i]);
                cards.Add(newCard);
                asignCardByRarity(newCard);
            }
            return;
        }
            
        Debug.LogWarning("Cards Manager: Card Names, Card Images and Card Stats provided do not have the same amount. Please check these fields.");

    }

    private void asignCardByRarity(Card card) {
        CardRarity rarity = card.rarity;
        if (!cardsByRarity.ContainsKey(rarity)) {
            cardsByRarity.Add(rarity, new List<Card>());
        }
        cardsByRarity[rarity].Add(card);
    }

    // // Update is called once per frame
    // void Update()
    // {
    //     if (cardsToCreate > 0) {
    //         createCard("baseAtk");
    //         cardsToCreate -= 1;
    //     }
    // }

    public Card findCardDetails(CardName cardName) {
        try {
            return cards.Find(c => c.name == cardName);
        } catch (Exception) {
            return null;
        }
    }


    // private void createCard(string cardName) {
    //     if (cards.ContainsKey(cardName)) {
    //         // Render card image
    //         GameObject cardImgObj = cardPrefab.transform.GetChild(0).gameObject;
    //         Sprite cardImg = cards[cardName].img;
    //         SpriteRenderer cardImgRenderer = cardImgObj.GetComponent(typeof(SpriteRenderer)) as SpriteRenderer;
    //         cardImgRenderer.sprite = cardImg;

    //         // Render card stats
    //         GameObject cardStatsObj = cardPrefab.transform.GetChild(1).gameObject;
    //         string cardStat = cards[cardName].stats;
    //         TextMeshPro textComp = cardStatsObj.GetComponent(typeof(TextMeshPro)) as TextMeshPro;
    //         textComp.text = cardStat;

    //         Instantiate(cardPrefab);
    //     } else {
    //         Debug.Log($"Cards Manager: Card image not found - {cardName}. Cannot create card.");
    //     }
    // }

    public Card[] getAllAvailableCards() {
        return cards.ToArray();
    }

    public Dictionary<CardRarity, List<Card>> getAllAvailableCardsByRarity() {
        return cardsByRarity;
    }

    public Card GetRandomCard() {
        // Ignore base cards
        while (true) {
            int randomIndex = UnityEngine.Random.Range(0, cards.Count);
            if (cards[randomIndex].name != CardName.BASE_ATK && cards[randomIndex].name != CardName.BASE_DEF)
                return cards[randomIndex];
        }
    }
}