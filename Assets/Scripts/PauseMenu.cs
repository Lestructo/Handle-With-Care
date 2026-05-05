using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    public GameObject pausePanel;
    public GameObject crosshair;
    public GameObject controlsPanel;
    public TextMeshProUGUI controlsText;

    static readonly string controlsContent =
        "<b>Controls</b>\n\n" +
        "WASD: Move\n" +
        "Space: Jump\n" +
        "Left Click: Grab / Release\n" +
        "Right Click: Magnetize (Hold)\n" +
        "Scroll Wheel: Adjust grab distance\n" +
        "T: Pause game";

    PlayerMovement playerMovement;
    MouseComponent mouseComponent;

    void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        mouseComponent = FindFirstObjectByType<MouseComponent>();
        if (controlsText != null) controlsText.text = controlsContent;
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (controlsPanel != null && controlsPanel.activeSelf)
                ToggleControls();
            else
                TogglePause();
        }
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        AudioListener.pause = IsPaused;
        if (pausePanel != null) pausePanel.SetActive(IsPaused);
        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsPaused;
        if (playerMovement != null) playerMovement.enabled = !IsPaused;
        if (mouseComponent != null) mouseComponent.enabled = !IsPaused;
        if (crosshair != null) crosshair.SetActive(!IsPaused);
    }

    public void ToggleControls()
    {
        bool showControls = controlsPanel != null && !controlsPanel.activeSelf;
        if (controlsPanel != null) controlsPanel.SetActive(showControls);
        if (pausePanel != null) pausePanel.SetActive(!showControls);
    }

    public void Resume()
    {
        if (IsPaused) TogglePause();
    }

    public void ReturnToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        // Make sure time scale is reset if this object is destroyed mid-pause
        IsPaused = false;
        Time.timeScale = 1f;
    }
}
