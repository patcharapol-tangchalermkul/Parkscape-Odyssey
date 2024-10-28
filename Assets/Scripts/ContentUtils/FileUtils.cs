using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;

using UnityEngine;
using UnityEngine.Networking;

using Microsoft.Geospatial;

public static class FileUtils {
    // This is the path where data files (e.g. for OR location quests) should be stored.
    // TODO: For complete documentation of the structure of this directory, see XXX.

    // Flag to indicate whether file saving has completed on a separate thread
    private static volatile bool filesSaved_Threaded = false;

    // Return the full filepath in the Application's persistant data folder
    public static string GetFilePath(string fileName, string folder="", string root=null) {
        return Path.Combine(root != null ? root : DatabaseManager.Instance.dataPath, folder, fileName);
    }

    /*
     *************************************************************************
     * Utility functions for the handling of location quest files
     *************************************************************************
     */

    // Check if the default quest files should be used (i.e. if no quest files
    // have been downloaded from Firebase yet)
    public static bool ShouldUseDefaultQuestFiles() {
        // True only if the last quest file update time is not set, or
        // the quest files are not present on disk
        return !PlayerPrefs.HasKey("LastQuestFileUpdate") ||
               !File.Exists(GetFilePath("locationQuestVectors", "quests")) ||
               !File.Exists(GetFilePath("locationQuestGraph", "quests")) ||
               !File.Exists(GetFilePath("locationQuestLabels", "quests"));
    }

    // Save the three given quest files (as byte arrays) to the GameState instance, and write them to disk
    // asynchronously. Also, update the last quest file update time in PlayerPrefs.
    public static void ProcessNewQuestFiles(
        byte[] locationQuestVectors, byte[] locationQuestGraph,
        byte[] locationQuestLabels, string folder="quests", string root=null, bool updateGameState=true) {
        Debug.LogWarning("Saving quest files to disk on new thread");

        if (updateGameState) {
            // Save the quest files to the GameState instance and
            // re-initialise the VecSearchManager
            GameState.Instance.locationQuestVectors = locationQuestVectors;
            GameState.Instance.locationQuestGraph = locationQuestGraph;
            GameState.Instance.locationQuestLabels = locationQuestLabels;
        }

        Save(locationQuestVectors, "locationQuestVectors.bytes", "quests");
        Save(locationQuestGraph, "locationQuestGraph.bytes", "quests");
        Save(locationQuestLabels, "locationQuestLabels.bytes", "quests");


        if (GameManager.Instance != null) {
            // Check thatt the file saved on disk  is the same as the one in the game state
            byte[] vectors = Load<byte[]>("locationQuestVectors.bytes", folder, root);
            byte[] graph = Load<byte[]>("locationQuestGraph.bytes", folder, root);
            byte[] labels = Load<byte[]>("locationQuestLabels.bytes", folder, root);

            if (vectors == null || graph == null || labels == null) {
                GameManager.Instance.LogTxt("Failed to load quest files from disk");
            } else if (!vectors.SequenceEqual(locationQuestVectors) ||
                       !graph.SequenceEqual(locationQuestGraph) ||
                       !labels.SequenceEqual(locationQuestLabels)) {
                GameManager.Instance.LogTxt("Saved quest files do not match the ones in the GameState");
            } else {
                GameManager.Instance.LogTxt("Quest files saved to disk on new thread :)");
            
            }
        }
        
        if (VecSearchManager.Instance != null) {
                VecSearchManager.Instance.Initialize();
        }
            // GameManager.Instance.LogTxt("GameManager was null.");
        Debug.LogWarning("Quest files saved to disk on new thread");



        filesSaved_Threaded = false;

        // Update the last quest file update time in PlayerPrefs
        PlayerPrefs.SetString("LastQuestFileUpdate", DateTime.Now.ToString());
    }

    /*
     *************************************************************************
     * Utility functions for saving files to disk
     *************************************************************************
     */

    // Save the given data as a serialised JSON string to the persistent data folder
    // See Unity docs on serializability: https://docs.unity3d.com/ScriptReference/JsonUtility.ToJson.html
    public static void Save<TData>(TData data, string fileName, string folder="", string root=null) {
        string filePath = GetFilePath(fileName, folder, root); 

        byte[] byteData;

        // If the data is already a byte array, we can save it directly
        if (typeof(TData) != typeof(byte[])) {
            string jsonData = JsonUtility.ToJson(data, true);
            Debug.LogWarning("JsonData: " + jsonData + " (type: " + typeof(TData) + ")");
            if (typeof(TData) == typeof(List<int>)) {
                // Print each element of the list
                List<int> list = (List<int>)Convert.ChangeType(data, typeof(List<int>));
            }
            byteData = Encoding.ASCII.GetBytes(jsonData);
        } else {
            byteData = (byte[])Convert.ChangeType(data, typeof(byte[]));
        }
        

        // Create the file in the path if it doesn't exist
        if (!Directory.Exists(Path.GetDirectoryName(filePath))) {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        // Attempt to save data
        try {
            File.WriteAllBytes(filePath, byteData);
            Debug.Log("Save data to: " + filePath);

            if (GameManager.Instance != null) {
                GameManager.Instance.LogTxt("Save data to: " + filePath);
            }
        } catch (Exception e) {
            Debug.LogError("Failed to save data to: " + filePath);
            Debug.LogError("Error " + e.Message);
        }
    }

    // Save the given files (with associated filenames) on a new thread
    private static void SaveFilesThreaded(Dictionary<string, byte[]> files, string folder="quests", string root=null) {
        new Thread(() => {
            foreach (var file in files) {
                Save(file.Value, file.Key, folder, root != null ? root : DatabaseManager.Instance.dataPath);
            }

            // Set the flag to indicate that the files have been saved
            filesSaved_Threaded = true;
        }).Start();
        }

    /*
     *************************************************************************
     * Utility functions for loading files from disk
     *************************************************************************
     */
    
    // Load the data from a file in the data folder and return it as the specified type
    public static TData Load<TData>(string fileName, string folder="", string root=null) {
        string filePath = GetFilePath(fileName, folder, root);

        // Return default if the requested file does not exist
        if (!Directory.Exists(Path.GetDirectoryName(filePath))) {
            Debug.LogWarning("File or path does not exist! " + filePath);
            return default(TData);
        }

        // Load in the save data as byte array
        byte[] jsonDataAsBytes = null;

        try {
            jsonDataAsBytes = File.ReadAllBytes(filePath);
            Debug.Log("Loaded all data from: " + filePath);
        } catch (Exception e) {
            Debug.LogError("Failed to load data from: " + filePath);
            Debug.LogError("Error: " + e.Message);
            return default(TData);
        }

        if (jsonDataAsBytes == null)
            return default(TData);

        // If the requested datatype is a byte array, no need for processing
        if (typeof(TData) == typeof(byte[])) {
            return (TData)Convert.ChangeType(jsonDataAsBytes, typeof(TData));
        }

        // Convert the byte array to json
        string jsonData;
        jsonData = Encoding.ASCII.GetString(jsonDataAsBytes);

        Debug.LogWarning("JsonData: " + jsonData);

        // Convert to the specified object type
        TData returnedData = JsonUtility.FromJson<TData>(jsonData);

        // return the casted json object to use
        return (TData)Convert.ChangeType(returnedData, typeof(TData));
    }

    // Load the bytes of a file from the Resources folder
    // In order to be loaded correctly:
    //  - The fileName argument must NOT have an extension, and
    //  - The file in /Resources should have a .bytes extension
    public static byte[] LoadBytesFromResources(string fileName, string folder="") {
        string fullPath = Path.Combine(folder, fileName);
        TextAsset textAsset = Resources.Load(fullPath) as TextAsset;
        if (textAsset == null) {
            Debug.LogWarning("Failed to load file from Resources: " + fullPath);
            return null;
        }
        Debug.LogWarning("Loaded file from Resources: " + fullPath);
        return textAsset.bytes;
    }

    public static async Task<AudioClip> LoadBackgroundMusicAsAudioClip() {
        AudioClip clip = null;

        string path = FileUtils.GetFilePath("background_music.mp3", "music");

        // Check if file exists
        if (!File.Exists(path)) {
            Debug.LogWarning("File does not exist: " + path);
            return null;
        }

        // Fetch the file locally and return it as an AudioClip
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(
            "file://" + path, AudioType.MPEG)) {
            uwr.SendWebRequest();

            while (!uwr.isDone) await Task.Delay(1);

            if (uwr.isNetworkError || uwr.isHttpError) {
                Debug.LogWarning($"{uwr.error}");
            } else {
                clip = DownloadHandlerAudioClip.GetContent(uwr);
            }
        }
        return clip;
    }
}
