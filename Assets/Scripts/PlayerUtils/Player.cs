using System;
using UnityEngine;

public class Player
{
    // Properties
    public string Name { get; }
    public string Id { get; }
    public string Role { get; }
    public int Speed { get; private set;}
    public int MaxHealth { get; private set;}
    public int CurrentHealth { get; private set; }
    public int Mana { get; private set; }
    public int MaxMana { get; private set;}
    public int Strength { get; private set;}
    public int Defence  { get; private set; }
    public string Description { get; }
    public Sprite Icon { get; set; }
    public bool IsLeader { get; set; }

    // Multipliers
    public float AttackMultiplier { get; set; }
    public float DefenceMultiplier { get; set; }

    // Constants
    private const int BASESTRENGTH = 0;
    private const int BASEMULTIPLIER = 1;

    // Constructor
    public Player(string name, string id, string role, int speed, int maxHealth, int maxMana, string description = "") {
        Name = name;
        Id = id;
        Role = role;
        Speed = speed;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        MaxMana = maxMana;
        Mana = maxMana;
        Strength = BASESTRENGTH;
        Defence = 0;
        AttackMultiplier = BASEMULTIPLIER;
        DefenceMultiplier = BASEMULTIPLIER;
        Description = description;
    }

    public void PlayCard(Card card) {
        Mana -= card.cost;
    }

    public void ResetMana() {
        Mana = MaxMana;
    }


    public void TakeDamage(int dmg) {
        int healthToDecrease = Math.Max(dmg - Defence, 0);
        Defence = Math.Max(0, Defence - dmg);
        CurrentHealth -= healthToDecrease;
        if (CurrentHealth < 0) {
            CurrentHealth = 0;
        }
    }

    public void Heal(int amount) {
        if (CurrentHealth + amount > MaxHealth) {
            CurrentHealth = MaxHealth;
        } else {
            CurrentHealth += amount;
        }
    }

    public void Revive() {
        CurrentHealth = MaxHealth / 2;
    }

    public bool IsDead() {
        return CurrentHealth <= 0;
    }

    public void AddDefence(int amount) {
        Defence += amount;
    }

    public void AddStrength(int amount) {
        Strength += amount;
    }

    public void AddSpeed(int amount) {
        Speed += amount;
    }

    public void AddHealthPotion() {
        MaxHealth += 2;
        CurrentHealth += 2;
    }

    public void AddManaPotion() {
        MaxMana += 1;
        Mana += 1;
    }
}
