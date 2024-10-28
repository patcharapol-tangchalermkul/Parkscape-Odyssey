using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameInterfaceManager : MonoBehaviour
{
    private readonly GameState GameState = GameState.Instance;
    [SerializeField]
    private GameObject inventoryObject;
    
    [SerializeField]
    private GameObject playerViewPrefab;
    private GameObject playerView;

    [SerializeField]
    private GameObject questsOverlayPrefab;
    private GameObject questsOverlay;

    [SerializeField]
    private GameObject playerIcon;

    [SerializeField]
    private List<Sprite> playerIcons;

    [SerializeField]
    private GameObject arCameraToggle;

    [SerializeField]
    private Sprite arCameraIcon;

    [SerializeField]
    private Sprite mapIcon;

    [SerializeField]
    private GameObject scanImageButton;

    [SerializeField]
    private GameObject questNotification;

    private Dictionary<string, int> roleToIcon = new Dictionary<string, int>()
        {
            { "Rogue", 0 },
            { "Mage", 1 },
            { "Faerie", 2 },
            { "Cleric", 3 },
            { "Scout", 4 },
            { "Warrior", 5 }
        };

    void Awake() {
        Player myPlayer = GameState.Instance.MyPlayer;
        myPlayer.Icon = GetIcon(myPlayer.Role);
        List<Player> otherPlayers = GameState.Instance.OtherPlayers;
        foreach (Player p in otherPlayers) {
            p.Icon = GetIcon(p.Role);
        }
        scanImageButton.SetActive(false);
    }

    void Start() {
        CloseNewQuestNotification();
    }


    // Open with actual inventory stored in GameManager
    public void OpenInventory() {
        inventoryObject.GetComponent<InventoryUIManager>().OpenInventory();
    }

    public void CloseInventory() {
        inventoryObject.SetActive(false);
    }

    public void SetUpInterface() {
        // Set up player icon.
        ((Image) playerIcon.GetComponent(typeof(Image))).sprite = GameState.MyPlayer.Icon;
    }

    // Opens the player view with the player's role informationa and stats
    public void OpenPlayerView() {
        playerView = Instantiate(playerViewPrefab, new Vector3(0, 0, 0), Quaternion.identity);

        // Disable Map Interactions
        MapManager.Instance.DisableMapInteraction();


        // Set close button onClick
        Button closeButton = playerView.transform.Find("Close Button").GetComponent<Button>();
        closeButton.onClick.AddListener(ClosePlayerView);

        // Set up player view
        playerView.SetActive(false);
        PlayerViewManager playerViewManager = playerView.GetComponent<PlayerViewManager>();

        // Role information
        playerViewManager.SetPlayer(GameState.MyPlayer);
        playerViewManager.SetPlayerIcon(GameState.MyPlayer.Icon);

        playerView.SetActive(true);
    }

    private void ClosePlayerView() {
        MapManager.Instance.EnableMapInteraction();
        GameManager.Instance.ClosePlayerView();
        Destroy(playerView);
    }

    public Sprite GetIcon(string role) {
        if (!roleToIcon.ContainsKey(role)) {
            throw new Exception("Role Icon not found");
        }
        return playerIcons[roleToIcon[role]];
    }

    public void OpenQuests() {
        questsOverlay = Instantiate(questsOverlayPrefab, new Vector3(0, 0, 0), Quaternion.identity);

        // Disable Map Interactions
        MapManager.Instance.DisableMapInteraction();


        // Set close button onClick
        Button closeButton = questsOverlay.transform.Find("Close Button").GetComponent<Button>();
        closeButton.onClick.AddListener(CloseQuests);

        // Set up Quests
        questsOverlay.GetComponent<QuestsUIManager>().SetUp(
            GameState.Instance.basicQuests,
            new List<LocationQuest>(GameState.Instance.locationQuests.Values)
        );

        questsOverlay.SetActive(true);
    }

    private void CloseQuests() {
        MapManager.Instance.EnableMapInteraction();
        GameManager.Instance.CloseQuests();
        Destroy(questsOverlay);
    }

    public void DisplayNewQuestNotification() {
        questNotification.SetActive(true);
    }

    public void CloseNewQuestNotification() {
        questNotification.SetActive(false);
    }

    public void UpdateQuests() {
        if (questsOverlay != null) {
            questsOverlay.GetComponent<QuestsUIManager>().UpdateLocationQuests();
        }
    }

    public void SetARCameraToggle(bool ARMode) {
        if (ARMode) {
            arCameraToggle.GetComponent<Image>().sprite = mapIcon;
            scanImageButton.SetActive(true);
            MapManager.Instance.DisableMapInteraction();
        } else {
            arCameraToggle.GetComponent<Image>().sprite = arCameraIcon;
            scanImageButton.SetActive(false);
            MapManager.Instance.EnableMapInteraction();
        }
    }
}
