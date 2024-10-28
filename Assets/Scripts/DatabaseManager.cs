using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

using Microsoft.Geospatial;

using UnityEngine;
using UnityEngine.SceneManagement;

using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Storage;


public class DatabaseManager : MonoBehaviour {
    public bool FirebaseReady { get; private set; } = false;

    public static DatabaseManager Instance { get; private set; }

    public Firebase.FirebaseApp App { get; private set; }

    public FirebaseFirestore Database { get; private set; }

    public string dataPath { get; private set; }

    void Awake() {
        // Make sure we only ever have one instance of this object
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Database");
        dataPath = Path.Combine(Application.persistentDataPath, "data/");

        if (objs.Length > 1) {
            Debug.Log("Found more than one database object - destroying the new one.");
            Destroy(this.gameObject);
        } else {
            Instance = this;
        }
    }

    void Start() {
        Initialize();
        StartCoroutine(CompleteInitialisation());
    }

    private void Initialize() {
        // Initialize Firebase
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available) {
                // Create and hold a reference to the FirebaseApp and FirebaseFirestore
                App = Firebase.FirebaseApp.DefaultInstance;
                Database = FirebaseFirestore.DefaultInstance;
                // Set a flag here to indicate whether Firebase is ready to use by your app.
                FirebaseReady = true;
            } else {
                UnityEngine.Debug.LogError(System.String.Format(
                    "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            } 
        });
    }


    // Load the Main Menu scene when either Firebase is ready
    // or the user has been waiting for more than 5 seconds.
    private IEnumerator CompleteInitialisation() {
        float timeWaited = 0;
        while (!FirebaseReady && timeWaited < 5) {
            timeWaited += Time.deltaTime;
            yield return null;
        }

        if (FirebaseReady) {
            Debug.Log("Firebase is ready! Loading Main Menu.");
        }
        else {
            Debug.Log("Waited 5 seconds for Firebase to be ready. Loading Main Menu.");
        }

        DontDestroyOnLoad(this.gameObject);

        SceneManager.LoadScene("Main Menu");
    }
}


