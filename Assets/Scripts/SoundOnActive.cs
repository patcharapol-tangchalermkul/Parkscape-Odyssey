using UnityEngine;

public class SoundOnActive : MonoBehaviour {

    [SerializeField]
    private bool playOnAwake;
    private bool postAwake = false;

    private void Awake() {
        if (playOnAwake) {
            PlayAudio();
        } 
        postAwake = true;
    }
    void OnEnable() {
        if (postAwake) {
            PlayAudio();
        }
    }

    void PlayAudio() {
        if (TryGetComponent(out AudioSource audioSource)) {
            if (audioSource.clip == null)
                Debug.LogWarning("No AudioClip found on " + gameObject.name);
            if (!audioSource.isPlaying)
                audioSource.Play();
        } else {
            Debug.LogWarning("No AudioSource found on " + gameObject.name);
        }
    }
}
