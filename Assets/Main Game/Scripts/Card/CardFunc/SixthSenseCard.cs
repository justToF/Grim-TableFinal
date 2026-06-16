using UnityEngine;

[CreateAssetMenu(fileName = "New Sixth Sense Card", menuName = "Cards/Sixth Sense")]
public class SixthSenseCard : CardData
{
    public float displayDuration = 5f;

    public override void Execute(GameObject user, GameObject target)
    {
        Participant userPart = user.GetComponent<Participant>();
        if (userPart == null) return;

        var possibleActions = GameManager.Instance.possibleActions;
        if (possibleActions == null || possibleActions.Count == 0)
        {
            Debug.Log("<color=yellow>SIXTH SENSE:</color> Nu există evenimente posibile!");
            return;
        }

        EntityAction nextAction = possibleActions[Random.Range(0, possibleActions.Count)];

        Debug.Log("<color=cyan>SIXTH SENSE:</color> " + userPart.participantName + " prevede: " + nextAction.actionName);

        if (UIManager.Instance != null && !userPart.isBot)
        {
            UIManager.Instance.ShowSixthSenseUI(nextAction.actionName, displayDuration);
        }
    }
}
