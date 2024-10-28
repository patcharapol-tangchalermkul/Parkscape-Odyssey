using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EncounterLobbyUIManager : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> partyMemberSlots;

    [SerializeField]
    private List<string> partyMembers;

    [SerializeField]
    private GameObject enemyDetailsPanel;

    [SerializeField]
    private GameObject startEncounterButton;
    [SerializeField]
    private GameObject mediumEncounterMessagePopupLeader;
    // FOR DEBUGGING ONLY
    [SerializeField]
    private int memberCountDebug;
    
    private string encounterId;
    private List<Monster> monsters;

    private EncounterController encounterController;
    private bool isMediumEncounter = false;

    void Update() {
        int expectedCount = GameState.Instance.PlayersDetails.Count;
        int memberCount = partyMembers.Count;
        if (GameState.MAPDEBUGMODE) {
            expectedCount = 2;
            memberCount = memberCountDebug;
        }
        // leader can start game if all players joined
        if (GameState.Instance.isLeader && isMediumEncounter && memberCount == expectedCount) {
            mediumEncounterMessagePopupLeader.SetActive(false);
            startEncounterButton.SetActive(true);
        }
    }

    public void ListPartyMembers(List<string> members) {
        for (int i = 0; i < partyMemberSlots.Count; i++) {
            if (i < members.Count)
                partyMemberSlots[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = members[i];
            else
                partyMemberSlots[i].GetComponent<Image>().enabled = false;
        }
    }

    public void EncounterLobbyInit(string encounterId, List<Monster> monsters, bool isLeader) {
        this.encounterId = encounterId;
        this.monsters = monsters;

        encounterController = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterController>();
        this.isMediumEncounter = encounterController.IsMediumEncounter(encounterId);
        DisplayMonsterDetails();

        // Only leader can start medium encounter when all players joined
        if (isMediumEncounter) {
            startEncounterButton.SetActive(false);
        }

        // Add self to party members
        if (isLeader) {
            partyMembers.Add(GameState.Instance.MyPlayer.Name);
            partyMemberSlots[0].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = GameState.Instance.MyPlayer.Name;
        } else {
            mediumEncounterMessagePopupLeader.SetActive(false);
            startEncounterButton.SetActive(false);
        }

        // Set all other party member slots to inactive
        for (int i = partyMembers.Count; i < partyMemberSlots.Count; i++) {
            partyMemberSlots[i].GetComponent<Image>().enabled = false;
        }
    }

    private void DisplayMonsterDetails() {
        enemyDetailsPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Enemy Level: {monsters[0].level}";
        enemyDetailsPanel.transform.GetChild(1).GetComponent<Image>().sprite = monsters[0].img;
    }

    public void MemberJoinedParty(string member) {
        partyMembers.Add(member);
        partyMemberSlots[partyMembers.Count - 1].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = member;
        partyMemberSlots[partyMembers.Count - 1].GetComponent<Image>().enabled = true;
    }

    public void StartEncounter() {
        encounterController.LeaderStartEncounter();
        SceneManager.LoadScene("Battle", LoadSceneMode.Additive);
        Destroy(gameObject);
    }

    public void ShowLeaderMediumEncounterMessagePopup() {
        mediumEncounterMessagePopupLeader.SetActive(true);
    }

    public void ExitEncounterLobby() {
        encounterController.ExitEncounterLobby();

        GameObject[] gameObjects;
        gameObjects = GameObject.FindGameObjectsWithTag("xrOrigin");
        if (gameObjects.Length == 0) {
            Debug.Log("No xrOrigin found");
        } else {
            GameObject xrOrigin = gameObjects[0];
            if (xrOrigin.activeInHierarchy) {
                xrOrigin.GetComponent<Depth_ScreenToWorldPosition>().EnableARInteraction();
            }
        }

        Destroy(gameObject);
    }
}
