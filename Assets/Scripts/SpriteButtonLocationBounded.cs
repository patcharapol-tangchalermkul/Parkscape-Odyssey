using System;
using UnityEngine;

public enum PinState {
    NEAR,
    FAR,
    UNKNOWN
}

public class SpriteButtonLocationBounded : SpriteButton {
    // The location of the sprite on the map. The location must be constant.
    private double latitude;
    private double longitude;
    private MapManager mapManager;
    [SerializeField]
    public System.Action onFound;
    public string encounterId;
    public EncounterType encounterType;

    // State
    [SerializeField]
    private bool known;
    public PinState PinState;

    // Time
    [SerializeField]
    private float repeat = 1;
    private float frameTime = 0;
    // fog
    [SerializeField]
    private bool isFog;

    private EncounterObjectManager encounterObjectManager;

    public SpriteButtonLocationBounded() : base() {}

    new void Start() {
        mapManager = MapManager.Instance;
        if (mapManager == null) {
            throw new Exception("MapManager has not been instantiated.");
        }

        base.Start();
        if (!isFog) {
            if (known) {
                PinState = PinState.FAR;
            } else {
                PinState = PinState.UNKNOWN;
                spriteRenderer.enabled = false;
            }

        
            // By default, the button is disabled.
            SetDisabled(true);

            encounterObjectManager = GameObject.Find("AREncounterSpawnManager").GetComponent<EncounterObjectManager>();
        }
        
    }

    new void Update() {
        if (isFog) {
            if (mapManager.WithinDistanceToPlayer(latitude, longitude, mapManager.fogOfWarSize / 2)) {
                spriteRenderer.enabled = false;
            }
            return;
        }
        base.Update();

        // Check distance to player, only once every repeat seconds
        frameTime += Time.deltaTime;
        if (frameTime < repeat) return;
        frameTime = 0;

        if (mapManager.WithinDistanceToPlayer(latitude, longitude)) {
            if (PinState == PinState.UNKNOWN) {
                spriteRenderer.enabled = true;
                if (onFound != null) {
                    onFound();
                    Debug.Log("Found encounters : " + GameState.Instance.foundMediumEncounters.Count);
                }
                encounterObjectManager.AddEncounterToSpawn(encounterId, gameObject);
            }

            PinState = PinState.NEAR;
            SetDisabled(false);
        } else if (PinState == PinState.NEAR) {
            PinState = PinState.FAR;
            SetDisabled(true);
        }
    }

    public void SetLocation(double latitude, double longitude) {
        this.latitude = latitude;
        this.longitude = longitude;
    }

    public void RemoveAREncounterSpawn() {
        encounterObjectManager.RemoveEncounterSpawn(encounterId);
    }
}
