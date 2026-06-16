using UnityEngine;

[CreateAssetMenu(fileName = "New Reload Card", menuName = "Cards/Reload")]
public class ReloadCard : CardData
{

    public static bool CanUseReload(GameObject user)
    {
        if (user == null) return false;
        Participant p = user.GetComponent<Participant>();
        return p != null && p.lastPlayedCard != null;
    }

    public override void Execute(GameObject user, GameObject target)
    {
        Participant userPart = user.GetComponent<Participant>();

        if (userPart == null || userPart.lastPlayedCard == null)
        {
            Debug.Log("<color=yellow>Reload BLOCAT:</color> Nu există nicio carte anterioară pentru Reload.");
            return;
        }

        userPart.AddCardToHand(userPart.lastPlayedCard);
        Debug.Log("Reload: " + userPart.participantName + " a recuperat " + userPart.lastPlayedCard.cardName);
        UIManager.Instance.AddLog(userPart.participantName + " used RELOAD!");

        int newCardIndex = userPart.hand.Count - 1;
        GameManager.Instance.StartCoroutine(
            GameManager.Instance.AnimateDealCardToParticipant(user, newCardIndex, userPart.lastPlayedCard));
    }
}
