using Microsoft.Geospatial;
using UnityEngine;
using System;
using System.Linq;

[Serializable]
public class LocationQuest : Quest {
    public QuestLocation Location {
        get => _location;
        private set => _location = value;
    }
    
    [SerializeField]
    private QuestLocation _location;

    public LocationQuest(QuestType questType, string label, Texture2D referenceImage, LatLon location) 
        : base(questType, label, referenceImage, 1) {
        Location = new QuestLocation(location);
    }

    public override string ToString() {
        return QuestType switch {
            QuestType.FIND => "Find the " + Label,
            _ => "Unknown Quest Type",
        };
    }

    public bool AttemptQuest(Texture2D image) {
        if (ImageIsCorrect(image)) {
            IncrementProgress();
            return true;
        }
        return false;
    }

    // Check if the image taken is the correct object
    public bool ImageIsCorrect(Texture2D image) {
        string[] searchResults = VecSearchManager.Instance.ClassifyImage(image);
        Debug.Log("Searching for " + Label + " in " + string.Join(", ", searchResults));
        return searchResults.Contains(Label);
    }
}
