using System.Collections.Generic;
using UnityEngine;

public class SetPlayerPin : MonoBehaviour {
    [SerializeField]
    private List<Sprite> playerPins;

    public void Set(string playerID) {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        string role = "";
        foreach (Player player in GameState.Instance.PlayersDetails.Values) {
            if (player.Id == playerID) {
                role = player.Role;
                break;
            }
        }
        Sprite playerPin = null;
        for (int i = 0; i < playerPins.Count; i++) {
            if (playerPins[i].name.Contains(role)) {
                playerPin = playerPins[i];
                break;
            }
        }
        if (playerPin == null) 
            throw new System.Exception("No player pin found for role " + role);
        spriteRenderer.sprite = playerPin;
    }
}
