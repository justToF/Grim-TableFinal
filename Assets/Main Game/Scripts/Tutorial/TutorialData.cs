using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ScriptedRound
{
    public EntityAction action;
    public string targetParticipantName;
    public string secondaryTargetParticipantName;
    public string specialRoundID;
}

[CreateAssetMenu(fileName = "TutorialData", menuName = "Grim Table/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    public List<ParticipantInitialSetup> initialSetups;
    public List<ScriptedRound> scriptedRounds;
}
