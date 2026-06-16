using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AmbientSoundManager : MonoBehaviour
{
    public static AmbientSoundManager Instance;

    [Header("Door Knocking")]
    public List<AudioClip> doorKnockClips;
    public Vector2 doorKnockIntervalRange = new Vector2(60f, 180f);
    public float doorKnockVolume = 0.8f;

    [Header("Mice in Pipes")]
    public List<AudioClip> miceClips;
    public Vector2 miceIntervalRange = new Vector2(30f, 90f);
    public float miceVolume = 0.3f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
    }

    [Header("Exterior / Storm")]
    public List<AudioClip> branchTapClips;
    public Vector2 branchTapIntervalRange = new Vector2(45f, 150f);
    public float branchTapVolume = 0.2f;
    public List<Transform> windowPositions;

    [Header("Rain Loop")]
    public AudioClip rainLoopClip;
    public float rainVolume = 0.15f;

    private void Start()
    {
        StartCoroutine(RandomSoundRoutine(doorKnockClips, doorKnockIntervalRange, doorKnockVolume, "Door Knock"));
        StartCoroutine(RandomSoundRoutine(miceClips, miceIntervalRange, miceVolume, "Mice"));
        StartCoroutine(RandomPositionalSoundRoutine(branchTapClips, branchTapIntervalRange, branchTapVolume, windowPositions, "Branch Tap"));
        
        SetupRainLoops();
    }

    private void SetupRainLoops()
    {
        if (rainLoopClip == null || windowPositions == null || windowPositions.Count == 0) return;

        foreach (var window in windowPositions)
        {
            if (window == null) continue;
            AudioSource rainSource = window.gameObject.AddComponent<AudioSource>();
            rainSource.clip = rainLoopClip;
            rainSource.loop = true;
            rainSource.volume = rainVolume;
            rainSource.spatialBlend = 1f;
            rainSource.minDistance = 1f;
            rainSource.maxDistance = 10f;
            rainSource.Play();
        }
    }

    private IEnumerator RandomPositionalSoundRoutine(List<AudioClip> clips, Vector2 intervalRange, float volume, List<Transform> positions, string debugName)
    {
        if (clips == null || clips.Count == 0 || positions == null || positions.Count == 0) yield break;

        while (true)
        {
            float waitTime = Random.Range(intervalRange.x, intervalRange.y);
            yield return new WaitForSeconds(waitTime);

            AudioClip clip = clips[Random.Range(0, clips.Count)];
            Transform pos = positions[Random.Range(0, positions.Count)];
            
            if (pos != null)
            {
                AudioSource.PlayClipAtPoint(clip, pos.position, volume);
                Debug.Log($"[Ambient] Playing {debugName} at {pos.name}: {clip.name}");
            }
        }
    }

    private IEnumerator RandomSoundRoutine(List<AudioClip> clips, Vector2 intervalRange, float volume, string debugName)
{
        if (clips == null || clips.Count == 0) yield break;

        while (true)
        {
            float waitTime = Random.Range(intervalRange.x, intervalRange.y);
            yield return new WaitForSeconds(waitTime);

            AudioClip clip = clips[Random.Range(0, clips.Count)];
            audioSource.PlayOneShot(clip, volume);
            Debug.Log($"[Ambient] Playing {debugName}: {clip.name}");
        }
    }
}
