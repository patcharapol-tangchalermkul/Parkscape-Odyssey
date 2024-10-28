using System.Collections.Generic;
using UnityEngine;

public class Monster {

  public readonly MonsterName name;

  public readonly Sprite img;

  public int Health { get; private set; }

  public int Defense { get; private set; }

  public readonly int defenseAmount;

  public int BaseDamage { get; private set; }

  public readonly List<Skill> skills;

  public readonly EnemyLevel level;


  public Monster(MonsterName name, Sprite img, int health, int defense, int defenseAmount, int baseDamage, List<Skill> skills, EnemyLevel level) {
    this.name = name;
    this.img = img;
    this.Health = health;
    this.Defense = defense;
    this.defenseAmount = defenseAmount;
    this.BaseDamage = baseDamage;
    this.skills = skills;
    this.level = level;
  }

  public void IncreaseDef() {
    Defense += defenseAmount;
  }

  public void TakeDamage(int dmg) {
    Health -= dmg;
  }

  public void Strengthen(int amount) {
    BaseDamage += amount;
  }

  public void DecreaseStrength(int amount) {
    BaseDamage -= amount;
  }
}