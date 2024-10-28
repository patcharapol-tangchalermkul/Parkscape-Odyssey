using UnityEngine;

public class AutoRotater : MonoBehaviour
{
    [SerializeField]
    private float speed = 10f;

    [SerializeField]
    private Vector3 direction = Vector3.up;

    // Update is called once per frame
    void Update() {
        gameObject.transform.Rotate(direction, speed * Time.deltaTime);
    }
}
