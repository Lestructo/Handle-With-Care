using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UISound : MonoBehaviour
{
    public AudioClip clickSound;
    [Range(0f, 1f)] public float volume = 1f;

    void Start()
    {
        if (UIAudioManager.Instance == null)
        {
            var go = new GameObject("UIAudioManager");
            go.AddComponent<UIAudioManager>();
        }

        GetComponent<Button>().onClick.AddListener(PlayClick);
    }

    void PlayClick()
    {
        UIAudioManager.Instance?.Play(clickSound, volume);
    }
}
