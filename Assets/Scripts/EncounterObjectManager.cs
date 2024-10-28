using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EncounterObjectManager : MonoBehaviour
{
    private static EncounterObjectManager selfReference;
    private int COL_DETECT_POINTS_NUM = 4;
    private float MAX_HEIGHT_FOR_COL = 0;
    private int ROW_DETECT_POINTS_NUM = 3;

    [SerializeField] private GameObject _xrOrigin;
    private Depth_ScreenToWorldPosition _depth_ScreenToWorldPosition;

    // Encounter spawning
    [SerializeField] private GameObject _randomEncounterPrefab;
    [SerializeField] private GameObject _bossEncounterPrefab;
    private Queue<(string, bool)> _encountersToSpawn = new();
    private Dictionary<string, (GameObject, GameObject)> _encountersSpawnedStatus = new();

    private bool _inARMode = false;
    private bool _interactionEnabled = true;

    // Semantics check for ground
    [SerializeField] private GameObject _segmentationManager;
    private SemanticQuerying _semanticQuerying;
    private List<(int, int)> _detectGroundPoints;
    private float MIN_DEPTH_FOR_SPAWN = 2.5f;
    private float MAX_DEPTH_FOR_SPAWN = 4f;
    private float SPAWN_Y_OFFSET = 3.5f;
    private int CHECK_GROUND_INTERVAL = 100;
    private int _checkGroundCounter = 0;

    public static EncounterObjectManager Instance {
        get {
            if (selfReference == null) {
                selfReference = new EncounterObjectManager();
            }
            return selfReference;
        }
    }

    void Awake() {
        selfReference = this;
    }

    void Start() {
        _semanticQuerying = _segmentationManager.GetComponent<SemanticQuerying>();
        _depth_ScreenToWorldPosition = _xrOrigin.GetComponent<Depth_ScreenToWorldPosition>();

        // Preload the points in the grid to detect for ground
        LoadDetectGroundPoints();
    }

    private bool _readyToSpawnEncounter = false;
    private GameObject _toSpawnEncounterPin;
    // private EncounterType _toSpawnEncounterType;

    void Update() {
        if (!_inARMode) {
            return;
        }
        
        if (Input.touches.Length > 0 && _interactionEnabled) {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out hit)) {
                    if (hit.transform.gameObject.CompareTag("AREncounterSpawn")) {
                        // Encounter spawn clicked, enter encounter lobby
                        string encounterId = hit.transform.gameObject.GetComponent<AREncounterSpawn>().encounterId;
                        GameObject pin = _encountersSpawnedStatus[encounterId].Item1;
                        _depth_ScreenToWorldPosition.DisableARInteraction();
                        pin.GetComponent<EncounterSpawnManager>().EncounterOnClick();
                    }
                }
            }
        }
        
        if (_checkGroundCounter >= CHECK_GROUND_INTERVAL) {
            if (_readyToSpawnEncounter) {
                if (TrySpawnEncounter()) {
                    // Successfully spawned an encounter on ground
                    _readyToSpawnEncounter = false;
                }
            } else {
                // Check if there is an encounter to spawn
                if (TryGetEncounterToSpawn() is GameObject pin) {
                    _toSpawnEncounterPin = pin;
                    _readyToSpawnEncounter = true;
                }
            }
            // Reset counter
            _checkGroundCounter = 0;
        } else {
            _checkGroundCounter += 1;
        }
    }

    private GameObject TryGetEncounterToSpawn() {
        if (_encountersToSpawn.TryPeek(out (string encounterId, bool toSpawn) encounter)) {
            if (encounter.toSpawn) {
                // Encounter is signaled to spawn
                return _encountersSpawnedStatus[encounter.encounterId].Item1;
            }

            // TODO: Encounter is signaled to despawn
        }
        return null;
    }
    private bool TrySpawnEncounter() {
        List<(int, int)> detectedGroundPoints = new();
        foreach ((int x, int y) point in _detectGroundPoints) {
            if (_semanticQuerying.GetPositionChannel(point.x, point.y) == "ground") {
                if (_depth_ScreenToWorldPosition.GetDepthOfPoint(point.x, point.y) >= MIN_DEPTH_FOR_SPAWN) {
                    detectedGroundPoints.Add(point);
                }
            }
        }

        if (detectedGroundPoints.Count > 0) {
            (int x, int y) = SelectRandomPoint(detectedGroundPoints);
            (Vector3 worldPosition, var depth) = _depth_ScreenToWorldPosition.TranslateScreenToWorldPoint(x, y, MAX_DEPTH_FOR_SPAWN);
            worldPosition.y += SPAWN_Y_OFFSET * (depth / MAX_DEPTH_FOR_SPAWN);
            GameObject newSpawn = Instantiate(
                _toSpawnEncounterPin.GetComponent<SpriteButtonLocationBounded>().encounterType == EncounterType.RANDOM_ENCOUNTER ? _randomEncounterPrefab : _bossEncounterPrefab, 
                worldPosition, Quaternion.identity);
            newSpawn.GetComponentInChildren<AREncounterSpawn>().encounterId = _toSpawnEncounterPin.GetComponent<SpriteButtonLocationBounded>().encounterId;
            _encountersSpawnedStatus[_toSpawnEncounterPin.GetComponent<SpriteButtonLocationBounded>().encounterId] = (_toSpawnEncounterPin, newSpawn);
            _encountersToSpawn.Dequeue();
            return true;
        }

        return false;
    }

    public void RemoveEncounterToSpawnFromQueue(string encounterId) {
        List<(string, bool)> newLs = _encountersToSpawn.ToList();
        newLs.RemoveAll(x => x.Item1 == encounterId);
        _encountersToSpawn = new Queue<(string, bool)>(newLs);
    }


    // Select a random point from a list of points
    private (int, int) SelectRandomPoint(List<(int, int)> points) {
        int index = UnityEngine.Random.Range(0, points.Count);
        return points[index];
    }

    // Get all the points in the grid to detect for ground
    private void LoadDetectGroundPoints() {
        List<(int, int)> detectPoints = new List<(int, int)>();

        for (int i = 1; i <= ROW_DETECT_POINTS_NUM; i++) {
            for (int j = 1; j <= COL_DETECT_POINTS_NUM; j++) {
                detectPoints.Add((Screen.width / (ROW_DETECT_POINTS_NUM + 1) * i, (int) (Screen.height * MAX_HEIGHT_FOR_COL) / (COL_DETECT_POINTS_NUM + 1) * j + (int) (Screen.height * MAX_HEIGHT_FOR_COL)));
                // GameManager.Instance.LogTxt($"Detect point added: {Screen.width / (ROW_DETECT_POINTS_NUM + 1) * i}, {Screen.height / (COL_DETECT_POINTS_NUM + 1) * j}");
            }
        }
        _detectGroundPoints = detectPoints;
    }

    // Add an encounter to the list of encounters to spawn
    public void AddEncounterToSpawn(string encounterId, GameObject pin) {
        _encountersSpawnedStatus[encounterId] = (pin, null);
        _encountersToSpawn.Enqueue((encounterId, true));
    }

    public void RemoveEncounterSpawn(string encounterId) {
        Destroy(_encountersSpawnedStatus[encounterId].Item2);
        _encountersSpawnedStatus[encounterId] = (_encountersSpawnedStatus[encounterId].Item1, null);
    }

    // Set AR mode
    public void SetARMode(bool inARMode) {
        _inARMode = inARMode;
    }

    public void DisableSpawnInteraction() {
        _interactionEnabled = false;
    }

    public void EnableSpawnInteraction() {
        _interactionEnabled = true;
    }
}
