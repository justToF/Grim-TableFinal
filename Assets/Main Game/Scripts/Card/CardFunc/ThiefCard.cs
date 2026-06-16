using UnityEngine;

[CreateAssetMenu(fileName = "New Thief Card", menuName = "Cards/Thief")]
public class ThiefCard : CardData
{
    public override void Execute(GameObject user, GameObject target)
    {
        Participant targetPart = target.GetComponent<Participant>();
        Participant userPart = user.GetComponent<Participant>();

        if (targetPart == null || userPart == null) return;

        if (targetPart.hand.Count == 0)
        {
            Debug.Log("<color=yellow>THIEF BLOCAT:</color> " + targetPart.participantName + " nu are cărți în mână!");
            return;
        }

        int randomIndex = Random.Range(0, targetPart.hand.Count);
        CardData stolenCard = targetPart.hand[randomIndex];

        Debug.Log(userPart.participantName + " a furat " + stolenCard.cardName + " de la " + targetPart.participantName);
        UIManager.Instance.AddLog(userPart.participantName + " used Thief on " + targetPart.participantName);

        targetPart.RemoveCardFromHand(stolenCard);

        BotGrudgeSystem targetGrudge = target.GetComponent<BotGrudgeSystem>();
        if (targetGrudge != null) targetGrudge.OnCardThieved(user);

        userPart.AddCardToHand(stolenCard);

        int newCardIndex = userPart.hand.Count - 1;
        GameManager.Instance.StartCoroutine(
            GameManager.Instance.AnimateCardTransfer(target, user, newCardIndex, stolenCard));
    }
}
