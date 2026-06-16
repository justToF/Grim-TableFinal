using UnityEngine;

public class CardEffectManager : MonoBehaviour
{
    public GrimPlayerManager inventory;
    private CardData lastUsedCard;

    public void UseCard(CardData card, GameObject target = null)
    {
        switch (card.cardName)
        {
            case "Thief":
                if (target != null) StealCard(target);
                break;

            case "Ghost":
                if (CheckMinPlayers(3)) ActivateGhostEffect();
                break;

            case "Reload":

                if (lastUsedCard != null) inventory.AddCard(lastUsedCard);
                else Debug.Log("Nu există nicio carte folosită anterior!");
                break;

            case "Deck Swap":
                if (target != null) SwapDecks(target);
                break;

            case "Sixth Sense":
                ShowNextEvent();
                break;
        }

        if (card.cardName != "Reload") lastUsedCard = card;

        inventory.RemoveCard(card);
    }

    void StealCard(GameObject target) { Debug.Log("Ai furat de la " + target.name); }
    bool CheckMinPlayers(int min) { return true; }
    void ActivateGhostEffect() { Debug.Log("Ești invizibil."); }
    void SwapDecks(GameObject target) { Debug.Log("Swap cu " + target.name); }
    void ShowNextEvent() { Debug.Log("Următorul event..."); }
}
