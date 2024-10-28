using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;

public class InventoryTest
{
    [UnityTest]
    public IEnumerator CardInventoryDisplayAllCardOnOpen()
    {
        // open card invetory
        // GameObject cardInventoryPrefab = AssetDatabase.LoadAssetAtPath("Prefabs/Inventory/CardsInventory", typeof(GameObject)) as GameObject;
        // Debug.Log(cardInventoryPrefab);
        // GameObject cardInventory = MonoBehaviour.Instantiate(cardInventoryPrefab);

        // // get all cards in inventory
        // List<string> cards = cardInventory.GetComponent<InventoryController>().inventoryCards;
        // // Debug.Log(cards.Count);
        // // Debug.Log(GameObject.FindGameObjectWithTag("CardsInventoryContent").transform.childCount);
        // Assert.AreEqual(cards.Count, GameObject.FindGameObjectWithTag("CardsInventoryContent").transform.childCount);
        return null;
    }
}
