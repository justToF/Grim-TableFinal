using UnityEngine;
using System.Collections.Generic;

public class BotCardStrategy : MonoBehaviour
{
    [Header("Tuning")]
    [Tooltip("Sub acest scor, botul nu joacă nicio carte")]
    public float minScoreToPlay = 25f;

    [Tooltip("Multiplier pentru șansa de a juca (1 = normal, 0.5 = pasiv, 1.5 = agresiv)")]
    public float playChanceMultiplier = 1f;

    [Tooltip("Cât de lacom e botul (0 = conservator, 1 = joacă tot ce poate)")]
    [Range(0f, 1f)] public float greediness = 0.5f;

    [Header("Spy Memory")]
    [Tooltip("Câte cicluri de gândire ține minte informația de la Spy")]
    public int spyMemoryDuration = 3;

    private GameObject spiedTarget;
    private List<CardData> spiedHand;
    private int spyMemoryRoundsLeft = 0;

    private Participant self;
    private BotBrain brain;

    void Awake()
    {
        self = GetComponent<Participant>();
        brain = GetComponent<BotBrain>();
    }

    public CardPlayDecision EvaluateHand()
    {
        CardPlayDecision noPlay = new CardPlayDecision
        {
            cardToPlay = null,
            target = null,
            confidence = 0f
        };

        if (self == null || self.hand.Count == 0) return noPlay;

        List<GameObject> participants = GameManager.Instance.participants;

        ValidateSpyMemory();

        List<CardScorePair> scoredCards = new List<CardScorePair>();

        foreach (CardData card in self.hand)
        {

            if (card is GhostCard && !GhostCard.CanUseGhost()) continue;

            if (card is ReloadCard && !ReloadCard.CanUseReload(self.gameObject)) continue;

            float score = BotCardEvaluator.EvaluateCard(
                card, self, participants, spiedTarget, spiedHand);

            score *= playChanceMultiplier;

            if (score >= minScoreToPlay)
            {
                scoredCards.Add(new CardScorePair { card = card, score = score });
            }
        }

        if (scoredCards.Count == 0) return noPlay;

        scoredCards.Sort((a, b) => b.score.CompareTo(a.score));

        CardData chosen = PickCardWithRandomness(scoredCards);
        if (chosen == null) return noPlay;

        GameObject target = null;
        if (chosen.needsTarget)
        {
            target = ChooseBestTarget(chosen, participants);
            if (target == null) return noPlay;
        }

        float confidence = scoredCards[0].score / 100f;

        return new CardPlayDecision
        {
            cardToPlay = chosen,
            target = target,
            confidence = Mathf.Clamp01(confidence)
        };
    }

    public void ExecuteDecision(CardPlayDecision decision)
    {
        if (decision.cardToPlay == null) return;

        CardData card = decision.cardToPlay;
        GameObject target = decision.target != null ? decision.target : self.gameObject;

        string targetName = target.GetComponent<Participant>() != null
            ? target.GetComponent<Participant>().participantName
            : target.name;

        Debug.Log("<color=orange>[BOT STRATEGIE]</color> " + self.participantName +
                  " joacă <b>" + card.cardName + "</b>" +
                  (decision.target != null ? " pe " + targetName : "") +
                  " (încredere: " + (decision.confidence * 100f).ToString("F0") + "%)");

        if (card is SpyCard && decision.target != null)
        {
            Participant targetPart = decision.target.GetComponent<Participant>();
            if (targetPart != null && targetPart.hand.Count > 0)
            {
                RememberSpiedHand(decision.target, new List<CardData>(targetPart.hand));
                Debug.Log("<color=cyan>[BOT SPY COMBO]</color> " + self.participantName +
                          " a memorat " + targetPart.hand.Count + " cărți de la " +
                          targetPart.participantName + ". Va folosi Thief/DeckSwap data viitoare!");
            }
        }

        if (card is SixthSenseCard)
        {
            Debug.Log("<color=cyan>[BOT SIXTH SENSE]</color> " + self.participantName +
                      " a folosit Sixth Sense (cosmetic — fără efect strategic).");
        }

        card.Execute(self.gameObject, target);

        if (!(card is ReloadCard))
        {
            self.lastPlayedCard = card;
        }

        self.RemoveCardFromHand(card);

        if ((card is ThiefCard || card is DeckSwapCard) && decision.target == spiedTarget)
        {
            ClearSpyMemory();
            Debug.Log("<color=cyan>[BOT SPY COMBO]</color> " + self.participantName +
                      " a folosit info-ul Spy pe " + targetName + ". Memoria ștearsă.");
        }
    }

    private GameObject ChooseBestTarget(CardData card, List<GameObject> participants)
    {

        if (spiedTarget != null && spiedHand != null &&
            (card is ThiefCard || card is DeckSwapCard))
        {
            Participant spiedPart = spiedTarget.GetComponent<Participant>();
            if (spiedPart != null && spiedPart.isAlive &&
                spiedPart.isTargetable && spiedPart.hand.Count > 0)
            {
                return spiedTarget;
            }
        }

        if (brain != null)
        {
            return brain.ChooseTargetForCard(card, participants);
        }

        return PickRandomValidTarget(card, participants);
    }

    private GameObject PickRandomValidTarget(CardData card, List<GameObject> participants)
    {
        List<GameObject> valid = new List<GameObject>();
        foreach (var p in participants)
        {
            if (p == null || p == self.gameObject) continue;
            Participant part = p.GetComponent<Participant>();
            if (part == null || !part.isAlive || !part.isTargetable) continue;

            if ((card is ThiefCard || card is DeckSwapCard) && part.hand.Count == 0) continue;

            valid.Add(p);
        }

        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }

    private CardData PickCardWithRandomness(List<CardScorePair> sorted)
    {
        if (sorted.Count == 0) return null;
        if (sorted.Count == 1) return sorted[0].card;

        if (Random.value < greediness || sorted.Count <= 2)
        {
            return sorted[0].card;
        }

        int topN = Mathf.Min(3, sorted.Count);
        float totalScore = 0f;
        for (int i = 0; i < topN; i++) totalScore += sorted[i].score;

        float roll = Random.Range(0f, totalScore);
        float cumulative = 0f;
        for (int i = 0; i < topN; i++)
        {
            cumulative += sorted[i].score;
            if (roll <= cumulative) return sorted[i].card;
        }

        return sorted[0].card;
    }

    public void RememberSpiedHand(GameObject target, List<CardData> hand)
    {
        spiedTarget = target;
        spiedHand = hand;
        spyMemoryRoundsLeft = spyMemoryDuration;
    }

    public void DecayMemory()
    {
        if (spyMemoryRoundsLeft > 0)
        {
            spyMemoryRoundsLeft--;
            if (spyMemoryRoundsLeft <= 0)
            {
                Debug.Log("<color=cyan>[BOT SPY]</color> " + self.participantName +
                          " a uitat informația de la Spy (memorie expirată).");
                ClearSpyMemory();
            }
        }
    }

    private void ValidateSpyMemory()
    {
        if (spiedTarget == null) return;

        Participant targetPart = spiedTarget.GetComponent<Participant>();
        if (targetPart == null || !targetPart.isAlive)
        {
            ClearSpyMemory();
        }
    }

    private void ClearSpyMemory()
    {
        spiedTarget = null;
        spiedHand = null;
        spyMemoryRoundsLeft = 0;
    }

    private struct CardScorePair
    {
        public CardData card;
        public float score;
    }
}

public struct CardPlayDecision
{
    public CardData cardToPlay;
    public GameObject target;
    public float confidence;
}
