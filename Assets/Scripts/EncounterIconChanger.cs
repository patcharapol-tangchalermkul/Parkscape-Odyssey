using UnityEngine;

public class EncounterIconChanger : MonoBehaviour {
    [SerializeField]
    private Sprite randomEncounterSprite;
    
    [SerializeField]
    private Sprite randomEncounterDeadSprite;

    [SerializeField]
    private Sprite mediumBossSprite;

    [SerializeField]
    private Sprite mediumBossDeadSprite;

    // To do: add final boss.

    private EncounterType encounterType = EncounterType.RANDOM_ENCOUNTER;
    private SpriteRenderer spriteRenderer;
    private SpriteButtonLocationBounded spriteButtonLocationBounded;

    void Awake() {
        if (TryGetComponent(out SpriteRenderer renderer)) {
            spriteRenderer = renderer;
        } else {
            throw new System.Exception("EncounterIconChanger requires a SpriteRenderer component on the object.");
        }

        if (TryGetComponent(out SpriteButtonLocationBounded spriteButton)) {
            spriteButtonLocationBounded = spriteButton;
        } else {
            throw new System.Exception("EncounterIconChanger requires a SpriteButtonLocationBounded component on the object.");
        }
    }

    public void SetEncounterType(EncounterType type) {
        encounterType = type;
        spriteRenderer.sprite = encounterType switch {
            EncounterType.RANDOM_ENCOUNTER => randomEncounterSprite,
            EncounterType.MEDIUM_BOSS => mediumBossSprite,
            _ => throw new System.Exception("Not yet implemented."),
        };
    }

    public void KillSprite() {
        // Ensure that sprite is rendered defaultly.
        spriteRenderer.color = Color.white;
        spriteRenderer.enabled = true;
        spriteRenderer.sprite = encounterType switch {
            EncounterType.RANDOM_ENCOUNTER => randomEncounterDeadSprite,
            EncounterType.MEDIUM_BOSS => mediumBossDeadSprite,
            _ => throw new System.Exception("Not yet implemented."),
        };

        // Remove AR encounter spawn.
        spriteButtonLocationBounded.RemoveAREncounterSpawn();

        // Enable AR interaction.
        GameObject[] gameObjects;
        gameObjects = GameObject.FindGameObjectsWithTag("xrOrigin");
        if (gameObjects.Length == 0) {
            Debug.Log("No xrOrigin found");
        } else {
            GameObject xrOrigin = gameObjects[0];
            if (xrOrigin.activeInHierarchy) {
                xrOrigin.GetComponent<Depth_ScreenToWorldPosition>().EnableARInteraction();
            }
        }

        // Disabled sprite button.
        spriteButtonLocationBounded.enabled = false;
    }
}
