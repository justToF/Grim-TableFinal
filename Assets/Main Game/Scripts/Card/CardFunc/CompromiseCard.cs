using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Compromise Card", menuName = "Cards/Compromise")]
public class CompromiseCard : CardData
{
    public override void Execute(GameObject user, GameObject target)
    {

        Participant userPart = user.GetComponent<Participant>();
        Participant targetPart = target.GetComponent<Participant>();

        if (userPart != null && targetPart != null)
        {
            List<CardData> pool = GameManager.Instance.cardPool;
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("[CompromiseCard] cardPool-ul din GameManager este gol!");
                return;
            }

            CardData cardForUser = pool[Random.Range(0, pool.Count)];
            CardData cardForTarget = pool[Random.Range(0, pool.Count)];

            userPart.AddCardToHand(cardForUser);
            targetPart.AddCardToHand(cardForTarget);

            GameManager.Instance.StartCoroutine(GameManager.Instance.AnimateDealCardToParticipant(userPart.gameObject, userPart.hand.Count - 1, cardForUser));
            GameManager.Instance.StartCoroutine(GameManager.Instance.AnimateDealCardToParticipant(targetPart.gameObject, targetPart.hand.Count - 1, cardForTarget));

            Debug.Log("Compromise: " + userPart.participantName + " si " + targetPart.participantName + " au primit carti noi.");
            UIManager.Instance.AddLog(userPart.participantName + " used COMPROMISE with " + targetPart.participantName);
        }
    }
}
