using UnityEngine;
using System.Collections;

public class EntityController : MonoBehaviour
{
    public float spinSpeed = 1000f;
    public AudioClip spinningSound;
    public AudioSource audioSource;
    [Range(0f, 1f)] public float spinningVolume = 0.4f;
    private bool isSpinning = false;
    private Quaternion targetRotation;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = spinningVolume;
    }

    public void StartSpinning()
    {
        isSpinning = true;
        if (spinningSound != null && audioSource != null)
        {
            audioSource.clip = spinningSound;
            audioSource.volume = spinningVolume;
            audioSource.Play();
        }
    }

    public void StopAndLookAt(Vector3 targetPos)
    {
        isSpinning = false;
        if (audioSource != null && audioSource.isPlaying)
        {
            StartCoroutine(FadeOutAudio(1.5f));
        }
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0;
        targetRotation = Quaternion.LookRotation(direction);

        StartCoroutine(SmoothLookAt());
    }

    void Update()
    {
        if (isSpinning)
        {

            transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime);
        }
    }

    IEnumerator SmoothLookAt()
    {
        float elapsed = 0;
        float duration = 1.5f;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {

            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    IEnumerator FadeOutAudio(float duration)
    {
        float startVolume = audioSource.volume;
        float startPitch = audioSource.pitch;
        float elapsed = 0;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            audioSource.volume = Mathf.Lerp(startVolume, 0, t);
            audioSource.pitch = Mathf.Lerp(startPitch, 0.3f, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
        audioSource.pitch = startPitch;
    }
}
