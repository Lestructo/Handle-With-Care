using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class VictoryScreen : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI timeText;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Trigger()
    {
        float time = GameTimer.Instance != null ? GameTimer.Instance.Elapsed : 0f;

        if (timeText != null) timeText.text = "Time: " + GameTimer.FormatTime(time);

        AudioListener.pause = true;
        AudioListener.volume = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (panel != null) panel.SetActive(true);
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
