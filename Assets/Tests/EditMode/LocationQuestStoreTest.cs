// using System.Collections;
// using System.Collections.Generic;
// using NUnit.Framework;
// using UnityEngine;
// using UnityEngine.TestTools;

// public class LocationQuestStoreTest
// {
//     [Test]
//     public void Remove_RemovesQuestWithMatchingLabel()
//     {
//         // Arrange
//         LocationQuestStore questStore = new LocationQuestStore();
//         LocationQuest quest1 = new LocationQuest("Quest1", null, null);
//         LocationQuest quest2 = new LocationQuest("Quest2", null, null);
//         LocationQuest quest3 = new LocationQuest("Quest3", null, null);
//         questStore.Quests = new List<LocationQuest> { quest1, quest2, quest3 };

//         // Act
//         questStore.Remove("Quest2");

//         // Assert
//         Assert.AreEqual(2, questStore.Quests.Count);
//         Assert.IsFalse(questStore.Quests.Contains(quest2));
//     }

//     [Test]
//     public void AddNewQuests_AddsQuestsWithUniqueLabels()
//     {
//         // Arrange
//         LocationQuestStore questStore = new LocationQuestStore();
//         LocationQuest quest1 = new LocationQuest("Quest1", null, null);
//         LocationQuest quest2 = new LocationQuest("Quest2", null, null);
//         LocationQuest quest3 = new LocationQuest("Quest3", null, null);
//         questStore.Quests = new List<LocationQuest> { quest1, quest2 };

//         // Act
//         questStore.AddNewQuests(new List<LocationQuest> { quest2, quest3 });

//         // Assert
//         Assert.AreEqual(3, questStore.Quests.Count);
//         Assert.IsTrue(questStore.Quests.Contains(quest1));
//         Assert.IsTrue(questStore.Quests.Contains(quest2));
//         Assert.IsTrue(questStore.Quests.Contains(quest3));
//     }
// }
