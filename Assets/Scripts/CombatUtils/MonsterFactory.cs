using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MonsterFactory {

  private MonsterFactory() {}

  private static readonly MonsterFactory singleton = new();

  public static MonsterFactory Instance() {
      return singleton;
  }

  public readonly static SkillsController skillsController = new();

  private static int IntRandomizer(int min, int max) {
    System.Random random = new();
    double multiplier = random.NextDouble();
    return min + Convert.ToInt32((max - min) * multiplier);
  }

  public static Monster CreateMonster(MonsterName name, Sprite img) {
      return name switch
      {
          MonsterName.GOBLIN => CreateGoblin(img),
          MonsterName.DRAGON => CreateDragon(img),
          _ => null,
      };
  }

  public static Monster CreateMonsterWithValues(MonsterName name, Sprite img, int health, int defense, int defenseAmount, int baseDamage, List<Skill> skills, EnemyLevel level) {
      return new Monster(name, img, health, defense, defenseAmount, baseDamage, skills, level);
  }


  private static Monster CreateGoblin(Sprite img) {
    List<Skill> skills = new()
    {
        skillsController.Get(SkillName.NORMAL_ATTACK),
        skillsController.Get(SkillName.BLOCK),
        skillsController.Get(SkillName.ENRAGE),
        // skillsController.Get(SkillName.TAUNT),
    };

    int health = IntRandomizer(30, 55);

    return new Monster(name: MonsterName.GOBLIN,
                       img: img,
                       health: health,
                       defense: 0,
                       defenseAmount: 15,
                       baseDamage: IntRandomizer(5, 10), 
                       skills: skills,
                       level: health >= 50 ? EnemyLevel.MEDIUM : EnemyLevel.EASY);
  }

  private static Monster CreateDragon(Sprite img) {
    List<Skill> skills = new()
    {
        skillsController.Get(SkillName.NORMAL_ATTACK),
        skillsController.Get(SkillName.AOE_NORMAL_ATTACK),
        skillsController.Get(SkillName.BLOCK),
        // skillsController.Get(SkillName.ENRAGE),
        // skillsController.Get(SkillName.FLY),
        skillsController.Get(SkillName.CATASTROPHE),
    };

    int health = IntRandomizer(250, 400);

    return new Monster(name: MonsterName.DRAGON,
                       img: img,
                       health: IntRandomizer(250, 400), 
                       defense: 0,
                       defenseAmount: 80,
                       baseDamage: IntRandomizer(15, 30), 
                       skills: skills,
                       level: health >= 300 ? EnemyLevel.HARD : EnemyLevel.BOSS);
  }
}