using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using TMPro;

using Firebase;
using Firebase.Firestore;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    public static GameManager selfReference;

    private GameInterfaceManager gameInterfaceManager;
    private DatabaseManager databaseManager;

    [SerializeField] private GameObject mainCamera;

    [SerializeField] private GameObject arSession;

    [SerializeField] private GameObject xrOrigin;
    private Depth_ScreenToWorldPosition depth_ScreenToWorldPosition;

    [SerializeField] private GameObject debugLoggerBox;
    [SerializeField] private GameObject debugLogger;
    [SerializeField] private AudioSource backgroundAudioSource;

    private Boolean inARMode = false;

    private volatile Boolean IsProcessingNewLocationQuests = false;

    private ListenerRegistration locationQuestListener;

    private ListenerRegistration musicListener;

    public static GameManager Instance {
        get {
            if (selfReference == null) {
                selfReference = new();
            }
            return selfReference;
        }
    }

    public void Awake() {
        selfReference = this;
    }

    // Start is called before the first frame update
    void Start() {
        GameObject databseOjb = GameObject.FindWithTag("Database");
        if (databseOjb == null) {
            Debug.LogError("Database not found.");
        } else {
            databaseManager = databseOjb.GetComponent<DatabaseManager>();
        }

        StartCoroutine(LoadMusicFromDisk());

        gameInterfaceManager = GetComponent<GameInterfaceManager>();
        gameInterfaceManager.SetUpInterface();
        databaseManager = GameObject.FindWithTag("Database").GetComponent<DatabaseManager>();
        depth_ScreenToWorldPosition = xrOrigin.GetComponent<Depth_ScreenToWorldPosition>();
        
        AddLocationQuestListener();
        AddMusicListener();
    }

    public IEnumerator LoadMusicFromDisk() {
        // Wait for DatabaseManager.Instance to be set
        while (DatabaseManager.Instance == null) {
            yield return null;
        }

        // Load any music saved on disk
        Debug.LogWarning("11. Loading music from disk");
        AudioSource backgroundAudioSource = GameObject.FindWithTag("BackgroundAudioSource").GetComponent<AudioSource>();
        Task<AudioClip> clipTask = FileUtils.LoadBackgroundMusicAsAudioClip();

        // Wait for the task to complete
        while (!clipTask.IsCompleted) {
            yield return null;
        }

        AudioClip clip = clipTask.Result;

        if (clip != null) {
            backgroundAudioSource.Stop();
            backgroundAudioSource.clip = clip;
            backgroundAudioSource.Play();
        }
    }

    // Add listener for location quest updates
    public void AddLocationQuestListener() {
        Query query = DatabaseManager.Instance.Database.Collection("locationQuests");
        
        // On an update to the locationQuests collection, we will:
        //  - Fetch the reference images to construct locationQuest objects
        //  - Fetch the updated files for the object classifier
        //  - Update the map of locationQuests in the GameState
        //  - Update the byte[] fields in the GameState
        //  - Save the new files to disk
        // The above steps must be atomic, i.e. saves should only be done
        // once all the required data is obtained from the database.
        // We will continuously try to fetch said data in the background.
        locationQuestListener = query.Listen(snapshot => {
            Debug.LogWarning("LocationQuests collection updated.");
            StartCoroutine(ProcessLocationQuestsUpdate(snapshot.GetChanges()));
        });
    }

    public void AddMusicListener() {
        DocumentReference query = DatabaseManager.Instance.Database.Collection("music").Document("music");
        musicListener = query.Listen(snapshot => {
            Debug.LogWarning("Music collection updated.");
            StartCoroutine(ProcessMusicUpdate(snapshot));
        });
    }

    public IEnumerator ProcessMusicUpdate(DocumentSnapshot snapshot) {
        if (snapshot == null) {
            yield break;
        }

        // Fetch the new music from Firebase and save it to disk
        Task<AudioClip> task = DatabaseUtils.ProcessMusicUpdateAsync(snapshot);

        // Wait for the task to complete
        while (!task.IsCompleted) {
            yield return null;
        }

        AudioClip clip = task.Result;

        if (clip == null) {
            Debug.LogError("New music was not able to be loaded (might be stale).");
            yield break;
        } else {
            Debug.LogWarning("Successfully downloaded new clip");

            if (backgroundAudioSource.isPlaying) {
                backgroundAudioSource.Stop();
                backgroundAudioSource.clip = clip;
                backgroundAudioSource.Play();
            } else {
                backgroundAudioSource.clip = clip;
            }
        }
    }

    public IEnumerator ProcessLocationQuestsUpdate(IEnumerable<DocumentChange> changes) {
        Debug.LogWarning("Done waiting - processing new location quests.");

        string previousUpdate = PlayerPrefs.GetString("LastQuestFileUpdate");
        Task task = DatabaseUtils.ProcessLocationQuestsUpdateAsync(changes, this);

        // Wait for the task to complete and for the last quest file update to change
        // The second condition is to ensure that the quest files have been saved to
        // the GameState. There may be read/write race conditions on the GameState otherwise.
        while (!task.IsCompleted || previousUpdate == PlayerPrefs.GetString("LastQuestFileUpdate")) {
            yield return null;
        }

        gameInterfaceManager.DisplayNewQuestNotification();
        gameInterfaceManager.UpdateQuests();
    }

    public void OpenInventory() {
        gameInterfaceManager.OpenInventory();
        if (inARMode) {
            depth_ScreenToWorldPosition.DisableARInteraction();
        }
    }

    public void CloseInventory() {
        gameInterfaceManager.CloseInventory();
        if (inARMode) {
            depth_ScreenToWorldPosition.EnableARInteraction();
        }
    }

    public void EndEncounter(int pointsToAdd=0) {
        if (!GameState.Instance.IsInEncounter) {
            Debug.LogError("Attempted to end encounter when there was none.");
        }
        Debug.Log($"Ending the encounter ({GameState.Instance.Score} -> {GameState.Instance.Score + pointsToAdd}).");
        GameState.Instance.Score += pointsToAdd;
        GameState.Instance.IsInEncounter = false;

        // Resume the background music
        backgroundAudioSource.Play();
    }

    // public void StartEncounter() {
    //     GameState.Instance.IsInEncounter = true;
    //     Debug.Log("Starting the encounter (wrong).");
    //     SceneManager.LoadScene("Battle", LoadSceneMode.Additive);
    // }

    // IEnumerator EncounterMonsterRandomly() {
    //     // float secondsToWait = Random.Range(5, 20);
    //     float secondsToWait = 10;
    //     yield return new WaitForSeconds(secondsToWait);
    //     Debug.Log("Waited 10s to start battle.");
    //     StartEncounter();
    // }

    public void OpenPlayerView() {
        gameInterfaceManager.OpenPlayerView();
        if (inARMode) {
            depth_ScreenToWorldPosition.DisableARInteraction();
        }
    }

    public void ClosePlayerView() {
        if (inARMode) {
            depth_ScreenToWorldPosition.EnableARInteraction();
        }
    }

    public void OpenQuests() {
        gameInterfaceManager.OpenQuests();
        if (inARMode) {
            depth_ScreenToWorldPosition.DisableARInteraction();
        }
    }

    public void CloseQuests() {
        if (inARMode) {
            depth_ScreenToWorldPosition.EnableARInteraction();
        }
    }

    //------------------------------- AR CAMERA -------------------------------
    public void ToggleARCamera() {
        if (inARMode) {
            CloseARSession();
            gameInterfaceManager.SetARCameraToggle(false);
        } else {
            OpenARSession();
            gameInterfaceManager.SetARCameraToggle(true);
        }
    }

    private void OpenARSession() {
        mainCamera.SetActive(false);
        arSession.SetActive(true);
        ARManager.Instance.StartAR();

        // Disable map interactions
        MapManager.Instance.DisableMapInteraction(true);

        inARMode = true;
    }

    private void CloseARSession() {
        ARManager.Instance.StopAR();
        arSession.SetActive(false);
        mainCamera.SetActive(true);

        // Enable map interactions
        MapManager.Instance.EnableMapInteraction(true);

        inARMode = false;
    }


    // ------------------------------ BUILD DEBUG ------------------------------
    public void LogTxt(string text) {
        if (!debugLogger.activeInHierarchy) {
            return;
        }
        debugLogger.GetComponent<TextMeshProUGUI>().text += "\n";
        debugLogger.GetComponent<TextMeshProUGUI>().text += text;
        debugLoggerBox.GetComponent<ScrollRect>().verticalNormalizedPosition = 0 ;
    }

    public void RelogTxt(string text) {
        if (!debugLogger.activeInHierarchy) {
            return;
        }
        debugLogger.GetComponent<TextMeshProUGUI>().text = text;
    }
}
