using System;
using System.Collections.Generic;

[Serializable]
public class LocationQuestStore
{
    public List<LocationQuest> quests = new List<LocationQuest>();

    public LocationQuestStore(List<LocationQuest> quests) {
        this.quests.AddRange(quests);
    }

    public void Add(LocationQuest quest) {
        quests.Add(quest);
    }

    public void Remove(string label) {
        int index = quests.FindIndex(q => q.Label == label);
        if (index >= 0) {
            quests.RemoveAt(index);
        }
    }

    // Add quests whose labels are not already in the store
    public void AddNewQuests(List<LocationQuest> newQuests) {
        foreach (var quest in newQuests) {
            if (quests.FindIndex(q => q.Label == quest.Label) < 0) {
                quests.Add(quest);
            }
        }
    }
}
