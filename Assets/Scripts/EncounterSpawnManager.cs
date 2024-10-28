using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EncounterSpawnManager : MonoBehaviour
{
    private List<Monster> monsters;

    private string encounterId;
    private EncounterType encounterType;

    private GameObject encounterManager;

    public void EncounterSpawnInit(string encounterId, List<Monster> monsters, EncounterType encounterType) {
        this.encounterId = encounterId;
        this.monsters = monsters;
        this.encounterType = encounterType;
        encounterManager = GameObject.FindGameObjectWithTag("EncounterManager");
        Debug.Log($"EncounterId {encounterId}");
    }

    public void EncounterOnClick() {
        if (encounterType == EncounterType.RANDOM_ENCOUNTER) {
            Debug.Log("Random Encounter Clicked");
            encounterManager.GetComponent<EncounterController>()
                .ShowRandomEncounterPopup(encounterId, monsters);
        } else if (encounterType == EncounterType.MEDIUM_BOSS) {
            Debug.Log("Medium Boss Clicked");
            CreateEncounterLobby();
        }
    }

    private void CreateEncounterLobby() {
        // Create a new encounter lobby
        encounterManager.GetComponent<EncounterController>()
            .CreateEncounterLobby(encounterId, monsters);
    }

    public string GetEncounterId() {
        return encounterId;
    }
}
