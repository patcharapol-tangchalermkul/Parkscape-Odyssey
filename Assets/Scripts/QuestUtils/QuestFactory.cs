using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.Geospatial;

public class QuestFactory : MonoBehaviour {

    // ----------------------------- BASIC QUESTS ------------------------------
    // public static BasicQuest CreateFindObjectQuest(string label, int target, Texture2D referenceImage, float[] featureVector) {
        // return new BasicQuest(QuestType.FIND, label, target, referenceImage, featureVector);
    // }


    // ----------------------------- GAMESTATE INIT ----------------------------
    public static async Task<Dictionary<string, LocationQuest>> CreateInitialLocationQuests() {
        // List of hardcoded location quests based on Hyde Park
        // Pause while fetching the list of locationQuests from the database
        List<LocationQuest> locationQuests = await DatabaseUtils.GetLocationQuestsWithTimeout(15);

        // Add a medium encounter for each location
        List<LatLon> encounterLocations = new();
        foreach (LocationQuest locationQuest in locationQuests) {
            Debug.LogWarning("9. LocationQuest: " + locationQuest.Label + ", " + locationQuest.Location);
            encounterLocations.Add(
                new LatLon(
                    locationQuest.Location.LatitudeInDegrees,
                    locationQuest.Location.LongitudeInDegrees
            ));
        }

        Dictionary<string, LocationQuest> locationQuestsDict = new();
        foreach (LocationQuest locationQuest in locationQuests) {
            locationQuestsDict.Add(locationQuest.Label, locationQuest);
        }

        if (GameState.Instance.isLeader) {
            // Add the encounters to the game state
            GPSManager.SetMediumEncounterID(encounterLocations);
        }

        // Return a dictionary mapping each quest's label to the object
        return locationQuestsDict;
    }

    public static List<BasicQuest> CreateInitialBasicQuests() {
        List<BasicQuest> basicQuests = new();
        basicQuests.Add(new BasicQuest(QuestType.FIND, "flower", new Texture2D(1, 1), 1));
        basicQuests.Add(new BasicQuest(QuestType.FIND, "bird", new Texture2D(1, 1), 1));
        basicQuests.Add(new BasicQuest(QuestType.FIND, "duck", new Texture2D(1, 1), 1));
        return basicQuests;
    }
}