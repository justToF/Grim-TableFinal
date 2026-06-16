using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Gamble Action", menuName = "Entity/Actions/Gamble")]
public class GambleAction : EntityAction
{
    public override void ExecuteAction(GameObject target)
    {
        BotBrain bot = target.GetComponent<BotBrain>();

        if (bot != null)
        {

            bool decideSaRiste = bot.MakeBinaryDecision();
            ProcessGambleResult(target, decideSaRiste);
        }
        else
        {

            UIManager.Instance.ShowGambleUI(target);
        }
    }

    public static void ProcessGambleResult(GameObject target, bool choseGamble)
    {
        GameManager.Instance.StartCoroutine(ProcessGambleResultRoutine(target, choseGamble));
    }

    private static IEnumerator ProcessGambleResultRoutine(GameObject target, bool choseGamble)
    {
        if (!choseGamble)
        {
            Debug.Log("<color=yellow>SKIP:</color> " + target.name + " a decis să nu riște.");
            GameManager.Instance.StartPostEventWindow();
        }
        else
        {

            if (Random.value > 0.5f)
            {
                Debug.Log("<color=green>WIN:</color> " + target.name + " a câștigat! Primește o carte.");
                
                List<CardData> pool = GameManager.Instance.cardPool;
                if (pool != null && pool.Count > 0)
                {
                    CardData randomCard = pool[Random.Range(0, pool.Count)];
                    Participant part = target.GetComponent<Participant>();
                    
                    if (part != null)
                    {
                        part.AddCardToHand(randomCard);
                        int cardIndex = part.hand.Count - 1;
                        yield return GameManager.Instance.StartCoroutine(
                            GameManager.Instance.AnimateDealCardToParticipant(target, cardIndex, randomCard)
                        );
                        yield return new WaitForSeconds(GameManager.Instance.dealCardInterval);
                    }
                }
                
                GameManager.Instance.StartPostEventWindow();
            }
            else
            {
                Debug.Log("<color=red>LOSS:</color> " + target.name + " a pierdut! Entitatea trage.");
                
                GameObject dealer = GameObject.Find("grim");
                
                EntityController entityCtrl = GameManager.Instance.entityVisual;
                if (entityCtrl != null)
                {
                    entityCtrl.StopAndLookAt(target.transform.position);
                    yield return new WaitForSeconds(1.6f);
                }

                ShootAction shootAction = null;
                if (GameManager.Instance.possibleActions != null)
                {
                    shootAction = GameManager.Instance.possibleActions.Find(a => a is ShootAction) as ShootAction;
                }

                if (shootAction != null && dealer != null)
                {
                    yield return GameManager.Instance.StartCoroutine(
                        shootAction.ExecuteActionAnimated(dealer, target, true)
                    );
                }
                else
                {
                    Debug.LogWarning("ShootAction sau Grim nu au fost găsite! Executăm animația manual (Fallback).");
                    
                    GrimWeaponHandler weaponHandler = dealer != null ? dealer.GetComponent<GrimWeaponHandler>() : null;
                    Animator grimAnim = dealer != null ? dealer.GetComponent<Animator>() : null;
                    
                    if (weaponHandler != null) weaponHandler.SetTarget(target);

                    if (grimAnim != null) grimAnim.SetTrigger("PrepareShot");
                    yield return new WaitForSeconds(3f); 

                    if (grimAnim != null) grimAnim.SetTrigger("FireShot");
                    yield return new WaitForSeconds(2f);

                    if (grimAnim != null) grimAnim.SetTrigger("ReturnIdle");
                    yield return new WaitForSeconds(1.5f);

                    GameManager.Instance.StartPostEventWindow();
                }
            }
        }
    }
}
