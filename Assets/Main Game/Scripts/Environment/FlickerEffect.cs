using UnityEngine;

public class FlickerEffect : MonoBehaviour
{
    private Light lightSource;
    private MeshRenderer bulbRenderer;
    public Material bulbMaterial;

    [Header("Flicker Settings")]
    public float minIntensity = 0.1f;
    public float maxIntensity = 3.0f;
    public float flickerSpeed = 0.05f;

    [Header("Blackout Settings")]
    public float chanceToBlackout = 0.1f;
    public float minBlackoutTime = 0.5f;
    public float maxBlackoutTime = 2.0f;

    private bool isBlackedOut = false;
    private float timer = 0f;

    void Start()
    {
        lightSource = GetComponent<Light>();
        bulbRenderer = GetComponentInParent<MeshRenderer>();
    }

    void Update()
    {
        if (isBlackedOut)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                isBlackedOut = false;

            }
            return;
        }

        float noise = Mathf.PerlinNoise(Time.time * (1 / flickerSpeed), 0);
        float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

        UpdateLight(currentIntensity);

        if (Random.value < (chanceToBlackout * Time.deltaTime))
        {
            StartBlackout();
        }
    }

    void StartBlackout()
    {
        isBlackedOut = true;
        timer = Random.Range(minBlackoutTime, maxBlackoutTime);
        UpdateLight(0f);
    }

    void UpdateLight(float intensity)
    {
        lightSource.intensity = intensity;

        if (bulbMaterial != null)
        {
            if (intensity <= 0.1f)
                bulbMaterial.DisableKeyword("_EMISSION");
            else
                bulbMaterial.EnableKeyword("_EMISSION");
        }
    }
}
