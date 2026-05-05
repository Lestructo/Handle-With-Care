using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    public float maximumDim = 0.2f;
    public float maximumBoost = 0.2f;
    public float speed = 0.1f;
    public float strength = 250f;

    public int minFlickers = 5;
    public int maxFlickers = 20;
    public float minCooldown = 3f;
    public float maxCooldown = 10f;

    private Light source;
    private float initialIntensity;

    void Start()
    {
        source = GetComponent<Light>();
        initialIntensity = source.intensity;
        StartCoroutine(Flicker());
    }

    private IEnumerator Flicker()
    {
        while (true)
        {
            int count = Random.Range(minFlickers, maxFlickers + 1);
            for (int i = 0; i < count; i++)
            {
                source.intensity = Mathf.Lerp(source.intensity, Random.Range(initialIntensity - maximumDim, initialIntensity + maximumBoost), strength * Time.deltaTime);
                yield return new WaitForSeconds(speed);
            }

            source.intensity = initialIntensity;
            yield return new WaitForSeconds(Random.Range(minCooldown, maxCooldown));
        }
    }
}
