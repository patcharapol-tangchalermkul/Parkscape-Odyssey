using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootController : MonoBehaviour
{
    private GameObject gameManager;

    private LootUIManager lootUIManager;

    private Destroyer destroyer;

    [SerializeField]
    private GameObject cardsManagerPrefab;

    private GameObject cardsManager;

    private CardsUIManager cardsUIManager;

    [SerializeField]
    private int cardsNumber = 2;
    
    private Card focusedCard;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager");

        if (GameObject.FindGameObjectsWithTag("CardsManager").Length <= 0) {
            cardsManager = Instantiate(cardsManagerPrefab);
            cardsManager.tag = "CardsManager";
        } else {
            cardsManager = GameObject.FindGameObjectWithTag("CardsManager");
        }
        cardsUIManager = cardsManager.GetComponent<CardsUIManager>();
        
        lootUIManager = GetComponent<LootUIManager>();

        destroyer = GetComponent<Destroyer>();

        // Generate cards loot and display on screen
        Card[] cardsLoot = generateCardsLoot(GameState.Instance.encounterMonsters[0].level);
        lootUIManager.displayCardsLoot(cardsLoot);
    }


    //TODO: Implement proper loot drop system, now it just randoms the card drops
    private Card[] generateCardsLoot(EnemyLevel enemyLevel) {
        Dictionary<CardRarity, List<Card>> allCardsByRarity = cardsUIManager.getAllAvailableCardsByRarity();

        // Generate the cummulative probability of card drops for each rarity
        float[] pDrops = generateCardsRarityProbability(enemyLevel);

        CardRarity[] allRarities = (CardRarity[]) Enum.GetValues(typeof(CardRarity));
        allRarities.Reverse();

        List<Card> cardsLoot = new();

        System.Random random = new();

        for (int j = 0; j < cardsNumber; j++) {
            float cardDropRarity = (float) random.NextDouble();

            for (int i = pDrops.Length - 1; i >= 0; i--) {
                if (cardDropRarity < pDrops[i]) {
                    Debug.Log($"LootController: Card chosen: {allRarities[i]}");
                    List<Card> cardsInRarity = allCardsByRarity[allRarities[i]];
                    Card cardLoot = cardsInRarity[random.Next(cardsInRarity.Count)];
                    cardsLoot.Add(cardLoot); 
                    break;
                }
            }
        }

        return cardsLoot.ToArray();
    }

    private float[] generateCardsRarityProbability(EnemyLevel enemyLevel) {
        switch(enemyLevel) {
            case EnemyLevel.EASY:
                return new [] {1f, 0.3f, 0f};
            case EnemyLevel.MEDIUM:
                return new [] {1f, 0.4f, 0.05f};
            case EnemyLevel.HARD:
                return new [] {1f, 0.7f, 0.12f};
            case EnemyLevel.BOSS:
                return new [] {1f, 1f, 0.30f};
            default:
                return null;
        }
    }


    // Loot selection confirmation
    public void confirmCardSelection() {
        if (focusedCard != null) {
            addCardToPlayerDeck(focusedCard);
            lootUIManager.closeCardConfirmationPopUp();

            // Enable map after this selection
            MapManager.Instance.EnableMapInteraction();

            destroyer.DestroySelf();
            return;
        }
        Debug.LogWarning("LootController: no focused card set.");
    }

    public void denyCardSelection() {
        lootUIManager.closeCardConfirmationPopUp();
    }

    private void addCardToPlayerDeck(Card card) {
        GameState.Instance.AddCard(card.name);
    }

    public void setFocusedCard(Card card) {
        focusedCard = card;
    }
}
