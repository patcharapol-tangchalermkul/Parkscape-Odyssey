using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Maps.Unity;
using Microsoft.Geospatial;
using System;
using Newtonsoft.Json;
using System.Linq;

public class MapManager : MonoBehaviour
{
    // Map manager is a singleton class
    public static MapManager selfReference;
    private GPSManager gpsManager;
    private EncounterController encounterController;
    private LatLon location;
    private bool permissionGranted = false;
    public GameObject map;
    private bool mapCenterSet = false;
    private bool follow = true;

    // Map Grids
    private double mapLength = 900;
    private double mapWidth = 2000;
    private double gridLength = 100;
    private double gridWidth = 100;
    private int gridLengthNum;
    private int gridWidthNum;
    private int randomGridChance = 10;
    private List<List<LatLon>> centres = new();

    // Pop ups
    [SerializeField]
    private GameObject outOfRadiusPopup;

    // Player pins
    [SerializeField]
    private GameObject playerPinPrefab;

    [SerializeField]
    private GameObject playerPinObject;
    private MapPin playerPin;

    [SerializeField]
    private GameObject playerRadiusObject;
    private MapPin playerRadiusPin;

    private Dictionary<string, MapPin> otherPlayerPins = new();

    // Network
    private NetworkUtils network;
    private int previousFoundEncounterCount = 0;

    // Map Blocker
    [SerializeField]
    private GameObject mapBlocker;

    // Map Components
    private MapRenderer mapRenderer;
    private MapTouchInteractionHandler mapTouchInteractionHandler;

    // Map Constants
    private const int earthRadius = 6371000;
    private const float granularity = 0.1f;
    private const float minZoomLevel = 16;
    private const float maxZoomLevel = 20;
    private const float defaultZoomLevel = 19;

    // [SerializeField]
    private float interactDistance = 40; // in meters

    // [SerializeField]
    private float maxRadius = 2000; // in meters

    // [SerializeField]
    private double startingLatitude = 51.506061;
    
    // [SerializeField]
    private double startingLongitude = -0.174226;

    // Pin Constants
    private const float minPinScale = 0.04f;
    private const float maxPinScale = 0.20f;
    
    // Fog of War
    [SerializeField]
    private GameObject fogOfWarPrefab;
    public float fogOfWarSize = 100;

    // AR
    public bool ARMode = false;


    public static MapManager Instance {
        get {
            if (selfReference == null) {
                selfReference = new MapManager();
            }
            return selfReference;
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        // Initialisation
        selfReference = this;
        map = gameObject;
        network = NetworkManager.Instance.NetworkUtils;
        gpsManager = GPSManager.Instance;

        Debug.Log("MapManager Awake");

        // Disable full blocker
        mapBlocker.SetActive(false);

        // Get Components
        mapRenderer = GetComponent<MapRenderer>();
        mapTouchInteractionHandler = GetComponent<MapTouchInteractionHandler>();

        playerPin = playerPinObject.GetComponent<MapPin>();
        playerPinObject.GetComponent<SetPlayerPin>().Set(GameState.Instance.MyPlayer.Id);
        playerRadiusPin = playerRadiusObject.GetComponent<MapPin>();

        // Disable any popups
        if (outOfRadiusPopup != null)
            outOfRadiusPopup.SetActive(false);

        // Set the map's zoom level
        mapRenderer.MinimumZoomLevel = minZoomLevel;
        mapRenderer.MaximumZoomLevel = maxZoomLevel;

        // Set Default zoom
        mapRenderer.ZoomLevel = defaultZoomLevel;
    }


    void Start() {
        if (GameState.MAPDEBUGMODE) {
            mapRenderer.Center = new LatLon(startingLatitude, startingLongitude);
            mapCenterSet = true;
        }
        encounterController = EncounterController.selfReference;
        DiscretiseMap();
        PinFogOfWar();
        SpawnRandomEncounters();
        AddMediumEncounterPins();

        foreach (var entry in GameState.Instance.PlayersDetails) {
            if (entry.Key != GameState.Instance.MyPlayer.Id) {
                GameObject otherPin = AddPin(playerPinPrefab, 0, 0, true);
                otherPin.GetComponent<SetPlayerPin>().Set(entry.Key);
                otherPlayerPins.Add(entry.Key, otherPin.GetComponent<MapPin>());
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (GameState.MAPDEBUGMODE) {
            location = mapRenderer.Center;
            // Update player pin location
            if (playerPin != null) {
                playerPin.Location = location;
                playerRadiusPin.Location = location;
            }
            KeepMapWithinRadius();
            return;
        }

        if (gpsManager.getLocationServiceStatus() == LocationServiceStatus.Running) {
            // Get GPS location
            location = gpsManager.GetLocation();

            // Set the map's center to the current location
            if (!mapCenterSet || follow) {
                mapRenderer.Center = location;
                mapCenterSet = true;
            }

            // Update player pin location
            if (playerPin != null) {
                playerPin.Location = location;
                playerRadiusPin.Location = location;
            }

            // Keep map within radius
            if (mapCenterSet)
                KeepMapWithinRadius();
        }

        // Check if map sharing is needed
        if (GameState.Instance.foundMediumEncounters.Count > previousFoundEncounterCount
            || NetworkManager.Instance.ChangeInConnectedPlayers()) {
            // Send map info to other players
            Debug.Log(GameState.Instance.foundMediumEncounters.Count + ", " + previousFoundEncounterCount);
            MapMessage mapMessage = new MapMessage(MapMessageType.FOUND_ENCOUNTERS, GameState.Instance.foundMediumEncounters.ToList(), new Dictionary<string, Dictionary<string, double>>());
            network.broadcast(mapMessage.toJson());
            previousFoundEncounterCount = GameState.Instance.foundMediumEncounters.Count;
        }
        
    }

    public CallbackStatus HandleMessage(Message message) {
        MapMessage mapMessage = (MapMessage) message.messageInfo;
        switch (mapMessage.type) {
            // Receive new found encounters from other players
            case MapMessageType.FOUND_ENCOUNTERS:
                // Add to list of found encounters
                HashSet<string> newFoundEncounters = new HashSet<string>(mapMessage.foundEncounterIds);
                // Add pins for the new found encounters
                foreach (string id in newFoundEncounters) {
                    if (GameState.Instance.foundMediumEncounters.Contains(id))
                        continue;
                    encounterController.CreateMonsterSpawn(id, GameState.Instance.mediumEncounterLocations[id], EncounterType.MEDIUM_BOSS);
                }
                GameState.Instance.foundMediumEncounters.UnionWith(newFoundEncounters);
                break;
            case MapMessageType.LEADER_SHARE_GEOLOCATION:
                // Move pins for the player locations
                Dictionary<string, LatLon> playerLocations = MapMessage.DictToLatLon(mapMessage.playerLocations);
                gpsManager.UpdatePlayerLocations(playerLocations);

                foreach (var entry in gpsManager.GetPlayerLocations()) {
                    if (entry.Key != GameState.Instance.MyPlayer.Id) {
                        otherPlayerPins[entry.Key].Location = entry.Value;
                        double pDistance = GetDistanceToPlayer(otherPlayerPins[entry.Key].Location.LatitudeInDegrees, otherPlayerPins[entry.Key].Location.LongitudeInDegrees);
                        // GameManager.Instance.LogTxt("[GEOLOCATION] Distance to " + entry.Key + " is " + pDistance);
                    }
                }
                break;

            case MapMessageType.MEMBER_SHARE_GEOLOCATION:
                gpsManager.UpdatePlayerLocations(mapMessage.sentFrom, MapMessage.DictToLatLon(mapMessage.myLocation));
                otherPlayerPins[mapMessage.sentFrom].Location = GPSManager.Instance.GetPlayerLocation(mapMessage.sentFrom);
                double distance = GetDistanceToPlayer(otherPlayerPins[mapMessage.sentFrom].Location.LatitudeInDegrees, otherPlayerPins[mapMessage.sentFrom].Location.LongitudeInDegrees);
                // GameManager.Instance.LogTxt("[GEOLOCATION] Distance to " + mapMessage.sentFrom + " is " + distance);
                break;
        }
        return CallbackStatus.PROCESSED;
    }

    private int leaderFreq = 5;
    private int count = 0;
    public void SendMessages() {
        if (GameState.Instance.MyPlayer.IsLeader) {
            if (count < leaderFreq) {
                count++;
            } else {
                count = 0;
                gpsManager.ShareLocationsToPlayers();
            }
        } else {
            gpsManager.ShareLocationToLeader();
        }
    }

    /*** Fog of War ***/
    public void PinFogOfWar() {
        if (fogOfWarSize < 0)
            return;

        float fogRadius = maxRadius + 350;
        for (float dx = -fogRadius; dx < fogRadius; dx += fogOfWarSize) {
            for (float dy = -fogRadius; dy < fogRadius; dy += fogOfWarSize) {
                (double, double) centre = AddMetersToCoordinate(startingLatitude, startingLongitude, dx, dy);
                AddPin(fogOfWarPrefab, centre.Item1, centre.Item2, dontAlter: true);
            }
        }
    }

    /*** Random Encounter Generation ***/
    // Discretise the map into grids
    public void DiscretiseMap() {
        (double, double) bottomLeft = AddMetersToCoordinate(startingLatitude, startingLongitude, - mapLength / 2, - mapWidth / 2);
        gridLengthNum = (int) Math.Ceiling(mapLength / gridLength);
        gridWidthNum = (int) Math.Ceiling(mapWidth / gridWidth);
        for (int i = 0; i < gridLengthNum; i++) {
            List<LatLon> row = new List<LatLon>();
            for (int j = 0; j < gridWidthNum; j++) {
                double latDisplacement = i * gridWidth + gridWidth / 2;
                double longDisplacement = j * gridLength + gridLength / 2;
                (double, double) centre = AddMetersToCoordinate(bottomLeft.Item1, bottomLeft.Item2, latDisplacement, longDisplacement);
                row.Add(new LatLon(centre.Item1, centre.Item2));
            }
            centres.Add(row);
        }
    }

    // Randomly choose grids to spawn encounters
    public void SpawnRandomEncounters() {
        // Get random number of encounters to spawn
        int gridNum = (int) Math.Ceiling(mapLength / gridLength) * (int) Math.Ceiling(mapWidth / gridWidth);
        int lb = (int) gridNum / 4;
        int ub = (int) gridNum / 4 * 3;
        int numEncounters = UnityEngine.Random.Range(lb, ub);
        for (int i = 0; i < gridLengthNum; i++) {
            for (int j = 0; j < gridWidthNum; j++) {
                // Get random grid to spawn encounter
                int spawn = UnityEngine.Random.Range(0, 100);
                if (spawn < randomGridChance) {
                    LatLon centre = centres[i][j];
                    string encounterId = Guid.NewGuid().ToString();
                    // Add pin for the encounter
                    encounterController.CreateMonsterSpawn(encounterId, new LatLon(centre.LatitudeInDegrees, centre.LongitudeInDegrees), EncounterType.RANDOM_ENCOUNTER);
                }
            }
        }

        // FOR DEBUG ONLY
        string testEncounterId = Guid.NewGuid().ToString();
        encounterController.CreateMonsterSpawn(testEncounterId, new LatLon(51.493553, -0.192372), EncounterType.RANDOM_ENCOUNTER);
    }

    /*** Map Pins ***/
    // Add Medium Encounter Pins to the map
    public void AddMediumEncounterPins() {
        foreach (var entry in GameState.Instance.mediumEncounterLocations) {
            Debug.Log("Adding pin for " + entry.Key);
            encounterController.CreateMonsterSpawn(entry.Key, entry.Value, EncounterType.MEDIUM_BOSS);
        }
    }

    // Add Pin to some location
    public GameObject AddPin(GameObject prefab, double latitude = -1, double longitude = -1, bool dontAlter = false) {
        GameObject pin = Instantiate(prefab, map.transform);
        // Add MapPin component to the pin if not already there
        if (!pin.TryGetComponent(out MapPin mapPinComponent)) {
            mapPinComponent = pin.AddComponent<MapPin>();
        }
        mapPinComponent.Location = new LatLon(latitude, longitude);


        // Set pin rotation to face the camera
        if (!dontAlter) {
            // Adjust scaling of the pin
            mapPinComponent.Altitude = 3;
            
            mapPinComponent.ScaleCurve = AnimationCurve.Linear(minZoomLevel, minPinScale, maxZoomLevel, maxPinScale);
            pin.transform.LookAt(Camera.main.transform);
        }

        // Set any other properties of the pin
        if (pin.TryGetComponent(out SpriteButtonLocationBounded spriteButton)) {
            spriteButton.SetLocation(latitude, longitude);
        }
        // Debug.Log("Pin added at " + latitude + ", " + longitude);

        return pin;
    }

    // Called when player clicks on the button.
    public void SnapBack() {
        mapRenderer.Center = location;
        follow = true;
    }

    // Called when user moves the map.
    public void StopFollowing() {
        follow = false;
    }

    private void KeepMapWithinRadius() {
        // Get distance between starting location and map center
        double distance = DistanceBetweenCoordinates(startingLatitude, startingLongitude, 
                            mapRenderer.Center.LatitudeInDegrees, mapRenderer.Center.LongitudeInDegrees);
        if (distance > maxRadius) {
            double farDistance = DistanceBetweenCoordinates(startingLatitude, startingLongitude, 
                            mapRenderer.Center.LatitudeInDegrees, mapRenderer.Center.LongitudeInDegrees);
            
            double dx = startingLatitude - mapRenderer.Center.LatitudeInDegrees;
            double dy = startingLongitude - mapRenderer.Center.LongitudeInDegrees;
            float maxRadiusDecreased = maxRadius - 1;
            double ratio = maxRadiusDecreased / farDistance;

            TriggerOutOfRadius();

            // Move map back to the edge of the radius
            double newX = startingLatitude - dx * ratio;
            double newY = startingLongitude - dy * ratio;
            mapRenderer.Center = new LatLon(newX, newY);
        }
    }

    public void TriggerOutOfRadius() {
        outOfRadiusPopup.SetActive(true);
    }

    public void CloseOutOfRadiusPopup() {
        outOfRadiusPopup.SetActive(false);
    }

    // Add Pin near current location or provided location based on max and min radius, in some direction.
    public GameObject AddPinNearLocation(GameObject prefab, float maxRadius, float minRadius = 0,
                                   double direction = -1, double latitude = -1, double longitude = -1) {
        //  Assert that direction must be between 0 and 360
        if (direction != -1 && (direction < 0 || direction > 360)) {
            throw new ArgumentException("Direction must be between 0 and 360");
        }

        // Assert that maxRadius must be greater than minRadius
        if (maxRadius < minRadius) {
            throw new ArgumentException("maxRadius must be greater than minRadius");
        }

        // Assert that minRadius must be greater than or equal to 0
        if (minRadius < 0) {
            throw new ArgumentException("minRadius must be greater than or equal to 0");
        }

        // Assert that location must have been initialised if latitude and longitude are not provided
        if (latitude == -1 && longitude == -1 && !mapCenterSet) {
            throw new ArgumentException("Location must be initialised or latitude and longitude must be provided");
        }

        // Get random distance within maxRadius that is not within minRadius with a granularity of 0.1 meters
        double randRadius = UnityEngine.Random.Range(minRadius / granularity, maxRadius / granularity) * granularity;

        // Get random direction if not provided
        direction = (direction == -1) ? UnityEngine.Random.Range(0, 360) : direction;

        // Convert direction to radians and get the change in latitude and longitude
        double directionInRadians = direction / 180 * Math.PI;
        double dLon = randRadius * Math.Cos(directionInRadians);
        double dLat = randRadius * Math.Sin(directionInRadians);

        // If latitude and longitude are not provided, use current location
        latitude = (latitude == -1) ? location.LatitudeInDegrees : latitude;
        longitude = (longitude == -1) ? location.LongitudeInDegrees : longitude;

        (double, double) newLocation = AddMetersToCoordinate(latitude, longitude, dLat, dLon);
        return AddPin(prefab, newLocation.Item1, newLocation.Item2);
    }

    public static (double, double) AddMetersToCoordinate(double latitude, double longitude, double dLat, double dLon) {
        double newLatitude  = latitude  + dLat / earthRadius * (180 / Math.PI);
        double newLongitude = longitude + dLon / earthRadius * (180 / Math.PI) / Math.Cos(latitude * Math.PI / 180);
        return (newLatitude, newLongitude);
    }

    // Get distance between two points in metres.
    public static double DistanceBetweenCoordinates(double lat1, double lon1, double lat2, double lon2) {
        double dLat = (lat2 - lat1) * (Math.PI / 180);
        double dLon = (lon2 - lon1) * (Math.PI / 180);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    public double GetDistanceToPlayer(double latitude, double longitude) {
        return DistanceBetweenCoordinates(location.LatitudeInDegrees, location.LongitudeInDegrees, latitude, longitude);
    }

    public bool WithinDistanceToPlayer(double latitude, double longitude, float distance = -1) {
        if (distance == -1)
            distance = interactDistance;
        return GetDistanceToPlayer(latitude, longitude) <= distance;
    }

    /*** Map Interactions ***/
    public void DisableMapInteraction(bool ARLock = false) {
        mapTouchInteractionHandler.enabled = false;
        mapBlocker.SetActive(true);
        if (!ARMode)
            ARMode = ARLock;
    }

    public void EnableMapInteraction(bool ARLock = false) {
        if (ARMode && !ARLock)
            return;
        if (ARMode && ARLock)
            ARMode = false;
        mapTouchInteractionHandler.enabled = true;
        mapBlocker.SetActive(false);

    }
}

public class MapMessage : MessageInfo 
{
    public MapMessageType type {get; set;}
    public MessageType messageType {get; set;}
    public List<string> foundEncounterIds;
    public Dictionary<string, Dictionary<string, double>> mediumEncounterLocations;
    public Dictionary<string, Dictionary<string, double>> playerLocations;
    public Dictionary<string, double> myLocation;
    public string sentFrom;

    [JsonConstructor]
    public MapMessage(MapMessageType type, List<string> foundEncounterIds, Dictionary<string, Dictionary<string, double>> mediumEncounterLocations) {
        this.messageType = MessageType.MAP;
        this.foundEncounterIds = foundEncounterIds;
        this.type = type;
        this.mediumEncounterLocations = mediumEncounterLocations;
    }

    public MapMessage(Dictionary<string, LatLon> playerLocations) {
        this.messageType = MessageType.MAP;
        this.type = MapMessageType.LEADER_SHARE_GEOLOCATION;
        this.playerLocations = LatLonToDict(playerLocations);
    }

    public MapMessage(LatLon myLocation) {
        this.messageType = MessageType.MAP;
        this.type = MapMessageType.MEMBER_SHARE_GEOLOCATION;
        this.sentFrom = GameState.Instance.MyPlayer.Id;
        this.myLocation = LatLonToDict(myLocation);
    }

    public static Dictionary<string, double> LatLonToDict(LatLon latLon) {
        return new Dictionary<string, double> {
            {"latitude", latLon.LatitudeInDegrees},
            {"longitude", latLon.LongitudeInDegrees}
        };
    }

    public static Dictionary<string, Dictionary<string, double>> LatLonToDict(Dictionary<string, LatLon> latLonDict) {
        Dictionary<string, Dictionary<string, double>> dict = new();
        foreach (var entry in latLonDict) {
            dict.Add(entry.Key, LatLonToDict(entry.Value));
        }
        return dict;
    }

    public static LatLon DictToLatLon(Dictionary<string, double> dict) {
        return new LatLon(dict["latitude"], dict["longitude"]);
    }

    public static Dictionary<string, LatLon> DictToLatLon(Dictionary<string, Dictionary<string, double>> dict) {
        Dictionary<string, LatLon> latLonDict = new();
        foreach (var entry in dict) {
            latLonDict.Add(entry.Key, DictToLatLon(entry.Value));
        }
        return latLonDict;
    }
    
    public string toJson() {
        return JsonConvert.SerializeObject(this);
    }

}

public enum MapMessageType {
    FOUND_ENCOUNTERS,
    MAP_INFO,
    LEADER_SHARE_GEOLOCATION,
    MEMBER_SHARE_GEOLOCATION
}
 