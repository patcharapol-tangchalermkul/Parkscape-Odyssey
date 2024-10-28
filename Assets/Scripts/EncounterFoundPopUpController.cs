using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EncounterFoundPopUpController : MonoBehaviour
{
    private EncounterController encounterController;

    // Start is called before the first frame update
    void Start()
    {
        encounterController = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterController>();
    }


    public void CloseEncounterFoundPopup() {
        encounterController.CloseEncounterFoundPopup();
    }
}
