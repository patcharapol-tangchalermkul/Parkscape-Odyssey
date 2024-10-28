using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FileUtilsTest : IPrebuildSetup {
    // Serializable class for testing
    //
    // Multidimensional arrays are NOT serializable:
    // https://docs.unity3d.com/Manual/script-Serialization.html
    // This can be worked around by having a wrapper class containing a List,
    // then storing a List of that wrapper class.
    [Serializable]
    private class SerializableClass {
        public string testString;
        public int testInt;
        public float[] testFloatArray;
        public List<int> testList;
    }

    public string root = TestContext.CurrentContext.TestDirectory;

    public void Setup() {
        // Create a DatabaseManager in the prebuild setup
        // Before any tests run, its Awake() method runs, setting the Instance and dataPath
        DatabaseManager dbManager = new GameObject().AddComponent<DatabaseManager>();
    }

    [UnityTest]
    public IEnumerator ShouldUseDefaultQuestFiles_ReturnsTrue_WhenLastQuestFileUpdateKeyNotPresent() {
        Assert.IsTrue(DatabaseManager.Instance != null);

        PlayerPrefs.DeleteKey("LastQuestFileUpdate");

        bool shouldUseDefaultQuestFiles = FileUtils.ShouldUseDefaultQuestFiles();

        Assert.IsTrue(shouldUseDefaultQuestFiles);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Save_SavesBytesCorrectlyToDisk() {
        string fileName = "testFile";
        string folder = "testFolder";
        byte[] data = Encoding.ASCII.GetBytes("Hello, World!");

        FileUtils.Save(data, fileName, folder, root);
        string filePath = FileUtils.GetFilePath(fileName, folder, root);

        // Assert that the file is saved to disk
        Assert.IsTrue(File.Exists(filePath));

        // Assert that the saved data matches the original data
        string savedData = File.ReadAllText(filePath);
        Assert.AreEqual(data, savedData);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Save_SavesSerialiazableObjectCorrectlyToDisk() {
        string fileName = "testFile";
        string folder = "testFolder";
        var data = new SerializableClass {
            testString = "Hello, World!",
            testInt = 42,
            testFloatArray = new float[] { 1.0f, 2.0f, 3.0f },
            testList = new List<int> { 1, 2, 3 }
        };

        FileUtils.Save(data, fileName, folder, root);
        string filePath = FileUtils.GetFilePath(fileName, folder, root);

        // Assert that the file is saved to disk
        Assert.IsTrue(File.Exists(filePath));

        // Load the bytes and convert it back into a SerialiazableClass object
        byte[] savedData = File.ReadAllBytes(filePath);
        string jsonData = Encoding.ASCII.GetString(savedData);
        var savedSerializableClass = JsonUtility.FromJson<SerializableClass>(jsonData);

        // Assert that the saved SerializableClass matches the original data
        Assert.AreEqual(data.testString, savedSerializableClass.testString);
        Assert.AreEqual(data.testInt, savedSerializableClass.testInt);
        Assert.AreEqual(data.testFloatArray, savedSerializableClass.testFloatArray);
        Assert.AreEqual(data.testList, savedSerializableClass.testList);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Load_LoadsDataFromFile() {
        string fileName = "testFile";
        string folder = "testFolder";
        string originalString = "Hello, World!";
        byte[] data = Encoding.ASCII.GetBytes(originalString);

        string filePath = FileUtils.GetFilePath(fileName, folder, root);

        // Save data to file (should have already been tested)
        FileUtils.Save(data, fileName, folder, root);

        // Load data from file
        byte[] loadedData = FileUtils.Load<byte[]>(fileName, folder, root);

        // Convert loaded data to string
        string loadedString = Encoding.ASCII.GetString(loadedData);

        // Assert that the loaded data matches the original data
        Assert.AreEqual(originalString, loadedString);

        yield return null;
    }

    [UnityTest]
    public IEnumerator ProcessNewQuestFiles_SavesFilesToDiskAndUpdatesLastQuestFileUpdateTime() {
        byte[] locationQuestVectors = new byte[] { 1, 2, 3 };
        byte[] locationQuestGraph = new byte[] { 4, 5, 6 };
        byte[] locationQuestLabels = new byte[] { 7, 8, 9 };

        FileUtils.ProcessNewQuestFiles(
            locationQuestVectors, locationQuestGraph, locationQuestLabels,
            root: root
        );

        Assert.IsTrue(DatabaseManager.Instance != null);

        // Assert that the files are saved to disk
        Assert.IsTrue(File.Exists(FileUtils.GetFilePath("locationQuestVectors.bytes", "quests", root)));
        Assert.IsTrue(File.Exists(FileUtils.GetFilePath("locationQuestGraph.bytes", "quests", root)));
        Assert.IsTrue(File.Exists(FileUtils.GetFilePath("locationQuestLabels.bytes", "quests", root)));

        // Assert that the data is saved to disk correctly
        byte[] savedLocationQuestVectors = File.ReadAllBytes(FileUtils.GetFilePath("locationQuestVectors.bytes", "quests", root));
        byte[] savedLocationQuestGraph = File.ReadAllBytes(FileUtils.GetFilePath("locationQuestGraph.bytes", "quests", root));
        byte[] savedLocationQuestLabels = File.ReadAllBytes(FileUtils.GetFilePath("locationQuestLabels.bytes", "quests", root));
        Assert.AreEqual(locationQuestVectors, savedLocationQuestVectors);
        Assert.AreEqual(locationQuestGraph, savedLocationQuestGraph);
        Assert.AreEqual(locationQuestLabels, savedLocationQuestLabels);

        // Assert that the last quest file update time is updated in PlayerPrefs
        Assert.IsTrue(PlayerPrefs.HasKey("LastQuestFileUpdate"));

        yield return null;
    }

    // [UnityTest]
    // public IEnumerator LoadBytesFromResources_LoadsBytesFromFileInResourcesFolder()
    // {
    //     string fileName = "helloWorld";
    //     string folder = "testFolder";
    //     byte[] data = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 };

    //     TextAsset testAsset = Resources.Load(fileName, typeof(TextAsset)) as TextAsset;
    //     yield return null;

    //     // Load bytes from Resources folder
    //     byte[] loadedBytes = FileUtils.LoadBytesFromResources(fileName, folder);

    //     // Assert that the loaded bytes match the original data
    //     Assert.AreEqual(data, loadedBytes);

    //     yield return null;
    // }
}
