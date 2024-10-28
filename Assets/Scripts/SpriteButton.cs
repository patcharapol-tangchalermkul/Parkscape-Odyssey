using UnityEngine;
using UnityEngine.Events;

public class SpriteButton : MonoBehaviour {
    public UnityEvent onClick = new();
    public bool Disabled;

    protected SpriteRenderer spriteRenderer;
    protected Color originalColor;

    public Camera cam;

    private Vector2 startedTouchPosition = Vector2.zero;
    private const float touchTolerance = 100;

    protected void Start() {
        if (onClick == null) {
            throw new System.Exception("SpriteButton requires an onClick event to be set.");
        }

        // Check if gameobject has box collider
        if (GetComponent<BoxCollider>() == null && GetComponent<BoxCollider2D>() == null 
            && GetComponent<SphereCollider>() == null && GetComponent<MeshCollider>() == null) {
            throw new System.Exception("SpriteButton requires a Collider component on the object.");
        }

        if (TryGetComponent<SpriteRenderer>(out spriteRenderer))
            originalColor = spriteRenderer.color;

        if (cam == null) {
            cam = Camera.main;
        }
    }

    // Update is called once per frame
    protected void Update() {
        if (Disabled) return;

        #if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) {
            startedTouchPosition = Input.mousePosition;
        }
        #else
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) {
            startedTouchPosition = Input.GetTouch(0).position;
        }
        #endif

        #if UNITY_EDITOR
        if (Input.GetMouseButtonUp(0)) {
        #else
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended && 
            Vector2.Distance(startedTouchPosition, Input.GetTouch(0).position) < touchTolerance) {
        #endif
            GameObject target = GetClickedObject(out RaycastHit hit);
            if (target == gameObject) {
                onClick.Invoke();
            }
        }
    }

    protected void SetDisabled(bool disabled) {
        Disabled = disabled;
        if (disabled) {
            spriteRenderer.color = Color.gray;
        } else {
            spriteRenderer.color = originalColor;
        }
    }

    private GameObject GetClickedObject(out RaycastHit hit) {
        GameObject target = null;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction * 10, out hit)) {
            if (hit.collider != null)
                target = hit.collider.gameObject;
        }
        return target;
    }
}
