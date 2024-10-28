using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum PotionType {
    HEALTH,
    MANA,
}

public class ARQuestRewardHandler : MonoBehaviour
{
    private ARObjectSpawner aRObjectSpawner;

    [SerializeField]
    private GameObject cardsUIManagerPrefab;
    private GameObject cardsUIManagerObj;

    [SerializeField]
    private GameObject cardPrefab;

    [SerializeField]
    private GameObject healthPotionPrefab;

    [SerializeField]
    private GameObject manaPotionPrefab;

    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private List<GameObject> trashPrefabs;

    [SerializeField]
    private GameObject fishPrefab;

    [SerializeField]
    private AudioClip failAudio;

    [SerializeField]
    private AudioClip successAudio;

    [SerializeField]
    private GameObject explodedRewardPrefab;

    void Start() {
        if (TryGetComponent<ARObjectSpawner>(out var aRObjectSpawner)) {
            this.aRObjectSpawner = aRObjectSpawner;
        } else {
            Debug.LogError("ARQuestRewardHandler: ARObjectSpawner not found.");
        }

        cardsUIManagerObj = Instantiate(cardsUIManagerPrefab, transform);
    }

    // Inputs unused for now.
    public void TriggerReward(BasicQuest _basicQuest) {
        SpawnReward((source) => {
            TriggerReward(source.transform.position, true);
        });
    }

    public void TriggerReward(LocationQuest _locationQuest) {
        SpawnReward((source) => {
            TriggerReward(source.transform.position, false);
        });
    }

    public void TriggerReward(Vector3 position, bool cardOrPotion) {
        if (cardOrPotion) {
            Card card = RewardCard();
            GameObject cardObj = Instantiate(cardPrefab, gameObject.transform);
            cardObj.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = card.img;
            StartCoroutine(AnimateObject(cardObj, position));

            // Add card to inventory.
            GameState.Instance.AddCard(card.name);
        } else {
            PotionType type = RewardPotion();
            GameObject potionObj = null;
            switch(type) {
                case PotionType.HEALTH:
                    potionObj = Instantiate(healthPotionPrefab, gameObject.transform);
                    GameState.Instance.MyPlayer.AddHealthPotion();
                    break;
                case PotionType.MANA:
                    potionObj = Instantiate(manaPotionPrefab, gameObject.transform);
                    GameState.Instance.MyPlayer.AddManaPotion();
                    break;
            }

            StartCoroutine(AnimateObject(potionObj, position));
        }
        // Play audio
        PlaySuccessAudio();
    }

    public void TriggerTrash(Vector3 position) {
        int index = UnityEngine.Random.Range(0, trashPrefabs.Count);
        GameObject trash = Instantiate(trashPrefabs[index], gameObject.transform);
        StartCoroutine(AnimateObject(trash, position));
        PlayFailAudio();
    }

    public void TriggerFish(Vector3 position) {
        GameObject fish = Instantiate(fishPrefab, gameObject.transform);
        GameState.Instance.MyPlayer.Heal(2);
        StartCoroutine(AnimateObject(fish, position));
        PlaySuccessAudio();
    }

    // Reward with random card.
    private Card RewardCard() {
        return cardsUIManagerObj.GetComponent<CardsUIManager>().GetRandomCard();
    }

    // Reward with random potion.
    private PotionType RewardPotion() {
        return (PotionType) UnityEngine.Random.Range(0, 2);
    }

    private IEnumerator AnimateObject(GameObject obj, Vector3 location, float duration = 1.5f) {
        // Animate the object.
        float elapsed = 0.0f;
        Vector3 start = location;
        while (elapsed < duration) {
            Vector3 end = arCamera.transform.position + arCamera.transform.forward * 0.6f;
            obj.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            obj.transform.LookAt(arCamera.transform);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // On complete destroy,
        Destroy(obj);
    }

    private void SpawnReward(Action<GameObject> onClick) {
        GameObject obj = aRObjectSpawner.SpawnARObject();
        SpriteButton spriteButton;
        if (!obj.TryGetComponent<SpriteButton>(out spriteButton)) {
            spriteButton = obj.AddComponent<SpriteButton>();
        }
        spriteButton.onClick.AddListener(() => {
            spriteButton.Disabled = true;
            onClick(obj);

            // Explode the reward.
            aRObjectSpawner.DestroyedObject(obj);
            Destroy(obj);
            GameObject explodedReward = Instantiate(explodedRewardPrefab, obj.transform.position, obj.transform.rotation, transform);
            aRObjectSpawner.AddTrackedObject(explodedReward, 10);
            foreach (Transform child in explodedReward.transform) {
                child.GetComponent<Rigidbody>().AddForce(UnityEngine.Random.insideUnitSphere * 10, ForceMode.Impulse);
            }
        });

        spriteButton.cam = arCamera;
    }

    private void PlayFailAudio() {
        GetComponent<AudioSource>().clip = failAudio;
        GetComponent<AudioSource>().Play();
    }

    private void PlaySuccessAudio() {
        GetComponent<AudioSource>().clip = successAudio;
        GetComponent<AudioSource>().Play();
    }
}
