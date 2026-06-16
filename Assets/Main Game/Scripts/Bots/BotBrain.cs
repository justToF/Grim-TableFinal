using UnityEngine;
using System.Collections.Generic;

public class BotBrain : MonoBehaviour
{
    private Participant self;

    void Awake() => self = GetComponent<Participant>();

    public bool MakeBinaryDecision()
    {

        return Random.value > 0.5f;
    }
    public bool DecideSplitOrSteal()
    {

        return Random.value > 0.5f;
    }

    public GameObject ChooseTarget(List<GameObject> possibleTargets)
    {

        List<GameObject> validTargets = possibleTargets.FindAll(t => t != this.gameObject && t.GetComponent<Participant>().isAlive && t.GetComponent<Participant>().isTargetable);

        if (validTargets.Count == 0) return null;

        BotGrudgeSystem grudge = GetComponent<BotGrudgeSystem>();
        if (grudge != null && grudge.HasAnyGrudge())
        {
            return grudge.ChooseTargetWithGrudge(validTargets);
        }

        return validTargets[Random.Range(0, validTargets.Count)];
    }

    public CardData ChooseCardToPlay()
    {
        if (self.hand.Count > 0)
            return self.hand[Random.Range(0, self.hand.Count)];
        return null;
    }

    public GameObject ChooseTargetForCard(CardData card, List<GameObject> possibleTargets)
    {
        List<GameObject> validTargets = possibleTargets.FindAll(t =>
            t != this.gameObject &&
            t.GetComponent<Participant>() != null &&
            t.GetComponent<Participant>().isAlive &&
            t.GetComponent<Participant>().isTargetable);

        if (card is ThiefCard || card is DeckSwapCard)
        {
            validTargets = validTargets.FindAll(t =>
                t.GetComponent<Participant>().hand.Count > 0);
        }

        if (validTargets.Count == 0) return null;

        if (card is ThiefCard)
        {
            validTargets.Sort((a, b) =>
                b.GetComponent<Participant>().hand.Count.CompareTo(
                a.GetComponent<Participant>().hand.Count));
            return validTargets[0];
        }

        if (card is DeckSwapCard)
        {
            validTargets.Sort((a, b) =>
                BotCardEvaluator.EvaluateHandQuality(
                    b.GetComponent<Participant>().hand).CompareTo(
                BotCardEvaluator.EvaluateHandQuality(
                    a.GetComponent<Participant>().hand)));
            return validTargets[0];
        }

        return validTargets[Random.Range(0, validTargets.Count)];
    }
}
