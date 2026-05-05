using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Ladder : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true; // enforced in code so it doesn't need to be set manually in the Inspector
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement player))
            player.AttachToLadder();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement player))
            player.DetachFromLadder();
    }
}
