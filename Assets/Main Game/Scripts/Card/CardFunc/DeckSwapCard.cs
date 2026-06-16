using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Deck Swap Card", menuName = "Cards/Deck Swap")]
public class DeckSwapCard : CardData
{
    public override void Execute(GameObject user, GameObject target)
    {
        Participant userPart = user.GetComponent<Participant>();
        Participant targetPart = target.GetComponent<Participant>();

        if (userPart == null || targetPart == null) return;

        if (targetPart.hand.Count == 0)
        {
            Debug.Log("<color=yellow>DECK SWAP BLOCAT:</color> " + targetPart.participantName + " nu are cărți în mână!");
            return;
        }

        Debug.Log("<color=green>DECK SWAP:</color> " + userPart.participantName + " schimbă cărțile cu " + targetPart.participantName);
        UIManager.Instance.AddLog(userPart.participantName + " used DECK SWAP on " + targetPart.participantName);

        BotGrudgeSystem targetGrudge = target.GetComponent<BotGrudgeSystem>();
        if (targetGrudge != null) targetGrudge.OnDeckSwapped(user);

        List<CardData> tempUserHand = new List<CardData>(userPart.hand);
        tempUserHand.Remove(this);
        List<CardData> tempTargetHand = new List<CardData>(targetPart.hand);

        userPart.hand = new List<CardData>(tempTargetHand);
        targetPart.hand = new List<CardData>(tempUserHand);

        GrimPlayerManager userGPM = user.GetComponent<GrimPlayerManager>();
        GrimPlayerManager targetGPM = target.GetComponent<GrimPlayerManager>();

        if (userGPM != null)
            userGPM.ClearAllWorldCards();
        else
            userPart.ClearAllPhysicalCards();

        if (targetGPM != null)
            targetGPM.ClearAllWorldCards();
        else
            targetPart.ClearAllPhysicalCards();

        for (int i = 0; i < userPart.hand.Count; i++)
        {
            GameManager.Instance.StartCoroutine(
                GameManager.Instance.AnimateCardTransfer(target, user, i, userPart.hand[i]));
        }

        for (int i = 0; i < targetPart.hand.Count; i++)
        {
            GameManager.Instance.StartCoroutine(
                GameManager.Instance.AnimateCardTransfer(user, target, i, targetPart.hand[i]));
        }

        if (userGPM != null) userGPM.RefreshHandUI();
        if (targetGPM != null) targetGPM.RefreshHandUI();
    }
}
