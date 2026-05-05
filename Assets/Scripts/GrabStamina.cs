using UnityEngine;
using UnityEngine.UI;

public class GrabStamina : MonoBehaviour
{
    public Image staminaRing;
    public float maxDuration = 5f;
    public float grabDrainRate = 0.5f;
    public float magnetizeDrainMultiplier = 0.25f;
    public float rechargeRate = 0.5f;

    public bool CanUse => currentStamina > 0f;

    float currentStamina;
    PlayerGrab playerGrab;

    void Start()
    {
        currentStamina = maxDuration;
        playerGrab = FindFirstObjectByType<PlayerGrab>();
    }

    void Update()
    {
        if (playerGrab == null) return;

        if (playerGrab.IsGrabbing)
            currentStamina -= grabDrainRate * Time.deltaTime;
        else if (playerGrab.IsMagnetizing)
            currentStamina -= grabDrainRate * magnetizeDrainMultiplier * Time.deltaTime;
        else
            currentStamina += rechargeRate * Time.deltaTime;

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxDuration);

        if (currentStamina <= 0f)
            playerGrab.ForceReleaseAll();

        if (staminaRing != null)
        {
            staminaRing.fillAmount = currentStamina / maxDuration;
            if (playerGrab.crosshairImage != null)
                staminaRing.color = playerGrab.crosshairImage.color;
        }
    }
}
