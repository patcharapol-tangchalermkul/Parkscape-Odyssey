using System.Collections.Generic;

public abstract class Skill {
  abstract public SkillName Name { get; }
  abstract public SkillType SkillType { get; }

  public abstract void Perform(Monster monster, List<Player> players);
}