using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PopUpManagerTest
{
    [UnityTest]
    public IEnumerator ExpandsPopUpGameObject()
    {
        // Initialisation
        PopUpManager popUpManager = new PopUpManager();
        popUpManager.popup = new GameObject();
        popUpManager.popup.transform.localScale = new Vector3(0, 0, 0);
        // Attempt to open pop up
        popUpManager.openPopUp();
        // Check if pop up is open
        Assert.AreEqual(new Vector3(1, 1, 1), popUpManager.popup.transform.localScale);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ClosesPopUpGameObject()
    {
        // Initialisation
        PopUpManager popUpManager = new PopUpManager();
        popUpManager.popup = new GameObject();
        popUpManager.popup.transform.localScale = new Vector3(1, 1, 1);
        // Attempt to close pop up
        popUpManager.closePopUp();
        // Check if pop up is closed
        Assert.AreEqual(new Vector3(0, 0, 0), popUpManager.popup.transform.localScale);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ConsecutiveOpensHasNoEffect()
    {
        // Initialisation
        PopUpManager popUpManager = new PopUpManager();
        popUpManager.popup = new GameObject();
        popUpManager.popup.transform.localScale = new Vector3(0, 0, 0);
        // Attempt to open pop up
        popUpManager.openPopUp();
        // Check if pop up is open
        Assert.AreEqual(new Vector3(1, 1, 1), popUpManager.popup.transform.localScale);
        // Open again and check if pop up is still open
        popUpManager.openPopUp();
        Assert.AreEqual(new Vector3(1, 1, 1), popUpManager.popup.transform.localScale);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ConsecutiveClosesHasNoEffect()
    {
        // Initialisation
        PopUpManager popUpManager = new PopUpManager();
        popUpManager.popup = new GameObject();
        popUpManager.popup.transform.localScale = new Vector3(1, 1, 1);
        // Attempt to close pop up
        popUpManager.closePopUp();
        // Check if pop up is closed
        Assert.AreEqual(new Vector3(0, 0, 0), popUpManager.popup.transform.localScale);
        // Close again and check if pop up is still closed
        popUpManager.closePopUp();
        Assert.AreEqual(new Vector3(0, 0, 0), popUpManager.popup.transform.localScale);
        yield return null;
    }
}
