using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Sub-Menus")]
    public GameObject playSubMenu;
    public GameObject settingsMenu;
    public GameObject creditsMenu;
    public GameObject mainMenuButtons;
    
    [Header("Effects")]
    public Volume blurVolume;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private float horizontalOffset = 50f;

    [Header("Tutorial Data")]
    public TutorialData licenseTutorial;

    [Header("Volume Sliders")]
    public Slider volumeSlider;
    public Image volumeFillImage;
    public Slider musicVolumeSlider;
    public Image musicVolumeFillImage;
    public UnityEngine.Audio.AudioMixer masterMixer;

    private CanvasGroup playSubMenuCG;
    private RectTransform playSubMenuRT;
    private CanvasGroup settingsCG;
    private CanvasGroup creditsCG;
    private CanvasGroup mainButtonsCG;
    private Coroutine transitionCoroutine;
    private Coroutine settingsCoroutine;
    private Coroutine creditsCoroutine;
    private Vector2 originalPosition;

    private void Awake()
    {
        if (playSubMenu != null)
        {
            playSubMenuCG = playSubMenu.GetComponent<CanvasGroup>();
            if (playSubMenuCG == null)
                playSubMenuCG = playSubMenu.AddComponent<CanvasGroup>();
            
            playSubMenuRT = playSubMenu.GetComponent<RectTransform>();
            originalPosition = playSubMenuRT.anchoredPosition;

            playSubMenu.SetActive(false);
            playSubMenuCG.alpha = 0;
            playSubMenu.transform.localScale = hiddenScale;
            playSubMenuRT.anchoredPosition = originalPosition - new Vector2(horizontalOffset, 0);
        }

        if (settingsMenu != null)
        {
            settingsCG = settingsMenu.GetComponent<CanvasGroup>();
            if (settingsCG == null) settingsCG = settingsMenu.AddComponent<CanvasGroup>();
            settingsMenu.SetActive(false);
            settingsCG.alpha = 0;
        }

        if (creditsMenu != null)
        {
            creditsCG = creditsMenu.GetComponent<CanvasGroup>();
            if (creditsCG == null) creditsCG = creditsMenu.AddComponent<CanvasGroup>();
            creditsMenu.SetActive(false);
            creditsCG.alpha = 0;
        }

        if (mainMenuButtons != null)
        {
            mainButtonsCG = mainMenuButtons.GetComponent<CanvasGroup>();
            if (mainButtonsCG == null) mainButtonsCG = mainMenuButtons.AddComponent<CanvasGroup>();
        }

        if (blurVolume != null)
        {
            blurVolume.weight = 0;
        }
    }

    public void TogglePlaySubMenu()
    {
        if (playSubMenu == null) return;

        bool isOpening = !playSubMenu.activeSelf;
        
        if (isOpening)
        {
            if (settingsMenu != null && settingsMenu.activeSelf) ToggleSettingsMenu();
            if (creditsMenu != null && creditsMenu.activeSelf) ToggleCreditsMenu();
        }

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionRoutine(isOpening));
    }

    private IEnumerator TransitionRoutine(bool opening)
    {
        if (opening)
        {
            playSubMenu.SetActive(true);
        }

        if (playSubMenuCG != null)
        {
            if (opening)
            {
                playSubMenuCG.interactable = true;
                playSubMenuCG.blocksRaycasts = true;
            }
        }

        float startAlpha = playSubMenuCG != null ? playSubMenuCG.alpha : 0f;
        float targetAlpha = opening ? 1f : 0f;
        
        Vector3 startScale = playSubMenu.transform.localScale;
        Vector3 targetScale = opening ? Vector3.one : hiddenScale;
        
        Vector2 startPos = playSubMenuRT.anchoredPosition;
        Vector2 targetPos = opening ? originalPosition : originalPosition - new Vector2(horizontalOffset, 0);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);
            
            if (playSubMenuCG != null) playSubMenuCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothedT);
            
            playSubMenu.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothedT);
            playSubMenuRT.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothedT);
            
            yield return null;
        }

        if (playSubMenuCG != null)
        {
            playSubMenuCG.alpha = targetAlpha;
            if (!opening)
            {
                playSubMenuCG.interactable = false;
                playSubMenuCG.blocksRaycasts = false;
            }
        }
        
        playSubMenu.transform.localScale = targetScale;
        playSubMenuRT.anchoredPosition = targetPos;

        if (!opening)
        {
            playSubMenu.SetActive(false);
        }
        
        transitionCoroutine = null;
    }

    public void ToggleSettingsMenu()
    {
        if (settingsMenu == null) return;

        bool isOpening = !settingsMenu.activeSelf;
        
        if (isOpening)
        {
            if (playSubMenu != null && playSubMenu.activeSelf) TogglePlaySubMenu();
            if (creditsMenu != null && creditsMenu.activeSelf) ToggleCreditsMenu();
        }

        if (settingsCoroutine != null)
            StopCoroutine(settingsCoroutine);

        settingsCoroutine = StartCoroutine(SettingsTransitionRoutine(isOpening));
    }

    private IEnumerator SettingsTransitionRoutine(bool opening)
    {
        if (opening)
        {
            settingsMenu.SetActive(true);
        }

        if (settingsCG != null)
        {
            if (opening)
            {
                settingsCG.interactable = true;
                settingsCG.blocksRaycasts = true;
            }
        }

        float startAlpha = settingsCG != null ? settingsCG.alpha : 0f;
        float targetAlpha = opening ? 1f : 0f;

        float startMainAlpha = mainButtonsCG != null ? mainButtonsCG.alpha : 1f;
        float targetMainAlpha = opening ? 0f : 1f;

        float startBlur = blurVolume != null ? blurVolume.weight : 0f;
        float targetBlur = opening ? 1f : 0f;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);

            if (settingsCG != null) settingsCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothedT);
            if (mainButtonsCG != null) mainButtonsCG.alpha = Mathf.Lerp(startMainAlpha, targetMainAlpha, smoothedT);
            if (blurVolume != null) blurVolume.weight = Mathf.Lerp(startBlur, targetBlur, smoothedT);

            yield return null;
        }

        if (settingsCG != null)
        {
            settingsCG.alpha = targetAlpha;
            if (!opening)
            {
                settingsCG.interactable = false;
                settingsCG.blocksRaycasts = false;
            }
        }

        if (mainButtonsCG != null)
        {
            mainButtonsCG.alpha = targetMainAlpha;
            mainButtonsCG.interactable = !opening;
            mainButtonsCG.blocksRaycasts = !opening;
        }
        
        if (blurVolume != null) blurVolume.weight = targetBlur;

        if (!opening)
        {
            settingsMenu.SetActive(false);
        }

        settingsCoroutine = null;
    }

    public void ToggleCreditsMenu()
    {
        if (creditsMenu == null) return;

        bool isOpening = !creditsMenu.activeSelf;

        if (isOpening)
        {
            if (playSubMenu != null && playSubMenu.activeSelf) TogglePlaySubMenu();
            if (settingsMenu != null && settingsMenu.activeSelf) ToggleSettingsMenu();
        }

        if (creditsCoroutine != null)
            StopCoroutine(creditsCoroutine);

        creditsCoroutine = StartCoroutine(CreditsTransitionRoutine(isOpening));
    }

    private IEnumerator CreditsTransitionRoutine(bool opening)
    {
        if (opening)
        {
            creditsMenu.SetActive(true);
        }

        if (creditsCG != null)
        {
            if (opening)
            {
                creditsCG.interactable = true;
                creditsCG.blocksRaycasts = true;
            }
        }

        float startAlpha = creditsCG != null ? creditsCG.alpha : 0f;
        float targetAlpha = opening ? 1f : 0f;

        float startMainAlpha = mainButtonsCG != null ? mainButtonsCG.alpha : 1f;
        float targetMainAlpha = opening ? 0f : 1f;

        float startBlur = blurVolume != null ? blurVolume.weight : 0f;
        float targetBlur = opening ? 1f : 0f;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);

            if (creditsCG != null) creditsCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothedT);
            if (mainButtonsCG != null) mainButtonsCG.alpha = Mathf.Lerp(startMainAlpha, targetMainAlpha, smoothedT);
            if (blurVolume != null) blurVolume.weight = Mathf.Lerp(startBlur, targetBlur, smoothedT);

            yield return null;
        }

        if (creditsCG != null)
        {
            creditsCG.alpha = targetAlpha;
            if (!opening)
            {
                creditsCG.interactable = false;
                creditsCG.blocksRaycasts = false;
            }
        }

        if (mainButtonsCG != null)
        {
            mainButtonsCG.alpha = targetMainAlpha;
            mainButtonsCG.interactable = !opening;
            mainButtonsCG.blocksRaycasts = !opening;
        }

        if (blurVolume != null) blurVolume.weight = targetBlur;

        if (!opening)
        {
            creditsMenu.SetActive(false);
        }

        creditsCoroutine = null;
    }

    private void Start()
    {
        float savedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        SetMainVolume(savedMasterVolume);
        SetMusicVolume(savedMusicVolume);

        if (volumeSlider != null)
        {
            volumeSlider.value = savedMasterVolume;
            UpdateVolumeFill(volumeSlider, volumeFillImage, savedMasterVolume);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = savedMusicVolume;
            UpdateVolumeFill(musicVolumeSlider, musicVolumeFillImage, savedMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
    }

    private void OnVolumeChanged(float value)
    {
        SetMainVolume(value);
        UpdateVolumeFill(volumeSlider, volumeFillImage, value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        SetMusicVolume(value);
        UpdateVolumeFill(musicVolumeSlider, musicVolumeFillImage, value);
    }

    private void UpdateVolumeFill(Slider slider, Image fillImage, float value)
    {
        if (fillImage != null && fillImage.type == Image.Type.Filled)
        {
            fillImage.fillAmount = value;
        }
    }

    public void SetMainVolume(float volume)
    {
        if (masterMixer != null)
        {
            float dbVolume = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            masterMixer.SetFloat("MasterVolume", dbVolume);
        }
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        if (masterMixer != null)
        {
            float dbVolume = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            masterMixer.SetFloat("MusicVolume", dbVolume);
        }
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void LoadTutorial()
    {
        GameSettings.IsManualMode = true;
        GameSettings.tutorialToLoad = licenseTutorial;
        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadScene("test", "Loading Tutorial");
        else
            SceneManager.LoadScene("test");
    }

    public void LoadNormalGame()
    {
        GameSettings.IsManualMode = false;
        GameSettings.tutorialToLoad = null;
        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadScene("test", "Loading Normal");
        else
            SceneManager.LoadScene("test");
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
