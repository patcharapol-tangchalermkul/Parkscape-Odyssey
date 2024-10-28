using UnityEngine;

public class PopUpManager : MonoBehaviour
{
    public GameObject popup;

    public void closePopUp() {
        popup.transform.localScale = new Vector3(0, 0, 0);
    }

    public void openPopUp() {
        popup.transform.localScale = new Vector3(1, 1, 1);
    }
}
