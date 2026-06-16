using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI conditionText;
    public TextMeshProUGUI expiryText;
    public Image iconImage;

    public CardData assignedData;
    public GameObject highlightEffect;
    private Button cardButton;
    private bool isRaised = false;
    private float lastClickTime = 0f;

    public void UpdateDisplay(CardData data)
    {
        assignedData = data;
        if (highlightEffect != null) highlightEffect.SetActive(false);
        isRaised = false;

        nameText.text = data.cardName;
        descriptionText.text = data.description;
        typeText.text = data.isUsable ? "USABLE" : "NON-USABLE";
        conditionText.text = data.needsTarget ? "Target Required" : "";

        if (data.cardName == "Shield")
            expiryText.text = "∞";
        else
            expiryText.text = "" + data.roundsUntilExpiry;

        if (iconImage != null)
        {
            if (data.cardIcon != null)
            {
                iconImage.sprite = data.cardIcon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        cardButton = GetComponent<Button>();
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            if (data.isUsable)
            {
                cardButton.onClick.AddListener(OnCardClicked);
            }

        }
    }

    public void OnCardClicked()
    {
        if (Time.time - lastClickTime < 0.1f) return;
        lastClickTime = Time.time;

        if (assignedData == null || !assignedData.isUsable) return;

        Debug.Log("Click pe carta: " + assignedData.cardName);
        
        if (GrimPlayerManager.Instance != null)
        {
            Debug.Log("Apelez GrimPlayerManager.OnCardClicked...");
            GrimPlayerManager.Instance.OnCardClicked(this);
        }
        else
        {
            Debug.LogError("GrimPlayerManager.Instance este NULL!");
        }
    }

    public void SetRaised(bool raise)
    {
        isRaised = raise;
        CardFanController fanCtrl = GetComponent<CardFanController>();
        if (fanCtrl != null)
        {
            fanCtrl.SetSelected(raise);
        }
    }

    public void ResetCard()
    {
        SetRaised(false);
        if (highlightEffect != null) highlightEffect.SetActive(false);
    }
}
