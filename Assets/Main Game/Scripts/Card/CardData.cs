using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Grim Deal/Card")]
public abstract class CardData : ScriptableObject
{
    public string cardName;
    [TextArea(3, 10)]
    public string description;

    [Header("Visual")]
    public Texture2D worldFrontTexture;
    public Sprite cardIcon;

    [Header("Rules")]
    public bool isUsable;
    public int roundsUntilExpiry = 3;
    public bool disappearsAfterTrigger = true;

    [Header("Conditions")]
    public bool needsTarget;
    public bool min3PlayersRequired;
    public bool worksAsCounter;
    public abstract void Execute(GameObject user, GameObject target);
}
