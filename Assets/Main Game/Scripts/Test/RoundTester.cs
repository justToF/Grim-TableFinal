using UnityEngine;
using System.Collections;

public class RoundTester : MonoBehaviour
{
    public static RoundTester Instance;
    public System.Collections.Generic.List<CardData> allAvailableCards;

    void Awake() => Instance = this;

    IEnumerator Start()
    {

        yield return null;

        GiveInitialCards(2);
    }

    public void GiveInitialCards(int count)
    {
        if (GameManager.Instance == null) return;

        foreach (GameObject pObj in GameManager.Instance.participants)
        {
            Participant pScript = pObj.GetComponent<Participant>();
            if (pScript != null)
            {
                for (int i = 0; i < count; i++)
                {
                    CardData randomCard = GetRandomCardAsset();
                    pScript.AddCardToHand(randomCard);
                }
            }
        }
    }

    public CardData GetRandomCardAsset()
    {
        if (allAvailableCards.Count == 0) return null;
        return allAvailableCards[Random.Range(0, allAvailableCards.Count)];
    }
}
