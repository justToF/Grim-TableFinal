using UnityEngine;
using TMPro;

public class GameTimerUI : MonoBehaviour
{
    public static GameTimerUI Instance;

    [Header("Timer UI Elements")]
    public GameObject playCardsUIPanel;
    public TextMeshProUGUI playCardsMessageText;
    public TMP_FontAsset wonderiaFont;

    [Header("Audio")]
    public AudioClip beepSound;

    private Color originalMessageColor;
    private bool colorInitialized = false;
    private int lastBeepSecond = -1;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowPlayCardsMessage(bool show)
    {
        if (show && playCardsMessageText != null)
        {
            if (!colorInitialized)
            {
                originalMessageColor = playCardsMessageText.color;
                colorInitialized = true;
            }
            
            playCardsMessageText.color = originalMessageColor;
            playCardsMessageText.transform.localScale = Vector3.one;
            if (wonderiaFont != null) playCardsMessageText.font = wonderiaFont;
            playCardsMessageText.text = "Play your cards. 15 seconds left";
            lastBeepSecond = -1;
        }
        else if (!show && playCardsMessageText != null && colorInitialized)
        {

            playCardsMessageText.color = originalMessageColor;
            playCardsMessageText.transform.localScale = Vector3.one;
        }

        if (playCardsUIPanel != null) playCardsUIPanel.SetActive(show);
    }

    public void UpdateTimerUI(float remaining)
    {
        if (playCardsMessageText == null) return;

        if (remaining <= 0)
        {
            playCardsMessageText.text = "Play your cards. 0 seconds left";
            playCardsMessageText.color = Color.red;
            return;
        }

        int seconds = Mathf.CeilToInt(remaining);
        playCardsMessageText.text = $"Play your cards. {seconds} seconds left";
        
        if (remaining <= 3.05f)
        {

            playCardsMessageText.color = Color.red;
            float pulse = 1f + Mathf.Sin(Time.time * 20f) * 0.15f;
            playCardsMessageText.transform.localScale = new Vector3(pulse, pulse, 1f);

            if (seconds != lastBeepSecond)
            {
                lastBeepSecond = seconds;
                if (beepSound != null)
                {

                    AudioSource.PlayClipAtPoint(beepSound, Camera.main.transform.position, 0.6f);
                }
            }
        }
        else
        {

            if (colorInitialized) playCardsMessageText.color = originalMessageColor;
            playCardsMessageText.transform.localScale = Vector3.one;
        }
    }
}
