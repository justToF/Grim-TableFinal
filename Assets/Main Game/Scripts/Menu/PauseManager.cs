using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public Button resumeButton;
    public Button settingsButton;
    public Button mainMenuButton;
    public Button quitButton;
    public Button settingsBackButton;
    public Slider volumeSlider;
    public Image volumeFillImage;
    public Slider musicVolumeSlider;
    public Image musicVolumeFillImage;
    public UnityEngine.Audio.AudioMixer masterMixer;
public AudioClip pauseSound;
    private AudioSource audioSource;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        float savedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        SetMainVolume(savedMasterVolume);
        SetMusicVolume(savedMusicVolume);

        if (volumeSlider != null)
        {
            volumeSlider.value = savedMasterVolume;
            UpdateVolumeFill(volumeSlider, volumeFillImage, savedMasterVolume);
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = savedMusicVolume;
            UpdateVolumeFill(musicVolumeSlider, musicVolumeFillImage, savedMusicVolume);
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
    }

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);
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

    private void SetMainVolume(float volume)
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

    private void SetMusicVolume(float volume)
    {
        if (masterMixer != null)
        {
            float dbVolume = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            masterMixer.SetFloat("MusicVolume", dbVolume);
        }
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    private void UpdateVolumeFill(Slider slider, Image fillImage, float value)
    {
        if (fillImage != null && fillImage.type == Image.Type.Filled)
        {
            fillImage.fillAmount = value;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                    CloseSettings();
                else
                    Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);

        if (audioSource != null && pauseSound != null)
            audioSource.PlayOneShot(pauseSound, 0.5f);

        GrimPlayerManager.BlockCameraRotation = true;
        
        var freeFly = Object.FindAnyObjectByType<FreeFlyCamera>();
        if (freeFly != null) freeFly.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        AudioListener.pause = true; 
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        AudioListener.pause = false;

        if (audioSource != null && pauseSound != null)
            audioSource.PlayOneShot(pauseSound, 0.5f);

        GrimPlayerManager.BlockCameraRotation = false;

        var freeFly = Object.FindAnyObjectByType<FreeFlyCamera>();
        if (freeFly != null) freeFly.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        GrimPlayerManager.BlockCameraRotation = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("Main Menu");
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
