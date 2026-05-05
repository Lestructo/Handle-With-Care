using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [SerializeField] TextMeshProUGUI timerText;

    public const string BestTimeKey = "BestTime";

    float elapsed;
    bool running = true;

    public float Elapsed => elapsed;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!running) return;
        elapsed += Time.deltaTime;
        int m = (int)(elapsed / 60f);
        float s = elapsed % 60f;
        if (timerText != null)
            timerText.text = $"{m}:{s:00.00}";
    }

    public void StopTimer()
    {
        if (!running) return;
        running = false;
        float best = PlayerPrefs.GetFloat(BestTimeKey, float.MaxValue);
        if (elapsed < best)
        {
            PlayerPrefs.SetFloat(BestTimeKey, elapsed);
            PlayerPrefs.Save();
        }
    }

    public static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        float s = seconds % 60f;
        return $"{m}:{s:00.00}";
    }
}
