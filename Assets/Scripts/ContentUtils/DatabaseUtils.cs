using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

using Microsoft.Geospatial;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

using Firebase;
using Firebase.Firestore;
using Firebase.Storage;
using Firebase.Extensions;

/*
 * The database MUST be initialized and its dependencies checked/fixed by DatabaseManager
 * before calling any of the methods defined in this class.
 */

public static class DatabaseUtils {
    private static FirebaseFirestore database = FirebaseFirestore.DefaultInstance;
    private const string storageBucketUrl = "gs://segp-03.appspot.com";

    /*
     **************************************************************************
     * Public methods
     **************************************************************************
     */

    // Get the location quests from the database and return them asynchronously.
    // After the provided timeout, the method will return an empty list
    // as well as cancelling the database fetch.
    public static async Task<List<LocationQuest>> GetLocationQuestsWithTimeout(int timeoutSeconds) {
        var tokenSource = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        // Load any location quests stored on disk first
        LocationQuestStore existingLocationQuests = null;

        if (File.Exists(FileUtils.GetFilePath("locationQuests", "quests"))) {
            // Load the location quests from the file
            Debug.LogWarning("2. Found location quests on disk.");
            existingLocationQuests = FileUtils.Load<LocationQuestStore>("locationQuests", "quests");
            existingLocationQuests.quests.ForEach(quest => quest.SetNotStarted());
        } else {
            Debug.LogWarning("2. No location quests found on disk.");
        }

        try {
            return await CancelAfterAsync(
                GetLocationQuestsAsync,
                timeout,
                tokenSource.Token
            );
        } catch (TimeoutException) {
            Debug.LogWarning("GetLocationQuestsAsync timed out.");
            return existingLocationQuests.quests;
        }
    }

    // Get the file asynchronously and timeout if it takes too long
    public static async Task<byte[]> GetFileWithTimeout(
        string filePath, int timeoutSeconds) {
        var tokenSource = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        try {
            return await CancelAfterAsync(
                (token) => GetFileAsync(filePath, token),
                timeout,
                tokenSource.Token
            );
        } catch (TimeoutException) {
            Debug.LogWarning("GetFileAsync timed out for " + filePath);
            return null;
        }
    }

    // Get the file from Firebase Storage and return it as a byte array
    public static async Task<byte[]> GetFileAsync(
        string filePath, CancellationToken token=default(CancellationToken)) {
        try {
            // Get a reference to the file in Firebase Storage
            StorageReference storageReference = FirebaseStorage
                .DefaultInstance
                .GetReferenceFromUrl(Path.Combine(storageBucketUrl, filePath).Replace('\\','/'));

            Debug.LogWarning("Got storage reference for " + Path.Combine(storageBucketUrl, filePath).Replace('\\','/'));

            // Maximum allowed size is 8MB
            const long maxAllowedSize = 8 * 1024 * 1024;
            
            // Wait for the file download and return the byte array
            byte[] fileContents = await storageReference.GetBytesAsync(maxAllowedSize);

            Debug.LogWarning("Successfully fetched contents for " + Path.Combine(storageBucketUrl, filePath).Replace('\\','/'));
            
            return fileContents;
        } catch (StorageException ex) {
            Debug.LogWarning("Failed to fetch file: " + filePath);
        } catch (OperationCanceledException ex) {
            Debug.LogWarning("Cancelled file fetch: " + filePath);
        }

        return null;
    }

    // Process the location quest updates from the database
    // The caller (GameManager, which inherits from MonoBehaviour) is passed in so that
    // we can start a coroutine to save the new quest files to disk
    public static async Task ProcessLocationQuestsUpdateAsync(
        IEnumerable<DocumentChange> changes, MonoBehaviour caller) {
        // Keep track of the asynchronous tasks we are about to start
        // so we can wait for them all to complete
        var tasks = new List<Task<LocationQuest>>();
        var questsToRemove = new List<string>();

        // Start a Task for each location quest fetch
        // Only try to fetch for those location quests that are not already in GameState
        foreach (DocumentChange locationQuestChange in changes) {
            DocumentSnapshot locationQuestDocument = locationQuestChange.Document;
            
            // If the location quest was removed, we will remove it from GameState
            // Otherwise, fetch the new/updated location quest
            if (locationQuestChange.ChangeType == DocumentChange.Type.Removed) {
                GameManager.Instance.LogTxt("Location quest removed: " + locationQuestDocument.Id);
                Debug.LogWarning("Location quest removed: " + locationQuestDocument.Id);
                questsToRemove.Add(locationQuestDocument.Id);
            } else {
                GameManager.Instance.LogTxt("Location quest added: " + locationQuestDocument.Id);
                Debug.LogWarning("Location quest updated: " + locationQuestDocument.Id);
                tasks.Add(GetLocationQuestAsync(locationQuestChange.Document));
            }
        }

        // Pause this method until all the tasks complete, at which point we have all
        // the required data to apply the update
        foreach (LocationQuest locationQuest in await Task.WhenAll(tasks)) {
            if (locationQuest != null) {
                // Add the location quest to GameState if it is not already there, or
                // update it if it is
                Debug.LogWarning("Update: " + locationQuest.Label);
                GameManager.Instance.LogTxt("Update: " + locationQuest.Label);
                GameState.Instance.UpdateLocationQuest(locationQuest);
            } else {
                Debug.LogWarning("Location quest was null: " + locationQuest.Label);
            }
        }

        // Remove the location quests that were deleted from the database
        foreach (string label in questsToRemove) {
            Debug.LogWarning("Removing location quest: " + label);
            GameState.Instance.RemoveLocationQuest(label);
        }

        // Start a Task for each file fetch and pause until all of them are retrieved
        byte[] locationQuestVectors = await GetFileAsync("locationQuestVectors.bytes");
        byte[] locationQuestGraph = await GetFileAsync("locationQuestGraph.bytes");
        byte[] locationQuestLabels = await GetFileAsync("locationQuestLabels.bytes");

        GameManager.Instance.LogTxt("Fetched all 3 files");

        // Save the location quest files to disk; we will know this operation is complete when
        // LastQuestFileUpdate in PlayerPrefs updates
        FileUtils.ProcessNewQuestFiles(
            locationQuestVectors,
            locationQuestGraph,
            locationQuestLabels);

        // Write the updated locationQuests to disk
        FileUtils.Save(new LocationQuestStore(
            new List<LocationQuest>(GameState.Instance.locationQuests.Values)
        ), "locationQuests", "quests");
    }

    // Process a database music update
    // The listener is simply a trigger for us to fetch from Firebase Cloud Storage
    public static async Task<AudioClip> ProcessMusicUpdateAsync(
        DocumentSnapshot snapshot) {
        // The snapshot contains an "added" field, denoting when the music was added
        // Don't perform any changes if the music is not new (i.e. stale)
        DateTime added = snapshot.GetValue<Firebase.Firestore.Timestamp>("added").ToDateTime();

        if (PlayerPrefs.HasKey("LastMusicUpdate")) {
           // Parse the string into a Timestamp
            string lastUpdateString = PlayerPrefs.GetString("LastMusicUpdate");
            DateTime lastUpdate = DateTime.Parse(lastUpdateString);

            if (added <= lastUpdate) {
                Debug.LogWarning("Music is stale.");
                return null;
            }
        }
        
        Debug.LogWarning("Music is new.");
        PlayerPrefs.SetString("LastMusicUpdate", added.ToString());

        // Download the music file in its orignial form and save it on disk
        byte[] musicFile = await GetFileAsync(GetMusicFilePath());
        FileUtils.Save(musicFile, "background_music.mp3", "music");
        AudioClip clip = await FileUtils.LoadBackgroundMusicAsAudioClip(); 

        return clip;
    }

    public static string GetMusicFilePath() {
        return (Path.Combine(
            "music",
            "background_music.mp3"
        ).Replace('\\','/'));
    }

    /*
     **************************************************************************
     * Private methods
     **************************************************************************
     */

    // Start the given task and cancel it after the given timeout
    // This is the wrapper which should be used for all DB operations,
    // so that we our updates are indeed opportunistic.
    private static async Task<TResult> CancelAfterAsync<TResult> (
        Func<CancellationToken, Task<TResult>> taskToRun,
        TimeSpan timeout, CancellationToken cancellationToken) {
        using (var timeoutCancellation = new CancellationTokenSource())

        // Link the two tokens so that when one is cancelled, the other is too
        using (var combinedCancellation = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token))
        {
            // Start the two tasks
            var originalTask = taskToRun(combinedCancellation.Token);
            var delayTask = Task.Delay(timeout, timeoutCancellation.Token);
            
            // Wait for either to complete
            var completedTask = await Task.WhenAny(originalTask, delayTask);

            // At this point either originalTask or delayTask has completed
            // Cancel the timeout to stop the remaining task
            // (Cancelling does not affect completed tasks)
            timeoutCancellation.Cancel();

            if (completedTask == originalTask) {
                // Original task completed
                return await originalTask;
            }
            else {
                // Timeout
                throw new TimeoutException();
            }
        }
    }

    // Get all the location quests from the database and return them asynchronously
    // The cancellation token is used to cancel the operation if it takes too long
    private static async Task<List<LocationQuest>> GetLocationQuestsAsync(
        CancellationToken token=default(CancellationToken)) {
        try {
            // // Load any location quests stored on disk first
            // LocationQuestStore existingLocationQuests = null;

            // if (File.Exists(FileUtils.GetFilePath("locationQuests", "quests"))) {
            //     // Load the location quests from the file
            //     Debug.LogWarning("2. Found location quests on disk.");
            //     existingLocationQuests = FileUtils.Load<LocationQuestStore>("locationQuests", "quests");
            // } else {
            //     Debug.LogWarning("2. No location quests found on disk.");
            // }

            // Get a reference to the locationQuests collection
            Query locationQuestsQuery = database.Collection("locationQuests");

            // First get the locationQuests collection from the database asynchronously
            // 'await' will return control to the caller while the query is in progress
            QuerySnapshot snapshot = await locationQuestsQuery.GetSnapshotAsync();
            Debug.LogWarning("3. Got location quests from the database.");
            
            // Keep track of the asynchronous tasks we are about to start
            // so we can wait for them all to complete
            var tasks = new List<Task<LocationQuest>>();

            // Start a Task for each location quest fetch
            foreach (DocumentSnapshot locationQuestDocument in snapshot.Documents) {
                Debug.LogWarning("4. Getting location quest from " + locationQuestDocument.Id);
                tasks.Add(GetLocationQuestAsync(locationQuestDocument, token));
            }

            // Create a list to store the results
            List<LocationQuest> locationQuests = new List<LocationQuest>();

            // Pause this method until all the tasks complete, then add the results to the list
            foreach (LocationQuest locationQuest in await Task.WhenAll(tasks)) {
                if (locationQuest != null) {
                    Debug.LogWarning("7. Added location quest: " + locationQuest.Label);
                    locationQuests.Add(locationQuest);
                } else {
                    Debug.LogWarning("7. Location quest was null");
                }
            }

            // If we are here, fetching the location quests was successful
            // Save the fetched location quests to disk if there are any
            // If not, default to using the existing location quests
            if (locationQuests.Count == 0) {
                Debug.LogWarning("8. Using existing location quests.");
                throw new TimeoutException();
            } else {
                Debug.LogWarning("8. Saving location quests to disk.");
                LocationQuestStore newLocationQuests = new LocationQuestStore(locationQuests);
                FileUtils.Save(newLocationQuests, "locationQuests", "quests");
            }

            // Return the list of LocationQuest objects
            return locationQuests;
        } catch (AggregateException ex) {
            foreach (var innerException in ex.InnerExceptions) {
                Debug.LogException(innerException);
            }
        }

        // If we are here, fetching the location quests failed
        // Return the existing location quests on disk if there are any
        if (File.Exists(FileUtils.GetFilePath("locationQuests", "quests"))) {
            Debug.LogWarning("9. Using existing location quests.");
            return FileUtils.Load<LocationQuestStore>("locationQuests", "quests").quests;
        } else {
            Debug.LogWarning("9. No location quests found.");
            return new List<LocationQuest>();
        }
    }

    // Get the reference image path from the location quest document
    // For now, this is defaulted to the first image in the location quest's folder
    // TODO: See XXX for full documentation about the Firebase Storage structure
    private static string GetReferenceImagePath(DocumentSnapshot locationQuestDocument) {
        return (Path.Combine(
            "data",
            locationQuestDocument.Id.Replace(" ", "_") + "/image_0.jpg"
        ).Replace('\\','/'));
    }

    private static async Task<LocationQuest> GetLocationQuestAsync(
        DocumentSnapshot locationQuestDocument,
        CancellationToken token=default(CancellationToken)) {
        
        Debug.LogWarning("5. Getting reference image for " + locationQuestDocument.Id);
        byte[] referenceImage = await GetFileAsync(GetReferenceImagePath(locationQuestDocument));

        if (referenceImage == null) {
            Debug.LogWarning("6. Failed to fetch reference image for " + locationQuestDocument.Id);
            return null;
        }


        Debug.LogWarning("6. Got image from " + locationQuestDocument.Id + "(" + referenceImage + ")");

        // Convert the downloaded byte array to a Texture2D
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(referenceImage);

        Debug.LogWarning("6. " + texture);

        // Save the reference image to persistent data storage, in the referenceImages folder
        FileUtils.Save(referenceImage, locationQuestDocument.Id + ".jpg", "referenceImages");

        // Extract the other fields from the document to construct a LocationQuest object
        Dictionary<string, object> locationQuestData = locationQuestDocument.ToDictionary();

        string label = locationQuestDocument.Id;
        
        GeoPoint geoPoint = (GeoPoint) locationQuestData["location"];
        LatLon location = new LatLon(geoPoint.Latitude, geoPoint.Longitude);
        
        // Create the LocationQuest object to be returned
        return new LocationQuest(QuestType.FIND, label, texture, location);
    }
}
