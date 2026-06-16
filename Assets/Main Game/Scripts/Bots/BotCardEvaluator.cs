using UnityEngine;
using System.Collections.Generic;

public static class BotCardEvaluator
{

    public static float EvaluateCard(
        CardData card,
        Participant self,
        List<GameObject> allParticipants,
        GameObject spiedTarget,
        List<CardData> spiedHand)
    {

        if (!card.isUsable) return 0f;

        int aliveCount = CountAlive(allParticipants);
        bool hasShield = self.hand.Exists(c => c is ShieldCard);
        bool hasLastLaugh = self.hand.Exists(c => c is LastLaughCard);
        bool hasThiefInHand = self.hand.Exists(c => c is ThiefCard && c != card);
        bool hasDeckSwapInHand = self.hand.Exists(c => c is DeckSwapCard && c != card);
        bool hasFollowUp = hasThiefInHand || hasDeckSwapInHand;

        if (card is GhostCard)      return ScoreGhost(aliveCount, hasShield, hasLastLaugh);
        if (card is ThiefCard)      return ScoreThief(self, allParticipants, spiedTarget, spiedHand);
        if (card is DeckSwapCard)   return ScoreDeckSwap(self, allParticipants, spiedTarget, spiedHand);
        if (card is SpyCard)        return ScoreSpy(hasFollowUp, spiedTarget, allParticipants, self);
        if (card is SixthSenseCard) return ScoreSixthSense();
        if (card is ReloadCard)     return ScoreReload(self);
        if (card is CompromiseCard) return ScoreCompromise(self);

        return 5f;
    }

    private static float ScoreGhost(int aliveCount, bool hasShield, bool hasLastLaugh)
    {
        if (aliveCount < 3) return 0f;

        float score = 50f;
        if (!hasShield && !hasLastLaugh) score += 30f;
        if (aliveCount <= 4) score += 15f;

        return score;
    }

    private static float ScoreThief(
        Participant self,
        List<GameObject> allParticipants,
        GameObject spiedTarget,
        List<CardData> spiedHand)
    {

        if (!HasAnyTargetWithCards(self, allParticipants)) return 0f;

        float score = 35f;

        if (self.hand.Count <= 2) score += 15f;

        if (spiedTarget != null && spiedHand != null && spiedHand.Count > 0)
        {

            bool hasValuableCard = spiedHand.Exists(c =>
                c is ShieldCard || c is LastLaughCard || c is GhostCard);
            if (hasValuableCard) score += 35f;
            else score += 15f;
        }

        return score;
    }

    private static float ScoreDeckSwap(
        Participant self,
        List<GameObject> allParticipants,
        GameObject spiedTarget,
        List<CardData> spiedHand)
    {
        if (!HasAnyTargetWithCards(self, allParticipants)) return 0f;

        float score = 15f;

        float ownHandQuality = EvaluateHandQuality(self.hand);

        if (ownHandQuality < 30f) score += 20f;

        if (spiedTarget != null && spiedHand != null && spiedHand.Count > 0)
        {
            float spiedHandQuality = EvaluateHandQuality(spiedHand);
            if (spiedHandQuality > ownHandQuality + 20f)
                score += 40f;
            else if (spiedHandQuality > ownHandQuality)
                score += 20f;
        }

        return score;
    }

    private static float ScoreSpy(
        bool hasFollowUp,
        GameObject spiedTarget,
        List<GameObject> allParticipants,
        Participant self)
    {

        if (spiedTarget != null) return 5f;

        if (!HasAnyTargetWithCards(self, allParticipants)) return 0f;

        float score = 15f;

        if (hasFollowUp) score += 40f;

        return score;
    }

    private static float ScoreSixthSense()
    {
        return 26f;
    }

    private static float ScoreReload(Participant self)
    {
        if (self.lastPlayedCard == null) return 0f;

        float score = 25f;

        if (self.lastPlayedCard is ShieldCard) score += 30f;
        if (self.lastPlayedCard is LastLaughCard) score += 25f;
        if (self.lastPlayedCard is GhostCard) score += 20f;
        if (self.lastPlayedCard is ThiefCard) score += 10f;

        return score;
    }

    private static float ScoreCompromise(Participant self)
    {
        float score = 25f;

        if (self.hand.Count <= 2) score += 15f;

        return score;
    }

    private static int CountAlive(List<GameObject> participants)
    {
        int count = 0;
        foreach (var p in participants)
        {
            if (p != null)
            {
                Participant part = p.GetComponent<Participant>();
                if (part != null && part.isAlive) count++;
            }
        }
        return count;
    }

    private static bool HasAnyTargetWithCards(Participant self, List<GameObject> participants)
    {
        foreach (var p in participants)
        {
            if (p == null || p == self.gameObject) continue;
            Participant part = p.GetComponent<Participant>();
            if (part != null && part.isAlive && part.isTargetable && part.hand.Count > 0)
                return true;
        }
        return false;
    }

    public static float EvaluateHandQuality(List<CardData> hand)
    {
        if (hand == null || hand.Count == 0) return 0f;

        float quality = 0f;
        foreach (CardData card in hand)
        {
            if (card is ShieldCard)          quality += 35f;
            else if (card is LastLaughCard)  quality += 30f;
            else if (card is GhostCard)      quality += 25f;
            else if (card is ThiefCard)      quality += 15f;
            else if (card is DeckSwapCard)   quality += 12f;
            else if (card is SpyCard)        quality += 10f;
            else if (card is ReloadCard)     quality += 8f;
            else if (card is CompromiseCard) quality += 8f;
            else if (card is SixthSenseCard) quality += 5f;
            else                             quality += 5f;
        }

        return Mathf.Min(quality, 100f);
    }
}
