using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject loadingOverlay;
    public Image backgroundImage;
    public Slider progressBar;
    public Image fillImage;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI quoteText;

    [Header("Background Sprites")]
    public Sprite[] backgroundSprites;
    public Color backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Quotes")]
    public string[] quotes = new string[]
    {
        "A full glass often hides an empty promise.",
        "The loudest man in the room is rarely the most dangerous.",
        "Keep your cards close, and your enemies closer... but never within arm's reach.",
        "If the bartender reaches below the counter, don't wait to see what he pulls out.",
        "A barricaded door doesn't mean you're safe. It just means they know exactly where you are.",
        "Rust and dried blood smell surprisingly similar in the dark.",
        "Even the oldest clocks strike midnight eventually. Don't linger.",
        "Shadows are good for hiding, but remember... you're not the only one using them.",
        "Walls have ears, but floorboards have memories. Step lightly.",
        "The rats always know when the ship is sinking. Watch where they run.",
        "Not every locked chest holds a treasure. Some just hold a warning.",
        "A flickering bulb isn't dying. It's trying to tell you something."
    };

    [Header("Settings")]
    public float fadeDuration = 0.5f;
    public float minLoadTime = 4.0f;

    [Header("Menu Music")]
    public List<AudioClip> menuTracks;
    public AudioSource audioSource;
    public AudioMixerGroup musicGroup;
    public float menuMusicVolume = 0.5f;

    private CanvasGroup canvasGroup;
    public bool IsLoading { get; private set; }
    private int currentTrackIndex = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            AudioListener.volume = savedVolume;

            canvasGroup = loadingOverlay.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = loadingOverlay.AddComponent<CanvasGroup>();
            
            loadingOverlay.SetActive(false);
            canvasGroup.alpha = 0;

            if (fillImage == null && progressBar != null && progressBar.fillRect != null)
            {
                fillImage = progressBar.fillRect.GetComponent<Image>();
            }

            SetupAudioSource();
            
            if (SceneManager.GetActiveScene().name == "Main Menu")
            {
                PlayRandomTrack();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.outputAudioMixerGroup = musicGroup;
        audioSource.volume = menuMusicVolume;
        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }

    private void Update()
    {

        if (menuTracks != null && menuTracks.Count > 0 && audioSource != null && !audioSource.isPlaying && !IsLoading)
        {

            if (SceneManager.GetActiveScene().name == "Main Menu")
            {
                PlayNextTrack();
            }
        }
    }

    private void PlayRandomTrack()
    {
        if (menuTracks == null || menuTracks.Count == 0) return;
        currentTrackIndex = Random.Range(0, menuTracks.Count);
        PlayCurrentTrack();
    }

    private void PlayNextTrack()
    {
        if (menuTracks == null || menuTracks.Count == 0) return;
        
        if (menuTracks.Count == 1)
        {
            currentTrackIndex = 0;
        }
        else
        {
            int nextIndex;
            do
            {
                nextIndex = Random.Range(0, menuTracks.Count);
            } while (nextIndex == currentTrackIndex);
            
            currentTrackIndex = nextIndex;
        }
        
        PlayCurrentTrack();
    }

    private void PlayCurrentTrack()
    {
        if (currentTrackIndex >= 0 && currentTrackIndex < menuTracks.Count && menuTracks[currentTrackIndex] != null)
        {
            audioSource.clip = menuTracks[currentTrackIndex];
            audioSource.Play();
        }
    }

    public void LoadScene(string sceneName, string text)
    {
        if (IsLoading) return;
        
        if (backgroundSprites != null && backgroundSprites.Length > 0 && backgroundImage != null)
        {
            backgroundImage.sprite = backgroundSprites[Random.Range(0, backgroundSprites.Length)];
            backgroundImage.color = backgroundColor;
        }

        if (quotes != null && quotes.Length > 0 && quoteText != null)
        {
            quoteText.text = "\"" + quotes[Random.Range(0, quotes.Length)] + "\"";
        }

        StartCoroutine(LoadSceneRoutine(sceneName, text));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, string text)
    {
        IsLoading = true;
        loadingText.text = text;
        UpdateProgress(0);
        
        loadingOverlay.SetActive(true);
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = elapsed / fadeDuration;
            yield return null;
        }
        canvasGroup.alpha = 1;

        yield return new WaitForSeconds(0.2f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float startTime = Time.time;

        while (Time.time - startTime < minLoadTime || op.progress < 0.9f)
        {
            float timeProgress = (Time.time - startTime) / minLoadTime;
            float loadProgress = op.progress / 0.9f;
            
            float displayProgress = Mathf.Min(timeProgress, loadProgress);
            
            UpdateProgress(displayProgress);

            if (!audioSource.isPlaying)
            {
                PlayNextTrack();
            }

            yield return null;
        }

        UpdateProgress(1f);
        yield return new WaitForSeconds(0.5f);

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        op.allowSceneActivation = true;

        while (!op.isDone) yield return null;

        yield return new WaitForEndOfFrame();

        elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1 - (elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0;
        loadingOverlay.SetActive(false);
        
        IsLoading = false;
    }

    private void UpdateProgress(float progress)
    {
        if (fillImage != null && fillImage.type == Image.Type.Filled)
        {
            fillImage.fillAmount = progress;

            if (progressBar != null) progressBar.value = progress; 
        }
        else if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }
}
