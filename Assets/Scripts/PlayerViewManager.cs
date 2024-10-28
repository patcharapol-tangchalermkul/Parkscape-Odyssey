using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerViewManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text PlayerRoleText;

    [SerializeField]
    private GameObject PlayerIcon;

    [SerializeField]
    private TMP_Text HealthValue;

    [SerializeField]
    private TMP_Text ManaValue;

    [SerializeField]
    private TMP_Text StrengthValue;

    [SerializeField]
    private TMP_Text SpeedValue;

    [SerializeField]
    private TMP_Text AttackMultiplierValue;

    [SerializeField]
    private TMP_Text DefenceMultiplierValue;

    [SerializeField]
    private TMP_Text Description;

    public void SetPlayer(Player player) {
        PlayerRoleText.text = player.Role;
        HealthValue.text = player.CurrentHealth.ToString() + "/" + player.MaxHealth.ToString();
        ManaValue.text = player.Mana.ToString() + "/" + player.MaxMana.ToString();
        StrengthValue.text = player.Strength.ToString();
        SpeedValue.text = player.Speed.ToString();
        AttackMultiplierValue.text = string.Format("{0:0.00}", player.AttackMultiplier);
        DefenceMultiplierValue.text = string.Format("{0:0.00}", player.DefenceMultiplier);
        Description.text = player.Description;
    }

    public void SetPlayerIcon(Sprite roleIcon) {
        ((Image) PlayerIcon.GetComponent(typeof(Image))).sprite = roleIcon;
    }
}
