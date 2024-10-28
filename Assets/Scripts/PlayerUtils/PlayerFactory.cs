using System;
using System.Collections.Generic;

public class PlayerFactory {
    // Constants
    private const int BASEMANA = 3;
    private const int BASEHEALTH = 75;
    private const int BASESPEED = 5;

    private const string MageDescription = 
        "A brilliant scholar, adept at manipulating the arcane forces. " 
        + "He casts spells of various elements, effects, and ranges. "
        + "A master of knowledge and logic, he seeks to uncover the secrets of magic.";

    private const string WarriorDescription =
        "A loyal champion, trained in the art of war. He fights with honor, strength, and courage. "
        + "A protector of his friends, he shields them from harm. "
        + "A hero of sacrifice, he gives his life for them.";

    private const string RogueDescription =
        "A shadowy figure, adept at stealth and deception. He strikes from the dark, "
        + "using daggers and poison. A master of traps and lockpicking, he loots without remorse.";

    private const string ClericDescription =
        "A holy warrior, skilled in both combat and magic. He heals his allies, smites his foes, "
        + "and invokes divine power. A leader of faith and valor, he inspires courage.";
    
    private const string FaerieDescription =
        "A peculiar creature, with a knack for finding four-leaf clovers, lucky pennies, and shooting stars. "
        + "They always seemed to be in the right place at the right time, avoiding danger and attracting fortune.";

    private const string ScoutDescription =
        "A keen explorer, gifted with sharp vision and curiosity. He sees things others miss, "
        + "using his telescope and map. A master of navigation and tracking, he scouts the land ahead.";

    // Factory Methods
    public static List<string> GetRoles() {
        return new List<string> { "Mage", "Warrior", "Rogue", "Cleric", "Faerie", "Scout" };
    }

    public static Player CreatePlayer(string id, string name, string role) {
        switch (role) {
            case "Mage":
                return CreateMage(id, name);
            case "Warrior":
                return CreateWarrior(id, name);
            case "Rogue":
                return CreateRogue(id, name);
            case "Cleric":
                return CreateCleric(id, name);
            case "Faerie":
                return CreateFaerie(id, name);
            case "Scout":
                return CreateScout(id, name);
            default:
                throw new ArgumentException("Invalid role: " + role);
        }
    }

    public static Player CreateMage(string id, string name) {
        return new Player(id: id,
                          name: name, 
                          role: "Mage", 
                          speed: BASESPEED, 
                          maxHealth: BASEHEALTH - 10, 
                          maxMana: BASEMANA + 1,
                          description: MageDescription);
    }

    public static Player CreateWarrior(string id, string name) {
        return new Player(id: id,
                          name: name, 
                          role: "Warrior", 
                          speed: BASESPEED - 2, 
                          maxHealth: BASEHEALTH + 25, 
                          maxMana: BASEMANA,
                          description: WarriorDescription);
    }

    public static Player CreateRogue(string id, string name) {
        return new Player(id: id,
                          name: name, 
                          role: "Rogue", 
                          speed: BASESPEED + 2, 
                          maxHealth: BASEHEALTH - 15, 
                          maxMana: BASEMANA,
                          description: RogueDescription);
    }

    public static Player CreateCleric(string id, string name) {
        return new Player(id: id,
                          name: name, 
                          role: "Cleric", 
                          speed: BASESPEED, 
                          maxHealth: BASEHEALTH - 10, 
                          maxMana: BASEMANA,
                          description: ClericDescription);
    }

    public static Player CreateFaerie(string id, string name) {
        return new Player(id: id,
                          name: name, 
                          role: "Faerie", 
                          speed: BASESPEED + 3, 
                          maxHealth: BASEHEALTH - 30, 
                          maxMana: BASEMANA + 2,
                          description: FaerieDescription);
    }

    public static Player CreateScout(string id, string name) {
        return new Player(id: id,
                          name: name, 
                          role: "Scout", 
                          speed: BASESPEED - 1, 
                          maxHealth: BASEHEALTH - 10, 
                          maxMana: BASEMANA,
                          description: ScoutDescription);
    }
}