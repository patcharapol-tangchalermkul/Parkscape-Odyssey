using System;
using System.Collections.Generic;
using Microsoft.Geospatial;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.PersistentAnchors;
using UnityEngine;

public class ARManager : MonoBehaviour
{
    public static ARManager selfReference;

    [SerializeField] private GameObject gameManagerObj;
    private GameManager gameManager;
    
    [SerializeField] private GameObject xrInteractionManager;
    
    [SerializeField] private GameObject xrOrigin;

    [SerializeField] private GameObject arCamera;

    [SerializeField] private GameObject semanticsRawImage;

    [SerializeField] private GameObject semanticsLabel;

    [SerializeField] private GameObject arEncounterSpawnManager;

    [SerializeField]
    private GameObject scannerLinePrefab;
    [SerializeField]
    private GameObject canvas;

    [SerializeField]
    private GameObject ARQuestRewardHandlerObj;
    private ARQuestRewardHandler ARQuestRewardHandler;

    [SerializeField]
    private GameObject questResultPopUp;

    private ObjectDetectionManager objectDetectionManager;

    public static ARManager Instance {
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

    public void Start() {
        gameManager = gameManagerObj.GetComponent<GameManager>();
        objectDetectionManager = GetComponent<ObjectDetectionManager>();
        ARQuestRewardHandler = ARQuestRewardHandlerObj.GetComponent<ARQuestRewardHandler>();

        // Enable AR interactions
        xrOrigin.GetComponent<Depth_ScreenToWorldPosition>().EnableARInteraction();
    }

    // public void ActivateTestLocation() {
    //     ARLocationManager locationManager = xrOrigin.GetComponent<ARLocationManager>();
    //     locationManager.StopTracking();
    //     locationManager.SetARLocations(arSpawnLocations[0].location);
    //     locationManager.StartTracking();
    //     gameManager.LogTxt("Test location activated.");
    // }

    // public void ActivateMuralLocation() {
    //     ARLocationManager locationManager = xrOrigin.GetComponent<ARLocationManager>();
    //     locationManager.StopTracking();
    //     locationManager.SetARLocations(arSpawnLocations[1].location);
    //     locationManager.StartTracking();
    //     gameManager.LogTxt("Mural location activated.");
    // }

    public void StartAR() {
        Debug.Log("Starting AR session.");
        arCamera.SetActive(true);
        semanticsRawImage.SetActive(true);
        semanticsLabel.SetActive(true);
        arEncounterSpawnManager.GetComponent<EncounterObjectManager>().SetARMode(true);
    }

    public void StopAR() {
        Debug.Log("Stopping AR session.");
        arCamera.SetActive(false);
        semanticsRawImage.SetActive(false);
        semanticsLabel.SetActive(false);
        arEncounterSpawnManager.GetComponent<EncounterObjectManager>().SetARMode(false);
    }

    public Texture2D TakeScreenCapture() {
        Debug.Log("Taking a screen capture.");
        return ScreenCapture.CaptureScreenshotAsTexture();
    }

    // Onclick button for taking images
    public void TakeQuestImage() {
        // Trigger Scanning animation
        ScannerController scannerController = TriggerScannerEffect();
        Texture2D screenCapture = TakeScreenCapture();
        gameManager.LogTxt("Screen capture taken.");
        Quest successQuest = null;
        LocationQuest locationQuest = QuestManager.Instance.CheckLocationQuests(screenCapture);
        if (locationQuest != null) {
            successQuest = locationQuest;
            gameManager.LogTxt("Location quest :" + locationQuest.Label + " progress: " + locationQuest.Progress);
        } else {
            gameManager.LogTxt("No location quest progress.");
            // Attempt basic Quests if location quest not fulfilled
            List<string> labels = objectDetectionManager.GetLabels();
            gameManager.LogTxt("Labels: " + string.Join(", ", labels));
            BasicQuest basicQuest = QuestManager.Instance.CheckBasicQuests(labels);
            if (basicQuest != null) {
                gameManager.LogTxt("Basic quest :" + basicQuest.Label + " progress: " + basicQuest.Progress);
                if (basicQuest.IsCompleted()) {
                    successQuest = basicQuest;
                    gameManager.LogTxt("Basic quest completed.");
                }
            } else {
                gameManager.LogTxt("No basic quest progress.");
            }
            
        }
        scannerController.SetSuccessQuest(successQuest);
        scannerController.SetReady();
        successQuest = null;
        // FUTURE: Save images.
    }

    private ScannerController TriggerScannerEffect() {
        Debug.Log("Triggering scanner effect.");
        GameObject scannerLine = Instantiate(scannerLinePrefab, canvas.transform);
        ScannerController scannerController = scannerLine.GetComponent<ScannerController>(); 
        Debug.Log("Instantiated scanner line.");
        return scannerController;
    }

    public void ShowQuestResultPopUp(Quest quest) {
        Debug.Log("Showing quest result pop up.");
        QuestsProgressPopUpManager questResultPopUpManager = questResultPopUp.GetComponent<QuestsProgressPopUpManager>();
        questResultPopUpManager.ShowQuestResultPopUp(quest);
    }

    public void TriggerReward(Quest quest) {
        if (quest != null) {
            if (quest is LocationQuest) {
                ARQuestRewardHandler.TriggerReward((LocationQuest) quest);
            } else {
                ARQuestRewardHandler.TriggerReward((BasicQuest) quest);
            }
        }
    }

    // NOT USED FOR NOW
    // public void SpawnAllLocations() {
    //     foreach (GameObject arLocation in arSpawnLocations) {
    //         GameObject spawnedARLocation = Instantiate(arLocation, xrOrigin.transform);
    //         spawnedARLocation.transform.SetParent(xrOrigin.transform);
    //     }
    // }

    // NOT USED FOR NOW
    // public void DeSpawnAllLocations() {
    //     var i = 0;
    //     foreach (Transform child in xrOrigin.transform) {
    //         if (i != 0) {
    //             Destroy(child.gameObject);
    //         }
    //         i++;
    //     }
    // }
}
