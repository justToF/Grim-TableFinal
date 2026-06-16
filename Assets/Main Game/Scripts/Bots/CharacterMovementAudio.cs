using UnityEngine;
using System.Collections.Generic;

public class CharacterMovementAudio : MonoBehaviour
{
    public List<AudioClip> rustleClips;
    [Range(0f, 1f)] public float volume = 0.15f;
    public float rotationThreshold = 40f;
    public float cooldown = 0.8f;

    private AudioSource audioSource;
    private float lastCooldownTime;
    private Quaternion lastRotation;
    private Transform monitoredTransform;

    private void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        PlayerRouletteAnimEvents animEvents = GetComponent<PlayerRouletteAnimEvents>();
        if (animEvents != null && animEvents.armBone != null)
        {
            monitoredTransform = animEvents.armBone;
        }
        else
        {
            monitoredTransform = transform;
        }

        lastRotation = monitoredTransform.rotation;
    }

    private void Update()
    {
        if (monitoredTransform == null) return;

        float deltaRotation = Quaternion.Angle(lastRotation, monitoredTransform.rotation);
        float rotationSpeed = deltaRotation / Time.deltaTime;

        if (rotationSpeed > rotationThreshold && Time.time > lastCooldownTime + cooldown)
        {
            PlayRandomRustle();
            lastCooldownTime = Time.time;
        }

        lastRotation = monitoredTransform.rotation;
    }

    public void PlayRandomRustle()
    {
        if (rustleClips == null || rustleClips.Count == 0) return;

        AudioClip clip = rustleClips[Random.Range(0, rustleClips.Count)];
        audioSource.PlayOneShot(clip, volume);
    }
}
