using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
public class BasicQuestUISetter : MonoBehaviour {
    [SerializeField]
    private TMP_Text questText;

    [SerializeField]
    private GameObject progressBar;

    [SerializeField]
    private TMP_Text progressValueText;

    [SerializeField]
    private TMP_Text progressTargetText;

    [SerializeField]
    private GameObject completedOverlay;

    [SerializeField]
    private GameObject referenceImage;

    [SerializeField]
    private List<Sprite> referenceImages;

    public void Set(BasicQuest quest) {
        questText.text = quest.ToString();
        progressBar.GetComponent<Slider>().maxValue = quest.Target;
        progressBar.GetComponent<Slider>().value = quest.Progress;
        progressValueText.GetComponent<TMP_Text>().text = quest.Progress.ToString();
        progressTargetText.GetComponent<TMP_Text>().text = quest.Target.ToString();
        referenceImage.GetComponent<Image>().sprite = FindImage(quest.Label);
        completedOverlay.SetActive(quest.IsCompleted());
    }

    private Sprite FindImage(string label) {
        foreach (Sprite image in referenceImages) {
            if (image.name.ToLower().Contains(label)) {
                return image;
            }
        }
        return null;
    }
}
