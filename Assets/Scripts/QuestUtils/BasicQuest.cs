using UnityEngine;

public class BasicQuest : Quest {
    public BasicQuest(QuestType questType, string label, Texture2D referenceImage, int target) 
        : base(questType, label, referenceImage, target) {
            // Set basic quests to be ongoing upon creation
            SetOngoing();
        }

    public override string ToString() {
        return QuestType switch {
            QuestType.FIND => "Find " + Target + " " + Label + ((Target > 1) ? "s" : ""),
            _ => "Unknown Quest Type",
        };
    }
}
