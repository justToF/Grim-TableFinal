using TMPro;
using UnityEngine;

public class WorldCardVisual : MonoBehaviour
{
    [Header("World Visual References")]
    public Renderer frontRenderer;
    public string frontTextureProperty = "_BaseMap";
    public TextMeshPro cardNameText;
    public TextMeshPro cardDescriptionText;
    
    [Header("Statistici Carte")]
    public TextMeshPro expiryText; 
    
    [Header("Iconița Cărții")]
    public SpriteRenderer iconRenderer;

    public void ApplyCardData(CardData data)
    {
        if (data == null) return;

        if (frontRenderer != null && data.worldFrontTexture != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            frontRenderer.GetPropertyBlock(block);
            block.SetTexture(frontTextureProperty, data.worldFrontTexture);
            frontRenderer.SetPropertyBlock(block);
        }

        if (cardNameText != null) cardNameText.text = data.cardName;
        if (cardDescriptionText != null) cardDescriptionText.text = data.description;

        if (iconRenderer != null)
        {
            if (data.cardIcon != null)
            {
                iconRenderer.sprite = data.cardIcon;
                iconRenderer.enabled = true;
            }
            else
            {
                iconRenderer.enabled = false;
            }
        }

        if (expiryText != null)
        {
            if (data.roundsUntilExpiry > 0 && data.roundsUntilExpiry != 99) 
            {
                expiryText.text = data.roundsUntilExpiry.ToString();
            }
            else if (data.roundsUntilExpiry == 99)
            {
                expiryText.text = "∞"; 
            }
        }
    }
}
