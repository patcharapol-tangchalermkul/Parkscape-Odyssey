using System.Collections.Generic;
using UnityEngine;
using TMPro;

// This class is responsible for managing the lobby UI.
// This class exposes methods for the LobbyManager to call.
public class LobbyUIManager : MonoBehaviour
{
    private int maxPlayerCount = 6;
    private string roomCode;
    private bool isLeader = false;
    private bool inLobby = false;
    public TMP_Text roomCodeText;
    public GameObject playerList;
    public GameObject startGameButton;
    private PopUpManager lobbyPopUpManager;
    private PopUpManager startGameButtonPopUpManager;

    private List<string> playerNames = new();
    private int updateCounter = 0;
    private float spinCounter = 0;
    private int spinDuration = 1;

    void Start() {
        // Ensure pop up is closed at start.
        lobbyPopUpManager = GetComponent<PopUpManager>();
        lobbyPopUpManager.closePopUp();
        startGameButtonPopUpManager = (PopUpManager) startGameButton.GetComponent(typeof(PopUpManager));
    } 

    // Called when code is submitted in the room selection pop up
    public void SetUpLobby(string roomCode, bool isLeader) {
        Debug.Log("Setting up lobby with room code " + roomCode);
        this.roomCode = roomCode;
        this.isLeader = isLeader;
        roomCodeText.text = roomCode;
        this.inLobby = true;

        DisplayPlayerList();

        // Disable start game button if not leader. Ensure it appears if they are.
        if (!isLeader) {
            startGameButtonPopUpManager.closePopUp();
        } else {
            startGameButtonPopUpManager.openPopUp();
        }

        // Open lobby pop up when everything is set up
        lobbyPopUpManager.openPopUp();
    }

    public void AddPlayer(string name) {
        playerNames.Add(name);
        foreach (string player in playerNames) {
            Debug.Log(player);
        }
        DisplayPlayerList();
        // updateCounter++;
    }

    public void RemovePlayer(string name) {
        playerNames.Remove(name);
        updateCounter++;
    }

    public void SetPlayers(List<string> playerNames) {
        this.playerNames = playerNames;
        updateCounter = 1;
    }

    // Called when the player clicks the cancel button while in lobby, or when the connection drops.
    // Additionally, this should drop the connection and the player from the room. 
    public void ExitLobby() {
        this.inLobby = false;
        this.playerNames = new List<string>();
        lobbyPopUpManager.closePopUp();
    }

    // Update is called once per frame
    void Update() {
        // Debug.Log("LobbyUIManager Update");
        if (!inLobby || updateCounter == 0)
            return;
        
        // Listen for any new connections or dropped connections
        if (spinCounter > spinDuration) {
            DisplayPlayerList();
            spinCounter = 0;
            updateCounter--;
        } else {
            spinCounter += Time.deltaTime;
        }
    }

    // For each player in the room, display their name in the player list.
    // In terms of the UI, to display a player, set the text and make the object visible.
    // To hide a player, make the object invisible.
    void DisplayPlayerList() {
        for (int i = 0; i < maxPlayerCount; i++) {
            if (i < playerNames.Count) {
                GameObject playerCard = playerList.transform.GetChild(i).gameObject;
                TMP_Text playerName = playerCard.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
                playerName.text = playerNames[i];
                PopUpManager playerCardPopUpManager = (PopUpManager) playerCard.GetComponent(typeof(PopUpManager));
                playerCardPopUpManager.openPopUp();
            } else {
                GameObject playerCard = playerList.transform.GetChild(i).gameObject;
                PopUpManager playerCardPopUpManager = (PopUpManager) playerCard.GetComponent(typeof(PopUpManager));
                playerCardPopUpManager.closePopUp();
            }
        }
    }
}
