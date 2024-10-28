using UnityEngine;
using Microsoft.Geospatial;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    private static QuestManager instance;

    public static QuestManager Instance { 
        get {
            if (instance == null) {
                // To make sure that script is persistent across scenes
                GameObject go = new GameObject("QuestManager");
                instance = go.AddComponent<QuestManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Start()
    {
    }

    private void Update()
    {
        // Add update logic here
    }

    public void GetNextLocationQuest() {
        LatLon currentLocation = GPSManager.Instance.GetLocation();
        double minDistance = double.MaxValue;
        LocationQuest nearestLocationQuest = null;
        // Get nearest unattempted location quest
        Debug.LogWarning("in getnextlocationquest: " + GameState.Instance.locationQuests.Values.Count);
        foreach (LocationQuest locationQuest in GameState.Instance.locationQuests.Values) {
            if (locationQuest.HasNotStarted()) {
                double distance = 
                    MapManager.DistanceBetweenCoordinates(
                        currentLocation.LatitudeInDegrees, currentLocation.LongitudeInDegrees, 
                        locationQuest.Location.LatitudeInDegrees, locationQuest.Location.LongitudeInDegrees);
                if (distance < minDistance) {
                    minDistance = distance;
                    nearestLocationQuest = locationQuest;
                }
            }
        }
        if (nearestLocationQuest != null) {
            // Add quest to ongoing quests
            nearestLocationQuest.SetOngoing();
            Debug.Log("Adding quest: " + nearestLocationQuest.ToString());
        } else {
            // No more location quests
            Debug.Log("No more location quests");
        }
    }

    // ----------------------------- REWARDS ------------------------------
    // Checks if any basic quests have been completed.
    public BasicQuest CheckBasicQuests(List<string> labels) {
        foreach (BasicQuest quest in GameState.Instance.basicQuests) {
            if (quest.IsOnGoing() && labels.Contains(quest.Label)) {
                quest.IncrementProgress();
                return quest;
            }
        }
        return null;
    }

    // Checks if any location quests have been completed, returns a quest if progressed or completed.
    public LocationQuest CheckLocationQuests(Texture2D image) {
        foreach (LocationQuest quest in GameState.Instance.locationQuests.Values) {
            // Only one ongoing location quest at a time
            if (quest.IsOnGoing() && quest.AttemptQuest(image)) {
                if (quest.IsCompleted()) {
                    GetNextLocationQuest();
                }
                return quest;
            }
        }
        return null;
    }
}
