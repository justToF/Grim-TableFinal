using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Split Steal Action", menuName = "Entity/Actions/SplitOrSteal")]
public class SplitOrStealAction : EntityAction
{
    private bool choice1, choice2;
    private GameObject p1, p2;

    public override void ExecuteAction(GameObject firstTarget)
    {

        p1 = firstTarget;
        List<GameObject> others = GameManager.Instance.participants.FindAll(p =>
            p != p1 && p.GetComponent<Participant>().isAlive && p.GetComponent<Participant>().isTargetable);

        if (others.Count > 0)
        {
            p2 = GameManager.Instance.forcedSecondaryTarget != null ? GameManager.Instance.forcedSecondaryTarget : others[Random.Range(0, others.Count)];
            GameManager.Instance.StartCoroutine(RunSplitOrSteal());
        }
        else
        {
            GameManager.Instance.StartPostEventWindow();
        }
    }

    private IEnumerator RunSplitOrSteal()
    {
        Debug.Log("<color=cyan>SPLIT OR STEAL:</color> " + p1.name + " vs " + p2.name);

        yield return GetChoice(p1, result => choice1 = result);

        yield return GetChoice(p2, result => choice2 = result);

        ResolveSplitOrStealResults();
    }

    private IEnumerator GetChoice(GameObject target, System.Action<bool> callback)
    {
        BotBrain bot = target.GetComponent<BotBrain>();
        if (bot != null)
        {
            yield return new WaitForSeconds(Random.Range(1.5f, 3f));
            callback(bot.DecideSplitOrSteal());
        }
        else
        {
            bool waiting = true;
            GameObject opponent = (target == p1) ? p2 : p1;
            string participantName = target.GetComponent<Participant>() != null ? target.GetComponent<Participant>().participantName : target.name;
            string opponentName = opponent.GetComponent<Participant>() != null ? opponent.GetComponent<Participant>().participantName : opponent.name;

            UIManager.Instance.ShowSplitStealUI(participantName, opponentName, result => {
                callback(result);
                waiting = false;
            });
            while (waiting) yield return null;
        }
}

    private void ResolveSplitOrStealResults()
    {
        string resultMsg = "";
        if (choice1 && choice2) 
            resultMsg = "Both chose SPLIT: Each receives 1 card.";
        else if (choice1 && !choice2)
        {
            resultMsg = p2.name + " STOLE! " + p2.name + " receives 2 cards, " + p1.name + " receives nothing.";

            BotGrudgeSystem g1 = p1.GetComponent<BotGrudgeSystem>();
            if (g1 != null) g1.OnStolenFrom(p2);
        }
        else if (!choice1 && choice2)
        {
            resultMsg = p1.name + " STOLE! " + p1.name + " receives 2 cards, " + p2.name + " receives nothing.";

            BotGrudgeSystem g2 = p2.GetComponent<BotGrudgeSystem>();
            if (g2 != null) g2.OnStolenFrom(p1);
        }
        else 
            resultMsg = "Both chose STEAL: Nobody receives anything.";

        UIManager.Instance.ShowSplitStealResolution(p1.name, choice1, p2.name, choice2, resultMsg);
        GameManager.Instance.StartCoroutine(WaitAndAnimateRewards());
    }

    private IEnumerator WaitAndAnimateRewards()
    {
        yield return new WaitForSeconds(4f);
        UIManager.Instance.HideSplitStealResolution();
        GameManager.Instance.StartCoroutine(AnimateRewards());
    }

    private IEnumerator AnimateRewards()
{
        List<CardData> pool = GameManager.Instance.cardPool;

        if (choice1 && choice2)
        {
            Debug.Log("Amândoi SPLIT: Ambii primesc 1 carte.");
            yield return GiveCardFromDeck(p1, pool);
            yield return GiveCardFromDeck(p2, pool);
        }
        else if (choice1 && !choice2)
        {
            Debug.Log(p2.name + " a FURAT! Primește 2 cărți. " + p1.name + " nimic.");
            yield return GiveCardFromDeck(p2, pool);
            yield return GiveCardFromDeck(p2, pool);
        }
        else if (!choice1 && choice2)
        {
            Debug.Log(p1.name + " a FURAT! Primește 2 cărți. " + p2.name + " nimic.");
            yield return GiveCardFromDeck(p1, pool);
            yield return GiveCardFromDeck(p1, pool);
        }
        else
        {
            Debug.Log("Amândoi STEAL: Nimeni nu primește nimic.");
        }

        yield return new WaitForSeconds(2f);
        GameManager.Instance.StartPostEventWindow();
    }

    private IEnumerator GiveCardFromDeck(GameObject participant, List<CardData> pool)
    {
        if (pool == null || pool.Count == 0) yield break;

        Participant part = participant.GetComponent<Participant>();
        if (part == null) yield break;

        CardData randomCard = pool[Random.Range(0, pool.Count)];
        part.AddCardToHand(randomCard);

        int cardIndex = part.hand.Count - 1;
        yield return GameManager.Instance.AnimateDealCardToParticipant(participant, cardIndex, randomCard);
        yield return new WaitForSeconds(GameManager.Instance.dealCardInterval);
    }
}
