using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public GameObject gamblePanel;
    private GameObject currentTarget;
    public GameObject splitStealPanel;
    public TMPro.TextMeshProUGUI splitStealOpponentText;
    public TMPro.TextMeshProUGUI splitStealParticipantText;
    
    [Header("Split Or Steal Resolution")]
    public GameObject splitStealResolutionPanel;
    public TMPro.TextMeshProUGUI p1NameText;
    public TMPro.TextMeshProUGUI p1ChoiceText;
    public TMPro.TextMeshProUGUI p2NameText;
    public TMPro.TextMeshProUGUI p2ChoiceText;
    public TMPro.TextMeshProUGUI splitStealResultText;

    private System.Action<bool> onChoiceMade;
public GameObject selectionInstructionUI;
    public GameObject winPanel;
    public TMPro.TextMeshProUGUI winText;

    [Header("Sixth Sense UI")]
    public GameObject sixthSensePanel;
    public TMPro.TextMeshProUGUI sixthSenseTitle;

    [Header("Spy UI")]
    public GameObject spyPanel;
    public TMPro.TextMeshProUGUI spyTitle;
    public Transform spyIconContainer;
    public GameObject spyIconPrefab;

    void Awake() => Instance = this;

    public bool IsGameOver { get; private set; } = false;

    public void ShowPlayCardsMessage(bool show)
    {
        if (IsGameOver) { if (GameTimerUI.Instance != null) GameTimerUI.Instance.ShowPlayCardsMessage(false); return; }
        if (GameTimerUI.Instance != null) GameTimerUI.Instance.ShowPlayCardsMessage(show);
    }

    public void UpdateTimerUI(float remaining)
    {
        if (IsGameOver) return;
        if (GameTimerUI.Instance != null) GameTimerUI.Instance.UpdateTimerUI(remaining);
    }

    public void ShowWinUI(string winnerName)
    {
        IsGameOver = true;

        ShowPlayCardsMessage(false);
        if (selectionInstructionUI != null) selectionInstructionUI.SetActive(false);
        if (gamblePanel != null) gamblePanel.SetActive(false);
        if (splitStealPanel != null) splitStealPanel.SetActive(false);
        if (sixthSensePanel != null) sixthSensePanel.SetActive(false);
        if (spyPanel != null) spyPanel.SetActive(false);

        winPanel.SetActive(true);
        if (winText != null)
        {
            winText.text = winnerName + " won Grim's game!";
        }

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        GrimPlayerManager.BlockCameraRotation = true;
    }

    public void OnClickRestart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        GrimPlayerManager.BlockCameraRotation = false;
    }

    public void OnClickMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
        GrimPlayerManager.BlockCameraRotation = false;
    }

    public void ShowSplitStealUI(string participantName, string opponentName, System.Action<bool> callback)
    {
        if (IsGameOver) return;
        onChoiceMade = callback;
        if (splitStealParticipantText != null)
            splitStealParticipantText.text = "Choosing: " + participantName;
        if (splitStealOpponentText != null)
            splitStealOpponentText.text = "Opponent: " + opponentName;
            
        splitStealPanel.SetActive(true);
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        GrimPlayerManager.BlockCameraRotation = true;
    }
    public void OnClickSplit() { FinalizeChoice(true); }
    public void OnClickSteal() { FinalizeChoice(false); }
    private void FinalizeChoice(bool choice)
    {
        splitStealPanel.SetActive(false);
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        GrimPlayerManager.BlockCameraRotation = false;

        onChoiceMade?.Invoke(choice);
    }

    public void ShowSplitStealResolution(string p1Name, bool p1Split, string p2Name, bool p2Split, string result)
    {
        if (splitStealResolutionPanel == null || IsGameOver) return;

        if (p1NameText != null) p1NameText.text = p1Name;
        if (p1ChoiceText != null)
        {
            p1ChoiceText.text = p1Split ? "SPLIT" : "STEAL";
            p1ChoiceText.color = p1Split ? Color.green : Color.red;
        }

        if (p2NameText != null) p2NameText.text = p2Name;
        if (p2ChoiceText != null)
        {
            p2ChoiceText.text = p2Split ? "SPLIT" : "STEAL";
            p2ChoiceText.color = p2Split ? Color.green : Color.red;
        }

        if (splitStealResultText != null) splitStealResultText.text = result;
        splitStealResolutionPanel.SetActive(true);
    }

    public void HideSplitStealResolution()
    {
        if (splitStealResolutionPanel != null)
            splitStealResolutionPanel.SetActive(false);
    }

    public void ShowGambleUI(GameObject target)
    {
        if (IsGameOver) return;
        currentTarget = target;
        gamblePanel.SetActive(true);

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        GrimPlayerManager.BlockCameraRotation = true;
    }

    private void HideGambleUI()
    {
        gamblePanel.SetActive(false);

        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        GrimPlayerManager.BlockCameraRotation = false;
    }

    public void OnClickGamble()
    {
        Debug.Log("Jucătorul a ales GAMBLE");
        HideGambleUI();
        GambleAction.ProcessGambleResult(currentTarget, true);
    }

    public void OnClickSkip()
    {
        Debug.Log("Jucătorul a ales SKIP");
        HideGambleUI();
        GambleAction.ProcessGambleResult(currentTarget, false);
    }

    public void ShowSelectionUI(bool show)
    {
        if (IsGameOver) return;
        selectionInstructionUI.SetActive(show);

    }

    public void AddLog(string message)
    {
        if (GameLogManager.Instance != null)
        {
            GameLogManager.Instance.AddLog(message);
        }
    }

    public void ShowSixthSenseUI(string eventName, float duration)
    {
        if (sixthSensePanel == null || IsGameOver) return;

        if (sixthSenseTitle != null)
            sixthSenseTitle.text = "Next Event: " + eventName;

        sixthSensePanel.SetActive(true);
        StartCoroutine(HideSixthSenseAfterDelay(duration));
    }

    private IEnumerator HideSixthSenseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sixthSensePanel != null)
            sixthSensePanel.SetActive(false);
    }

    public void ShowSpyUI(string targetName, List<Sprite> icons, List<string> cardNames, float duration)
    {
        if (spyPanel == null || IsGameOver) return;

        if (spyTitle != null)
            spyTitle.text = targetName + "'s Hand";

        if (spyIconContainer != null)
        {
            foreach (Transform child in spyIconContainer)
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log($"[SpyUI] Încerc să creez {icons.Count} iconițe. Prefab: {spyIconPrefab != null}, Container: {spyIconContainer != null}");

        for (int i = 0; i < icons.Count; i++)
        {
            if (spyIconPrefab == null || spyIconContainer == null) 
            {
                Debug.LogError("[SpyUI] EROARE: spyIconPrefab sau spyIconContainer este NULL!");
                break;
            }

            GameObject iconObj = Instantiate(spyIconPrefab, spyIconContainer);
            Debug.Log($"[SpyUI] Am instanțiat cu succes iconița {i}!");

            Transform iconTrans = iconObj.transform.Find("IconImage");
            Image iconImage = iconTrans != null ? iconTrans.GetComponent<Image>() : iconObj.GetComponentInChildren<Image>();
            
            if (iconImage != null && icons[i] != null)
            {
                iconImage.sprite = icons[i];
                iconImage.enabled = true;
            }

            Transform textTrans = iconObj.transform.Find("IconTitle");
            TMPro.TextMeshProUGUI nameText = textTrans != null ? textTrans.GetComponent<TMPro.TextMeshProUGUI>() : iconObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            
            if (nameText != null)
            {
                nameText.text = cardNames[i];
            }
}

        spyPanel.SetActive(true);
        StartCoroutine(HideSpyAfterDelay(duration));
    }

    private IEnumerator HideSpyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (spyPanel != null)
            spyPanel.SetActive(false);
    }

}
