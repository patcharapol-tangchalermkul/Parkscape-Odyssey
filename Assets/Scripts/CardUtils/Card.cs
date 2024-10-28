using System;
using System.Collections.Generic;
using UnityEngine;

public class Card {
  public readonly CardName name;
  public readonly Sprite img;
  public readonly string stats;
  public readonly int cost;
  public readonly string type;

  public readonly CardRarity rarity;

  public System.Action<List<Player>, List<Monster>, string> UseCard;

  public Card(CardName name, Sprite img, string stats, int cost, string type, CardRarity rarity) {
    this.name = name;
    this.img = img;
    this.stats = stats;
    this.cost = cost;
    this.type = type;
    this.rarity = rarity;
    applyUseCardFunc();
  }

  private void applyUseCardFunc() {
    switch (name) {
      case CardName.BASE_ATK:
        UseCard = (players, monsters, id) => {
          Player player = players.Find(p => p.Id == id);
          monsters[0].TakeDamage(5 + player.Strength);
        };
        break;
      case CardName.BASE_DEF:
        UseCard = (players, monsters, id) => {
            Player player = players.Find(p => p.Id == id);
            player.AddDefence(5);
        };
        break;
      case CardName.WAR_CRY:
        UseCard = (players, monsters, id) => {
          foreach (Player player in players) {
            if (player.Id == GameState.Instance.myID) {
              continue;
            }
            player.AddStrength(1);
          }
        };
        break;
      case CardName.ENRAGE:
        UseCard = (players, monsters, id) => {
          Player player = players.Find(p => p.Id == id);
          player.AddStrength(5);
          player.TakeDamage(10);
        };
        break;
      case CardName.SPRINT:
        UseCard = (players, monsters, id) => {
          Player player = players.Find(p => p.Id == id);
          player.AddSpeed(2);
        };
        break;
      case CardName.SODA_AID:
        UseCard = (players, monsters, id) => {
          Player player = players.Find(p => p.Id == id);
          player.AddSpeed(2);
        };
        break;
      case CardName.BIG_HEAL:
        UseCard = (players, monsters, id) => {
          foreach (Player player in players) {
            player.Heal(2);
          }
        };
        break;
      case CardName.BLOOD_OFFERING:
        UseCard = (players, monsters, id) => {
          Player player = players.Find(p => p.Id == id);
          player.TakeDamage(5);
          monsters[0].TakeDamage(10);
        };
        break;
      case CardName.FEMALE_WARRIOR:
        UseCard = (players, monsters, id) => {
          monsters[0].DecreaseStrength(5);
        };
        break;
      case CardName.DRAW_CARDS:
        UseCard = (players, monsters, id) => {
          Debug.Log("Draw cards");
        };
        break;
      default:
        Debug.LogError("Card not found");
        break;
    }
  }
}