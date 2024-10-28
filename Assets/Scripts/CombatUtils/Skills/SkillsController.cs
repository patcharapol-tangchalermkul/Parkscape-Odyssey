using System;
using System.Collections.Generic;
using System.Diagnostics;

public class SkillsController {
  public Dictionary<SkillName, Skill> skillsDB = new();

  public SkillsController() {
      Initialise();
  }

  private static Player SelectRandomPlayer(List<Player> players) {
    // select a random player from the list of players who are not dead
    Random random = new Random();
    players.RemoveAll((player) => {
      return player.IsDead();
    });
    if (players.Count == 0) {
      throw new ArgumentException("Players list provided shoulen't be empty / contain onyl dead players!");
    }
    int index = random.Next(players.Count);
    return players[index];
  }

  private class SKNormalAttack : Skill {
    public override SkillName Name => SkillName.NORMAL_ATTACK;
    public override SkillType SkillType => SkillType.SINGLE;

    public override void Perform(Monster monster, List<Player> players) {
      Player target = SelectRandomPlayer(players);
      target.TakeDamage(monster.BaseDamage);
    }
  }

  private class SKAoeNormalAttack : Skill {
    public override SkillName Name => SkillName.AOE_NORMAL_ATTACK;
    public override SkillType SkillType => SkillType.AOE;

    public override void Perform(Monster monster, List<Player> players) {
      foreach (Player player in players) {
        if (!player.IsDead()) {
          player.TakeDamage(Convert.ToInt32(monster.BaseDamage * 0.8));
        }
      }
    }
  }

  private class SKBlock : Skill {
    public override SkillName Name => SkillName.BLOCK;
    public override SkillType SkillType => SkillType.BUFF;

    public override void Perform(Monster monster, List<Player> players) {
      monster.IncreaseDef();
    }
  }

  private class SKEnrage : Skill {
    public override SkillName Name => SkillName.ENRAGE;
    public override SkillType SkillType => SkillType.BUFF;

    public override void Perform(Monster monster, List<Player> players) {
      monster.TakeDamage(6);
      monster.Strengthen(10);
    }
  }

  // Catastrophe
  private class SKCatastrophe : Skill {
    public override SkillName Name => SkillName.CATASTROPHE;
    public override SkillType SkillType => SkillType.AOE;

    public override void Perform(Monster monster, List<Player> players) {
      foreach (Player player in players) {
        if (!player.IsDead()) {
          player.TakeDamage(25);
        }
      }
      // can add burn effect once debuffs are implemented
    }
  }

  private void Initialise() {
    skillsDB.Add(SkillName.NORMAL_ATTACK, new SKNormalAttack());
    skillsDB.Add(SkillName.AOE_NORMAL_ATTACK, new SKAoeNormalAttack());
    skillsDB.Add(SkillName.BLOCK, new SKBlock());
    skillsDB.Add(SkillName.ENRAGE, new SKEnrage());
    skillsDB.Add(SkillName.CATASTROPHE, new SKCatastrophe());
  }

  public Skill Get(SkillName name) {
    if (skillsDB.ContainsKey(name)) {
      return skillsDB[name];
    }
    
    return null;
  }

  public List<Player> SelectTargets(Skill skill, List<Player> players) {
    switch (skill.SkillType) {
      case SkillType.SINGLE:
        return new List<Player> { SelectRandomPlayer(players) };
      case SkillType.AOE:
          players.RemoveAll((player) => { return !player.IsDead(); });
          return players;
      case SkillType.BUFF:
          return new();
      default:
        throw new ArgumentException("Invalid skill type!");
    }
  }
}