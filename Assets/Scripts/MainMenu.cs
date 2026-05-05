using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public string gameSceneName = "Handle_With_Care";

    [Header("Controls Panel")]
    public GameObject controlsPanel;
    public GameObject controlsOverlay;
    public TextMeshProUGUI controlsText;

    static readonly string controlsContent =
        "<b>Controls</b>\n\n" +
        "WASD: Move\n" +
        "Space: Jump\n" +
        "Left Click: Grab / Release\n" +
        "Right Click: Magnetize (Hold)\n" +
        "Scroll Wheel: Adjust grab distance\n" +
        "T: Pause game";

    void Awake()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (controlsOverlay != null) controlsOverlay.SetActive(false);
    }

    void Start()
    {
        if (controlsText != null) controlsText.text = controlsContent;
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ToggleControls()
    {
        bool show = controlsPanel != null && !controlsPanel.activeSelf;
        if (controlsPanel != null) controlsPanel.SetActive(show);
        if (controlsOverlay != null) controlsOverlay.SetActive(show);
    }

    // Call this from a TextMeshProUGUI on the controls panel to auto-populate it
    public void SetControlsText(TextMeshProUGUI target)
    {
        if (target != null) target.text = controlsContent;
    }
}
