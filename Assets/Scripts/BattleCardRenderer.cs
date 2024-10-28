using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BattleCardRenderer : CardRenderer, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler {
    private RectTransform rectTransform;

    private float canvasScaleFactor;

    private bool selectThisCard = false;
    private BattleUIManager battleUIManager;
    private BattleManager battleManager;

    private float lastTapTime = 0;

    private float doubleTapThreshold = 0.3f;

    
    private void Awake() {
        Canvas canvas = (Canvas) FindObjectOfType(typeof(Canvas));
        canvasScaleFactor = canvas.scaleFactor;
        rectTransform = GetComponent<RectTransform>();
        battleUIManager = (BattleUIManager) FindObjectOfType(typeof(BattleUIManager));
        battleManager = (BattleManager) FindObjectOfType(typeof(BattleManager));
    }

    public void OnBeginDrag(PointerEventData eventData) {
        // Debug.Log("OnBeginDrag");
        // No rotation while moving the card around
        rectTransform.rotation = Quaternion.identity;
    }
    public void OnEndDrag(PointerEventData eventData) {
        // Debug.Log("OnEndDrag");
        if (!selectThisCard) {
            // Snap back to starting position
            StartCoroutine(ResetCardPosition(0.2f));
            return;
        } else {
            // Inform the battle manager that this card is to be played
            battleManager.PlayCard(this.cardIndex);
        }
    }

    public void OnDrag(PointerEventData eventData) {
        // Move the card to follow the pointer every frame
        rectTransform.anchoredPosition += eventData.delta / canvasScaleFactor;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if(other.gameObject.GetComponent<MonsterRenderer>()) {
            // Collision with monster - select this card
            selectThisCard = true;
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if(other.gameObject.GetComponent<MonsterRenderer>()) {
            // No more collision - deselect this card
            selectThisCard = false;
        }
    }

    public IEnumerator ResetCardPosition (float time) {
        Vector3 startingPosition  = rectTransform.position;
        Vector3 startingRotation  = rectTransform.eulerAngles;

        (Vector3 defaultPosition, Quaternion defaultRotation) =
            battleUIManager.getCardPositionAtIndex(this.cardIndex);

        float elapsedTime = 0;
        
        while (elapsedTime < time) {
            rectTransform.position = Vector3.Lerp(startingPosition, defaultPosition, (elapsedTime / time));
            rectTransform.rotation = Quaternion.Lerp(
                Quaternion.Euler(startingRotation.x, startingRotation.y, startingRotation.z),
                defaultRotation,
                (elapsedTime / time)
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Snap back to starting position in case anything went wrong
        rectTransform.position = defaultPosition;
        rectTransform.rotation = defaultRotation; 
    }

    public void OnPointerClick(PointerEventData eventData){
        float currentTimeClick = eventData.clickTime;
        if(Mathf.Abs(currentTimeClick - lastTapTime) < doubleTapThreshold){
            Debug.Log("DOUBLE CLICK");
            Debug.Log(this.GetCardDetails());
            battleUIManager.DisplayCardDescription(this.GetCardDetails());
        }
        lastTapTime = currentTimeClick;
    }
}