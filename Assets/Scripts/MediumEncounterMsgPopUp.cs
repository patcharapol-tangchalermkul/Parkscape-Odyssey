using UnityEngine;

public class MediumEncounterMsgPopUp : MonoBehaviour
{
    public static MediumEncounterMsgPopUp selfReference;

    // Medium Encounter Message
    [SerializeField]
    private GameObject mediumEncounterMessagePopupMember;
    // FOR DEBUGGING ONLY
    [SerializeField]
    private bool debugIsLeader;
    void Awake() {
        if (!selfReference) {
            selfReference = this;
            mediumEncounterMessagePopupMember.SetActive(false);
        } else {
            Destroy(gameObject);
        }
    }

    /*** Medium encounter popups ***/
    public void ShowMediumEncounterMessagePopup() {
        bool isLeader = GameState.Instance.isLeader;
        if (GameState.MAPDEBUGMODE) {
            isLeader = debugIsLeader;
        }
        if (isLeader) {
            return;
        }
        Debug.Log("ShowMediumEncounterMessagePopup");
        mediumEncounterMessagePopupMember.SetActive(true);
    }

    public void CloseMediumEncounterMessagePopup() {
        // bool isLeader = GameState.Instance.isLeader;
        // if (GameState.MAPDEBUGMODE) {
        //     isLeader = debugIsLeader;
        // }
        // if (isLeader) {
        //     return;
        // }
        Debug.Log("CloseMediumEncounterMessagePopup");
        mediumEncounterMessagePopupMember.SetActive(false);
    }


}
