using System;
using Niantic.Lightship.AR.Utilities;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class Depth_ScreenToWorldPosition : MonoBehaviour
{
    public AROcclusionManager _occMan;
    public Camera _camera;
    public GameObject _prefabToSpawn;

    [SerializeField]
    private GameObject segmentationManager;
    private SemanticQuerying semanticQuerying;

    [SerializeField]
    private GameObject gameManagerObj;
    private GameManager gameManager;

    private bool arInteractionEnabled = true;

    // FISHING
    [SerializeField]
    private GameObject overlayCamera;

    [SerializeField]
    private GameObject fishingRod;
    private LineRenderer fishingRodLine;
    private Vector3? fishingAnchorPosition = null;

    [SerializeField]
    private GameObject arEncounterSpawnManager;
    private EncounterObjectManager encounterObjectManager;

    [SerializeField]
    private GameObject questRewardHandlerObj;
    private ARQuestRewardHandler questRewardHandler;

    [SerializeField]
    private GameObject waterRippleEffect;
    private bool fishingInCooldown = false;
    private bool isFishing = false;
    private float FISHING_REWARD_MIN_TIME = 10;  // Seconds
    private float FISHING_REWARD_MAX_TIME = 20; // Seconds
    private DateTime fishingStartTime;
    private DateTime fishingRewardTime;
    private bool initializedFishingReward = false;
    private float FISHING_CLICK_BOUND = 300;
    private float FISHING_COOLDOWN = 2; // Seconds
    private DateTime FishingRefreshTime;
    private const float REWARD_CHANCE = 0.3f;

    void Start() 
    {
        semanticQuerying = segmentationManager.GetComponent<SemanticQuerying>();
        gameManager = gameManagerObj.GetComponent<GameManager>();
        questRewardHandler = questRewardHandlerObj.GetComponent<ARQuestRewardHandler>();
        encounterObjectManager = arEncounterSpawnManager.GetComponent<EncounterObjectManager>();

        fishingRodLine = overlayCamera.GetComponent<LineRenderer>();
        fishingRodLine.positionCount = 2;
    }

    XRCpuImage? depthimage;
    void Update()
    {

        if (_occMan.subsystem != null) {
            if (!_occMan.subsystem.running)
            {
                return;
            }
        }

        Matrix4x4 displayMat = Matrix4x4.identity;

        if (_occMan.TryAcquireEnvironmentDepthCpuImage(out var image))
        {
            depthimage?.Dispose();
            depthimage = image;
        }
        else
        {
            return;
        }


        // Render fishing rod
        if (fishingAnchorPosition != null && isFishing) {
            // Sample eye depth
            var uvt = new Vector2(1 / 2, 1 / 2);
            var eyeDeptht = depthimage.Value.Sample<float>(uvt, displayMat);

            var centerWorldPosition =
                _camera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, eyeDeptht));

            fishingRodLine.SetPosition(0, fishingAnchorPosition.Value);
            fishingRodLine.SetPosition(1, centerWorldPosition);
        }

        if (isFishing && DateTime.Now > fishingRewardTime && !initializedFishingReward) {
            ShowHasFishingReward();
        }

        if (!arInteractionEnabled) 
        {
            return;
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            var screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#else
        if(Input.touches.Length>0)
        {
            var screenPosition = Input.GetTouch(0).position;
#endif
            if (depthimage.HasValue)
            {
                // Check if fishing is in cooldown
                CheckFishingCooldown();

                if (initializedFishingReward && fishingAnchorPosition != null && isFishing) {
                    // Sample eye depth
                    var anchorScreenPosition = _camera.WorldToScreenPoint(fishingAnchorPosition.Value);
                    CheckFishingRewardRetrieval(anchorScreenPosition, screenPosition);
                } else {
                    // Get semantics of screen position touched
                    string channelName = semanticQuerying.GetPositionChannel((int) screenPosition.x, (int) screenPosition.y);
                    if (channelName == "water") {
                        if (!isFishing && !fishingInCooldown) {
                            StartFishing(screenPosition, displayMat);
                        }
                    } else {
                        StopFishing();
                    }
                }
            }
        }
    }

    private void StartFishing(Vector2 screenPosition, Matrix4x4 displayMat) {
        ShowFishingLayer();

        // Sample eye depth
        var uv = new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
        var eyeDepth = depthimage.Value.Sample<float>(uv, displayMat);

        // Get world position
        var worldPosition =
            _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, eyeDepth));
        fishingAnchorPosition = worldPosition;

        waterRippleEffect.transform.position = worldPosition;
        fishingStartTime = DateTime.Now;
        float randomTime = UnityEngine.Random.Range(FISHING_REWARD_MIN_TIME, FISHING_REWARD_MAX_TIME);
        fishingRewardTime = fishingStartTime.AddSeconds(randomTime);
        isFishing = true;
    }

    private void StopFishing() {
        CloseFishingLayer();
        CloseFishingWaterRipple();
        isFishing = false;
        initializedFishingReward = false;
    }

    private void ShowHasFishingReward() {
        ShowFishingWaterRipple();
        waterRippleEffect.GetComponent<AudioSource>().Play();
        initializedFishingReward = true;
    }

    private void CheckFishingRewardRetrieval(Vector2 anchorScreenPosition, Vector2 clickedPosition) {
        if (clickedPosition.x >= 0 && clickedPosition.x <= Screen.width && clickedPosition.y >= 0 && clickedPosition.y <= Screen.height) {
            if (clickedPosition.x <= Mathf.Min(Screen.width, anchorScreenPosition.x + FISHING_CLICK_BOUND)
                    && clickedPosition.x >= Mathf.Max(0, anchorScreenPosition.x - FISHING_CLICK_BOUND)
                    && clickedPosition.y <= Mathf.Min(Screen.height, anchorScreenPosition.y + FISHING_CLICK_BOUND)
                    && clickedPosition.y >= Mathf.Max(0, anchorScreenPosition.y - FISHING_CLICK_BOUND)) {
                gameManager.LogTxt("Fishing reward clicked, getting reward...");
                if (UnityEngine.Random.value <= REWARD_CHANCE)
                    questRewardHandler.TriggerReward(waterRippleEffect.transform.position, true);
                else {
                    if (UnityEngine.Random.value <= 0.5)
                        questRewardHandler.TriggerTrash(waterRippleEffect.transform.position);
                    else
                        questRewardHandler.TriggerFish(waterRippleEffect.transform.position);
                }
                StartFishingCooldown();
                StopFishing();
            }
        }
    }

    // Start fishing cooldown
    private void StartFishingCooldown() {
        FishingRefreshTime = DateTime.Now.AddSeconds(FISHING_COOLDOWN);
        fishingInCooldown = true;
    }

    // Check if fishing is in cooldown
    private void CheckFishingCooldown() {
        if (fishingInCooldown && DateTime.Now > FishingRefreshTime) {
            fishingInCooldown = false;
        }
    }

    // Show fishing layer
    private void ShowFishingLayer() {
        overlayCamera.SetActive(true);
    }

    // Close fishing layer
    private void CloseFishingLayer() {
        overlayCamera.SetActive(false);
        CloseFishingWaterRipple();
    }

    // Show fishing water ripple effect
    private void ShowFishingWaterRipple() {
        waterRippleEffect.SetActive(true);
    }

    // Close fishing water ripple effect
    private void CloseFishingWaterRipple() {
        waterRippleEffect.SetActive(false);
    }

    public void DisableARInteraction() {
        arInteractionEnabled = false;
        // uiFullBlocker.SetActive(true);
        encounterObjectManager.DisableSpawnInteraction();
    }

    public void EnableARInteraction() {
        arInteractionEnabled = true;
        // uiFullBlocker.SetActive(false);
        encounterObjectManager.EnableSpawnInteraction();
    }

    public float GetDepthOfPoint(int x, int y) {
        // Sample eye depth
        var uv = new Vector2(x / Screen.width, y / Screen.height);
        return depthimage.Value.Sample<float>(uv, Matrix4x4.identity);
    }

    public (Vector3, float) TranslateScreenToWorldPoint(int x, int y, float maxDepth) {
        // Sample eye depth
        var uv = new Vector2(x / Screen.width, y / Screen.height);
        var eyeDepth = Math.Min(depthimage.Value.Sample<float>(uv, Matrix4x4.identity), maxDepth);

        // GameManager.Instance.LogTxt($"Depth: {eyeDepth}");

        // Get world position
        return (_camera.ScreenToWorldPoint(new Vector3(x, y, eyeDepth)), eyeDepth);
    }
}