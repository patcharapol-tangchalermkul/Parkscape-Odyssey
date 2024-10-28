using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Geospatial;
using System;

public class GPSManager : MonoBehaviour
{
    private static GPSManager instance;
    private LocationInfo location;
    private LocationServiceStatus locationServiceStatus;
    private NetworkUtils network;
    private bool permissionGranted = false;

    private Dictionary<string, LatLon> playerLocations = new();
    private Dictionary<string, float> playerLocationTimes = new();
    private const float staleTime = 10f;

    public static GPSManager Instance { 
        get {
            if (instance == null) {
                // To make sure that script is persistent across scenes
                GameObject go = new("GPSManager");
                instance = go.AddComponent<GPSManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    
    }

    void Awake() {
        // Start GPS location service
        StartCoroutine(InitialiseAndUpdateGPS());
        network = NetworkManager.Instance.NetworkUtils;
    }
    private void Start() {

    }

    private void Update()
    {
        // TODO: Update GPS data
    }

    IEnumerator InitialiseAndUpdateGPS() {
        yield return StartCoroutine(InitialiseGPS());
        yield return StartCoroutine(GPSLoc());
    }

    IEnumerator InitialiseGPS() {
        #if UNITY_EDITOR
            // No permission handling needed in Editor
        #elif UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation)) {
                Debug.Log("Requesting Fine Location Permission");
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
            }

            // Wait for the user to interact with the permission dialog
            while (!permissionGranted)
            {
                // Check the permission status
                permissionGranted = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation);
                Debug.Log("Permission Granted: " + permissionGranted);

                // Yielding allows other parts of the program to execute while waiting for user interaction
                yield return null;
            }

            // Check if location service is initializing
            if (UnityEngine.Input.location.status == LocationServiceStatus.Initializing)
            {
                // Wait for initialization to complete
                Debug.Log("Waiting for location service to initialize");
                yield return new WaitUntil(() => UnityEngine.Input.location.status != LocationServiceStatus.Initializing);
            }

            // First, check if user has location service enabled
            if (!UnityEngine.Input.location.isEnabledByUser) {
                Debug.LogError("Android and Location not enabled");
                Debug.Log("Editor Location Service Status: " + UnityEngine.Input.location.status);
                yield break;
            }

        #elif UNITY_IOS
            if (!UnityEngine.Input.location.isEnabledByUser) {
                Debug.LogError("IOS and Location not enabled");
                yield break;
            }
        #endif
        yield return null;
    }


IEnumerator GPSLoc() {
    while (true) {
        // Start service before querying location
        UnityEngine.Input.location.Start(1, 1);
        Debug.Log("Location Service Status: " + UnityEngine.Input.location.status);
                
        // Wait until service initializes
        int maxWait = 20;
        while (UnityEngine.Input.location.status == LocationServiceStatus.Initializing && maxWait > 0) {
            yield return new WaitForSecondsRealtime(1);
            maxWait--;
        }

        // Editor has a bug which doesn't set the service status to Initializing. So extra wait in Editor.
        #if UNITY_EDITOR
            int editorMaxWait = 30;
            while (UnityEngine.Input.location.status == LocationServiceStatus.Stopped && editorMaxWait > 0) {
                yield return new WaitForSecondsRealtime(1);
                // Debug.Log("Editor Wait: " + editorMaxWait);
                // Debug.Log("Editor Location Service Status: " + UnityEngine.Input.location.status);
                editorMaxWait--;
            }
        #endif

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            Debug.LogError("Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location");
            yield break;
        }
        else
        {
            // Access granted and location value could be retrieved
            UpdateGPSData();
            // Input.location.Stop();
        }
        yield return new WaitForSecondsRealtime(0.3f);
    }
}

    private void UpdateGPSData() {
        if (Input.location.status == LocationServiceStatus.Running) {
            // Access granted and location value could be retrieved
            location = Input.location.lastData;
            // Debug.Log("Location: (" + location.latitude + ", " + location.longitude + ")");
        } else {
            // GPS service stopped
        }
        locationServiceStatus = Input.location.status;
        // Debug.Log("Location Service Status: " + locationServiceStatus); 

    }

    public LatLon GetLocation() {
        return new LatLon(location.latitude, location.longitude);
    }

    public Dictionary<string, LatLon> GetPlayerLocations() {
        return playerLocations;
    }

    private void OnDestroy()
    {
        // Stop location services when the script is destroyed
        Input.location.Stop();
    }

    // Getter functions for GPS location
    public string getLatitude() {
        return location.latitude.ToString();
    }

    public string getLongitude() {
        return location.longitude.ToString();
    }

    public LocationServiceStatus getLocationServiceStatus() {
        return locationServiceStatus;
    }

    // ENCOUNTER SPAWNING
    // Leader gets medium encounter locations from web authoring tool
    public void GetMediumEncounters() {
        if (GameState.Instance.MyPlayer.IsLeader) {
            // TODO: get list from web authoring tool
            // Hardcoded for now
            List<LatLon> encounterLocations = new List<LatLon>
            {
                // new LatLon(51.496451, -0.176775),
                // new LatLon(51.506061, -0.174226)
                new LatLon(51.493553, -0.192372), // Kenway
                new LatLon(51.49891971744029, -0.1794724314649951), // Huxley
                
            };
            // GameState.Instance.mediumEncounterGeoLocations.Add(new LatLon(51.502305, -0.177689));
            // GameState.Instance.mediumEncounterGeoLocations.Add(new LatLon(51.39355, -0.1924046));

            // Debug.Log("Sending encounter info to players in lobby");
            // Send medium encounters to players
            SetMediumEncounterID(encounterLocations);
        }
    }

    public static void SetMediumEncounterID(List<LatLon> locations) {
        Debug.LogWarning("Setting medium encounter IDs");
        foreach (LatLon location in locations) {
            string encounterId = Guid.NewGuid().ToString();
            Debug.LogWarning("Setting encounter ID: " + encounterId + " at location: " + location);
            GameState.Instance.mediumEncounterLocations.Add(encounterId, location);
        }
        Debug.LogWarning("Medium encounters: " + GameState.Instance.mediumEncounterLocations.Count);
    }

    // Geolocation sharing
    public void ShareLocationToLeader() {
        if (GameState.Instance.MyPlayer.IsLeader)
            return;
        Debug.Log("Sending location to leader");
        network.broadcast(new MapMessage(GetLocation()).toJson());
    }

    public void ShareLocationsToPlayers() {
        if (!GameState.Instance.MyPlayer.IsLeader)
            return;
        Debug.Log("Sending locations to players");
        UpdatePlayerLocations(GameState.Instance.MyPlayer.Id, new LatLon(location.latitude, location.longitude));

        // Remove stale locations
        foreach (KeyValuePair<string, float> locationTime in playerLocationTimes) {
            if (Time.time - locationTime.Value > staleTime) {
                playerLocations.Remove(locationTime.Key);
                playerLocationTimes.Remove(locationTime.Key);
            }
        }
        network.broadcast(new MapMessage(playerLocations).toJson());
    }

    public void UpdatePlayerLocations(string playerId, LatLon location) {
        if (playerLocations.ContainsKey(playerId)) {
            playerLocations[playerId] = location;
            playerLocationTimes[playerId] = Time.time;
        } else {
            playerLocations.Add(playerId, location);
            playerLocationTimes.Add(playerId, Time.time);
        }
    }

    public void UpdatePlayerLocations(Dictionary<string, LatLon> locations) {
        foreach (KeyValuePair<string, LatLon> location in locations) {
            UpdatePlayerLocations(location.Key, location.Value);
        }
        // Check if some locations are not included. If so, send them away.
        foreach (var entry in playerLocations) {
            if (entry.Key != GameState.Instance.MyPlayer.Id && !locations.ContainsKey(entry.Key)) {
                playerLocations[entry.Key] = new LatLon(0, 0);
            }
        }
    }

    public LatLon GetPlayerLocation(string playerId) {
        if (playerLocations.ContainsKey(playerId)) {
            return playerLocations[playerId];
        }
        return new LatLon(0, 0);
    }
}
