using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class JukeboxController : MonoBehaviour
{
    [SerializeField] private List<AudioClip> tracks;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private float volume = 0.3f;

    private int currentTrackIndex;

    private void Start()
    {
        if (tracks == null || tracks.Count == 0)
        {
            Debug.LogWarning("No tracks assigned to JukeboxController.");
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.outputAudioMixerGroup = musicGroup;
        audioSource.volume = volume;
        audioSource.loop = false;
        audioSource.playOnAwake = false;

        currentTrackIndex = Random.Range(0, tracks.Count);
        PlayCurrentTrack();
    }

    private void Update()
    {
        if (tracks == null || tracks.Count == 0) return;

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        if (!audioSource.isPlaying)
        {
            PlayNextTrack();
        }
    }

    private void PlayCurrentTrack()
    {
        if (tracks[currentTrackIndex] != null)
        {
            audioSource.clip = tracks[currentTrackIndex];
            audioSource.Play();
        }
    }

    private void PlayNextTrack()
    {
        currentTrackIndex = (currentTrackIndex + 1) % tracks.Count;
        PlayCurrentTrack();
    }
}
