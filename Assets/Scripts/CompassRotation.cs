using UnityEngine;

public class CompassRotation : MonoBehaviour {
    [SerializeField]
    private Vector3 direction = new (0, 1, 0);

    private float _headingVelocity = 0;
    private float rotationToNorth = 0;
    void Start() {
        // Check if location services are available
        if (!Input.location.isEnabledByUser)
            throw new System.Exception("Location services are not enabled");
        
        // Enable the compass
        if (!Input.compass.enabled)
            Input.compass.enabled = true;
    }

    void Update() {
        rotationToNorth = Mathf.SmoothDampAngle(rotationToNorth, Input.compass.trueHeading, ref _headingVelocity, 0.1f);
        Vector3 rotation = -direction * rotationToNorth;
        transform.rotation = Quaternion.Euler(rotation);
    }
}
