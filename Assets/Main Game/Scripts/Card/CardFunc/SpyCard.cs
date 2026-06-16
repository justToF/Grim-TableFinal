using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Spy Card", menuName = "Cards/Spy")]
public class SpyCard : CardData
{
    public float displayDuration = 5f;

    public override void Execute(GameObject user, GameObject target)
    {
        Participant targetPart = target.GetComponent<Participant>();
        Participant userPart = user.GetComponent<Participant>();

        if (targetPart == null || userPart == null) return;

        if (targetPart.hand.Count == 0)
        {
            Debug.Log("<color=yellow>SPY:</color> " + targetPart.participantName + " nu are cărți în mână!");
            return;
        }

        Debug.Log("<color=cyan>SPY:</color> " + userPart.participantName + " vede cărțile lui " + targetPart.participantName);
        UIManager.Instance.AddLog(userPart.participantName + " used SPY on " + targetPart.participantName);

        List<Sprite> icons = new List<Sprite>();
        List<string> names = new List<string>();
        foreach (CardData card in targetPart.hand)
        {
            icons.Add(card.cardIcon);
            names.Add(card.cardName);
        }

        if (UIManager.Instance != null && !userPart.isBot)
        {
            UIManager.Instance.ShowSpyUI(targetPart.participantName, icons, names, displayDuration);
        }
    }
}
