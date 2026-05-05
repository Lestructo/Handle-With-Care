using UnityEngine;

public class KillZone : MonoBehaviour
{
    public float killBelowY = -50f;

    void Update()
    {
        foreach (Rigidbody rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            if (rb.position.y < killBelowY)
                Destroy(rb.gameObject);
    }
}
