using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Ghost Card", menuName = "Cards/Ghost")]
public class GhostCard : CardData
{
    public int ghostDuration = 1;

    public static bool CanUseGhost()
    {
        int targetableCount = 0;
        foreach (GameObject p in GameManager.Instance.participants)
        {
            if (p == null) continue;
            Participant part = p.GetComponent<Participant>();
            if (part != null && part.isAlive && part.isTargetable && part.ghostRoundsRemaining <= 0)
                targetableCount++;
        }
        return targetableCount > 2;
    }

    public override void Execute(GameObject user, GameObject target)
    {
        if (!CanUseGhost())
        {
            Debug.Log("<color=grey>GHOST BLOCAT:</color> Prea puțini jucători targetable. Ghost nu poate fi folosit!");
            return;
        }

        Participant p = user.GetComponent<Participant>();
        if (p != null)
        {
            p.isTargetable = false;
            p.ghostRoundsRemaining = ghostDuration;
            Debug.Log("<color=grey>GHOST:</color> " + p.participantName + " este GHOST pentru " + ghostDuration + " runde. Nu poate fi țintit!");
            UIManager.Instance.AddLog(p.participantName + " used GHOST!");
        }
    }
}
