using UnityEngine;
using System.Collections.Generic;

public class BotGrudgeSystem : MonoBehaviour
{
    [Header("Tuning")]
    [Tooltip("Cât de mult contează ura vs. alegerea random (0 = ignoră ura, 1 = doar ură)")]
    [Range(0f, 1f)] public float grudgeInfluence = 0.65f;

    [Tooltip("Cât se degradează ura la fiecare rundă (ex: 0.85 = pierde 15% per rundă)")]
    [Range(0.5f, 1f)] public float grudgeDecayRate = 0.85f;

    [Header("Grudge Points per Event")]
    [Tooltip("Ura primită când cineva îți aruncă bomba (Hot Potato)")]
    public float hotPotatoGrudge = 30f;

    [Tooltip("Ura primită când cineva trage în tine (Russian Roulette, chiar și click gol)")]
    public float rouletteGrudge = 25f;

    [Tooltip("Ura primită când cineva votează împotriva ta (Vote1Out)")]
    public float voteGrudge = 20f;

    [Tooltip("Ura primită când cineva alege STEAL și tu ai ales SPLIT")]
    public float stealGrudge = 40f;

    [Tooltip("Ura primită când cineva îți fură o carte (Thief)")]
    public float thiefCardGrudge = 25f;

    [Tooltip("Ura primită când cineva îți schimbă pachetul forțat (DeckSwap)")]
    public float deckSwapGrudge = 20f;

    private Dictionary<GameObject, float> grudgeMap = new Dictionary<GameObject, float>();

    private Participant self;

    void Awake()
    {
        self = GetComponent<Participant>();
    }

    public void AddGrudge(GameObject aggressor, float amount)
    {
        if (aggressor == null || aggressor == this.gameObject) return;

        if (!grudgeMap.ContainsKey(aggressor))
            grudgeMap[aggressor] = 0f;

        grudgeMap[aggressor] += amount;

        string aggressorName = aggressor.GetComponent<Participant>() != null
            ? aggressor.GetComponent<Participant>().participantName
            : aggressor.name;

        Debug.Log("<color=red>[GRUDGE]</color> " + self.participantName +
                  " +(" + amount + " ură) față de " + aggressorName +
                  " → TOTAL: " + grudgeMap[aggressor].ToString("F0"));
    }

    public void OnBombReceived(GameObject thrower)   => AddGrudge(thrower, hotPotatoGrudge);
    public void OnShotAt(GameObject shooter)         => AddGrudge(shooter, rouletteGrudge);
    public void OnVotedAgainst(GameObject voter)     => AddGrudge(voter, voteGrudge);
    public void OnStolenFrom(GameObject stealer)     => AddGrudge(stealer, stealGrudge);
    public void OnCardThieved(GameObject thief)      => AddGrudge(thief, thiefCardGrudge);
    public void OnDeckSwapped(GameObject swapper)     => AddGrudge(swapper, deckSwapGrudge);

    public void DecayGrudges()
    {
        List<GameObject> toRemove = new List<GameObject>();

        List<GameObject> keys = new List<GameObject>(grudgeMap.Keys);
        foreach (GameObject key in keys)
        {
            grudgeMap[key] *= grudgeDecayRate;

            if (grudgeMap[key] < 1f)
                toRemove.Add(key);
        }

        foreach (GameObject key in toRemove)
            grudgeMap.Remove(key);
    }

    public GameObject ChooseTargetWithGrudge(List<GameObject> validTargets)
    {
        if (validTargets == null || validTargets.Count == 0) return null;
        if (validTargets.Count == 1) return validTargets[0];

        float[] weights = new float[validTargets.Count];
        float baseWeight = 10f;

        for (int i = 0; i < validTargets.Count; i++)
        {
            float grudge = GetGrudge(validTargets[i]);

            weights[i] = baseWeight + (grudge * grudgeInfluence);
        }

        float totalWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
            totalWeight += weights[i];

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < validTargets.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return validTargets[i];
        }

        return validTargets[validTargets.Count - 1];
    }

    public float GetGrudge(GameObject target)
    {
        if (target == null) return 0f;
        return grudgeMap.ContainsKey(target) ? grudgeMap[target] : 0f;
    }

    public GameObject GetMostHated()
    {
        GameObject mostHated = null;
        float maxGrudge = 0f;

        foreach (var kvp in grudgeMap)
        {
            if (kvp.Key != null && kvp.Value > maxGrudge)
            {
                Participant part = kvp.Key.GetComponent<Participant>();
                if (part != null && part.isAlive)
                {
                    mostHated = kvp.Key;
                    maxGrudge = kvp.Value;
                }
            }
        }

        return mostHated;
    }

    public bool HasAnyGrudge(float threshold = 5f)
    {
        foreach (var kvp in grudgeMap)
        {
            if (kvp.Key != null && kvp.Value >= threshold)
            {
                Participant part = kvp.Key.GetComponent<Participant>();
                if (part != null && part.isAlive)
                    return true;
            }
        }
        return false;
    }
}
