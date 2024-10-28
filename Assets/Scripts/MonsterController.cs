using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [SerializeField]
    private List<MonsterName> monsterNames;

    [SerializeField]
    private List<Sprite> monsterImgs;

    private readonly Dictionary<MonsterName, Sprite> allMonsters = new();

    void Awake() {
        if (monsterNames.Count != monsterImgs.Count) {
            Debug.LogWarning("MonsterController: Monster Names and Monster Images provided do not have the same amount. Please check these fields.");
            return;
        }

        // Initialise data for all monsters
        for (int i = 0; i < monsterNames.Count; i++) {
            allMonsters.Add(monsterNames[i], monsterImgs[i]);
        }
    }

    public Monster createMonster(MonsterName name) {
        Sprite img = allMonsters[name];
        return MonsterFactory.CreateMonster(name, img);
    }

    public MonsterName GetRandomMonster() {
        int index = UnityEngine.Random.Range(0, monsterNames.Count);
        return monsterNames[index];
    }

    // Gets monster sprite
    public Sprite GetMonsterSprite(MonsterName name) {
        return allMonsters[name];
    }
}
