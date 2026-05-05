using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    public Image overlay;
    public float fadeInDuration = 1.5f;

    IEnumerator Start()
    {
        overlay.color = Color.black;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeInDuration;
            overlay.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t));
            yield return null;
        }
        overlay.color = new Color(0f, 0f, 0f, 0f);
        overlay.gameObject.SetActive(false);
    }
}
