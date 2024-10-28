using System;
using System.Collections.Generic;
using UnityEngine;

public class ARObjectSpawner : MonoBehaviour {
    [SerializeField]
    private GameObject arObjectPrefab;

    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private bool gravity = true;

    private Dictionary<GameObject, float> trackObjects = new();

    private const float threshold = -100;
    private const float LIFE_TIME = 45;

    void Start() {
        if (arObjectPrefab == null) {
            Debug.LogError("ARObjectSpawner: arObjectPrefab is not set.");
        }
        if (arCamera == null) {
            Debug.LogError("ARObjectSpawner: arCamera is not set.");
        }
    }

    void Update() {
        // #if UNITY_EDITOR
        // if(Input.GetMouseButtonDown(0))
        // #else
        // if (Input.touchCount > 0)
        // #endif
        // {
        //     var obj = SpawnARObject();
        //     Debug.Log("Spawned AR object: " + obj);
        // }

        // Track if the object has settled on the ground
        if (!gravity)
            return;

        foreach (var obj in trackObjects) {
            if (obj.Key.transform.position.y < threshold) {
                obj.Key.transform.position = GetPosition(2.0f);

                // Reset the velocity and angular velocity
                if (obj.Key.TryGetComponent<Rigidbody>(out var rb)) {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            // Delete the obj after some time.
            if (obj.Value > 0) {
                trackObjects[obj.Key] -= Time.deltaTime;
            } else {
                trackObjects.Remove(obj.Key);
                Destroy(obj.Key);
            }
        }
    }

    public GameObject SpawnARObject(float distance = 2.0f) {
        //spawn in front of at the camera with some random degree deviation.
        var obj = Instantiate(arObjectPrefab, GetPosition(distance), Quaternion.identity);

        // Track if the object has settled on the ground
        if (!gravity)
            return obj;
        
        trackObjects.Add(obj, LIFE_TIME);
        return obj;
    }

    public void DestroyedObject(GameObject obj) {
        trackObjects.Remove(obj);
    }

    public void AddTrackedObject(GameObject obj, float time = LIFE_TIME) {
        trackObjects.Add(obj, time);
    }

    private Vector3 GetPosition(float distance) {
        var pos = arCamera.transform.position;
        var forw = arCamera.transform.forward.normalized;
        return pos + (forw * distance);
    }
}
