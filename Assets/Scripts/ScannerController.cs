using UnityEngine;
using System.Collections;

public class ScannerController : MonoBehaviour
{
    public float speed = 200f; // Adjust the speed of the scanner line
    private RectTransform rectTransform;
    
    float x_pos = 0;
    float starting_y = -1169.4f;
    float width = 1.3059f;
    float height = 0.398f;
    private float startTime;
    public bool ScannerComplete = false;
    private Quest successQuest;
    private bool ready = false;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        StartCoroutine(AnimateScanner());
    }

    private IEnumerator AnimateScanner()
    {
        startTime = Time.time;
        rectTransform.sizeDelta = new Vector2(width, height);
        float screenHeight = 2800;
        while (!ScannerComplete)
        {
            // Oscillate the Y position using Mathf.Cos
            float elapsedTime = Time.time - startTime; 
            float displacement = Mathf.Sin(elapsedTime * speed / 2);
            // Debug.Log(displacement);
            float newY = starting_y + Mathf.Sin(displacement) * screenHeight; // Adjust the amplitude (100f) as needed
            rectTransform.anchoredPosition3D = new Vector3(x_pos, newY, 0f);

            // Check if the scanner completes one cycle (up and down)
            if (displacement < 0) // Adjust the threshold as needed
            {
                ScannerComplete = true;
                // Show Photo Results Function
                Debug.Log("Scanner complete");
                while (!ready)
                {
                    Debug.Log("Waiting for ready");
                    yield return null;
                }
                ARManager.Instance.ShowQuestResultPopUp(successQuest);
                ARManager.Instance.TriggerReward(successQuest);
                Destroy(gameObject);
                
            }

            yield return null;
        }
    }

    public void SetSuccessQuest(Quest quest)
    {
        Debug.Log("Setting success quest");
        successQuest = quest;
    }

    public void SetReady()
    {
        Debug.Log("Setting ready");
        this.ready = true;
    }
}
