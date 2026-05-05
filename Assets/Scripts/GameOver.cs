using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public Image Overlay;
    public GameObject gameOverPanel;
    public float duration = 2f;
    public float blackFadeDuration = 1f;
    public float maxShakeIntensity = 0.3f;

    void Start()
    {
        Overlay.color = new Color(1f, 1f, 1f, 0f);
        gameOverPanel.SetActive(false);
    }

    public void Trigger()
    {
        StartCoroutine(DoGameOver());
    }

    IEnumerator DoGameOver()
    {
        Camera cam = Camera.main;
        Vector3 camOrigin = cam != null ? cam.transform.localPosition : Vector3.zero;

        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float clamped = Mathf.Clamp01(t);

            Overlay.color = new Color(1f, 1f, 1f, clamped);

            if (cam != null)
                cam.transform.localPosition = camOrigin + Random.insideUnitSphere * (clamped * maxShakeIntensity);

            yield return null;
        }

        if (cam != null) cam.transform.localPosition = camOrigin;

        float t2 = 0f;
        while (t2 < 1f)
        {
            t2 += Time.deltaTime / blackFadeDuration;
            float clamped2 = Mathf.Clamp01(t2);
            Overlay.color = Color.Lerp(Color.white, Color.black, clamped2);
            AudioListener.volume = Mathf.Lerp(1f, 0f, clamped2);
            yield return null;
        }

        AudioListener.pause = true;
        AudioListener.volume = 1f;
        Overlay.raycastTarget = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        gameOverPanel.SetActive(true);
    }

    public void Restart()
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
