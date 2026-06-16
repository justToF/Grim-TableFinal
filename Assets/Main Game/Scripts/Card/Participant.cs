using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Participant : MonoBehaviour
{
    public string participantName;
    public bool isAlive = true;
    public bool hasShield = false;
    public bool isTargetable = true;
    public int ghostRoundsRemaining = 0;

    [Header("Blood Effects")]
    public Vector3 bloodHoleOffset = new Vector3(0f, 0.1f, 0.2f);
    public Vector3 bloodHoleRotation = new Vector3(0f, 0f, 0f);

    [Header("Animatie Bot")]
    public Animator botAnimator;
    public bool isBot = true;
    private bool isSimulatingThinking = false;

    [Header("Carti in Mana (Nou)")]
    public Transform raisedCardsAnchor;
    public Vector3 raisedRotationEuler = new Vector3(90f, 180f, 0f);
    public float cardMoveSpeed = 10f;
    private List<GameObject> physicalCards = new List<GameObject>();

    [Header("Vizual Masa")]
    public GameObject shieldVfxPrefab;
    public Transform shieldPoint;
    public Material deadMaterial;
public Transform headPoint;
    public Transform cardTablePoint;
    public Vector3 cardFlatRotationEuler = new Vector3(90f, 180f, 0f);
    public float worldCardSpacing = 0.08f;
    public float worldCardCurve = 0.03f;
    public float worldCardYawStep = 7f;
    public float worldCardStackDepth = 0.0035f;

    [Header("Inventar")]
    public List<CardData> hand = new List<CardData>();
    public CardData lastPlayedCard;

    void Start()
    {
        if (botAnimator == null) botAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        UpdateShieldStatus();
        
        if (isBot)
        {
            if (GameManager.Instance.currentState == GameState.CardsPlay && isAlive)
            {
                if (!isSimulatingThinking) StartCoroutine(BotThinkingRoutine());
            }
            else if (isSimulatingThinking)
            {

                if (botAnimator != null) botAnimator.SetBool("isLookingAtCards", false);
                isSimulatingThinking = false;
            }
        }

        UpdatePhysicalCardsPosition();
    }

    public void RegisterPhysicalCard(GameObject cardObj)
    {
        if (cardObj != null && !physicalCards.Contains(cardObj))
        {
            physicalCards.Add(cardObj);
        }
    }

    public void ClearAllPhysicalCards()
    {
        foreach (GameObject card in physicalCards)
        {
            if (card != null) Destroy(card);
        }
        physicalCards.Clear();
    }

    private void UpdatePhysicalCardsPosition()
    {
        if (physicalCards.Count == 0) return;

        Transform targetAnchor = (isSimulatingThinking && raisedCardsAnchor != null) ? raisedCardsAnchor : cardTablePoint;
        if (targetAnchor == null) return;

        float halfCount = (physicalCards.Count - 1) * 0.5f;

        for (int i = 0; i < physicalCards.Count; i++)
        {
            GameObject card = physicalCards[i];
            if (card == null) continue;

            float indexFactor = i - halfCount;
            Vector3 localOffset = new Vector3(
                indexFactor * worldCardSpacing,           
                i * worldCardStackDepth,          
                -Mathf.Abs(indexFactor) * worldCardCurve  
            );

            Vector3 worldTarget = targetAnchor.TransformPoint(localOffset);
            Quaternion fanYaw = Quaternion.Euler(0f, indexFactor * worldCardYawStep, 0f);
            
            Vector3 eulerToUse = (isSimulatingThinking && raisedCardsAnchor != null) ? raisedRotationEuler : cardFlatRotationEuler;
            Quaternion targetRot = targetAnchor.rotation * fanYaw * Quaternion.Euler(eulerToUse);

            card.transform.position = Vector3.Lerp(card.transform.position, worldTarget, Time.deltaTime * cardMoveSpeed);
            card.transform.rotation = Quaternion.Slerp(card.transform.rotation, targetRot, Time.deltaTime * cardMoveSpeed);
        }
    }

    private IEnumerator BotThinkingRoutine()
    {
        isSimulatingThinking = true;
        BotCardStrategy strategy = GetComponent<BotCardStrategy>();
        yield return new WaitForSeconds(Random.Range(0.5f, 4.0f));

        while (GameManager.Instance.currentState == GameState.CardsPlay && isAlive)
        {

            if (botAnimator != null) botAnimator.SetBool("isLookingAtCards", true);

            yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));

            if (strategy != null && hand.Count > 0)
            {
                CardPlayDecision decision = strategy.EvaluateHand();
                if (decision.cardToPlay != null)
                {

                    yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

                    if (GameManager.Instance.currentState == GameState.CardsPlay && isAlive)
                    {
                        strategy.ExecuteDecision(decision);
                    }
                }
            }

            yield return new WaitForSeconds(Random.Range(1.5f, 3.0f));

            if (GameManager.Instance.currentState != GameState.CardsPlay || !isAlive) break;

            if (botAnimator != null) botAnimator.SetBool("isLookingAtCards", false);

            if (strategy != null) strategy.DecayMemory();
            BotGrudgeSystem grudge = GetComponent<BotGrudgeSystem>();
            if (grudge != null) grudge.DecayGrudges();

            yield return new WaitForSeconds(Random.Range(2.0f, 5.0f));
        }

        isSimulatingThinking = false;
    }

    void UpdateShieldStatus()
    {
        hasShield = hand.Exists(c => c.cardName == "Shield");
    }

    public void ResetAnimTriggers()
    {
        Animator anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim == null) return;

        anim.ResetTrigger("t_Catch");
        anim.ResetTrigger("t_Throw");
        anim.ResetTrigger("t_Reset");
        anim.ResetTrigger("ReturnToSit");
        anim.ResetTrigger("CatchPistol");
        anim.ResetTrigger("HoldPistol");
        anim.ResetTrigger("AimPistol");
        anim.ResetTrigger("ShootPistol");
        anim.ResetTrigger("ThrowPistol");
        anim.ResetTrigger("DieBullet");
        anim.ResetTrigger("GrenadeDeath");
        anim.ResetTrigger("PointFinger");
        anim.ResetTrigger("AimPistol");
    }

    public void AddCardToHand(CardData card)
    {
        if (card == null) return;
        CardData instance = Instantiate(card);
        hand.Add(instance);

        if (GetComponent<GrimPlayerManager>() != null)
            GetComponent<GrimPlayerManager>().RefreshHandUI();
    }

    public void RemoveCardFromHand(CardData card)
    {
        int index = hand.IndexOf(card);
        
        if (index >= 0)
        {
            hand.RemoveAt(index);
            
            GrimPlayerManager gpm = GetComponent<GrimPlayerManager>();
            if (gpm != null)
            {

                gpm.RemoveWorldCard(index);
            }
            else if (index < physicalCards.Count)
            {

                GameObject card3D = physicalCards[index];
                physicalCards.RemoveAt(index);
                
                if (card3D != null) 
                {
                    Destroy(card3D);
                }
            }
        }

        if (GetComponent<GrimPlayerManager>() != null)
            GetComponent<GrimPlayerManager>().RefreshHandUI();
    }

    public void ProcessCardExpiry()
    {

        for (int i = hand.Count - 1; i >= 0; i--)
        {

            if (hand[i].roundsUntilExpiry > 0 && hand[i].roundsUntilExpiry != 99)
            {
                hand[i].roundsUntilExpiry--;
                
                if (hand[i].roundsUntilExpiry <= 0)
                {
                    Debug.Log($"<color=orange>[Expiry]</color> Cartea {hand[i].cardName} a expirat pentru {participantName}");
                    UIManager.Instance.AddLog($"{participantName}'s {hand[i].cardName} expired!");
                    RemoveCardFromHand(hand[i]);
                }
            }
        }
    }

    public void ReceiveShot(GameObject shooter = null, bool isBullet = true)
    {

        CardData lastLaughCard = hand.Find(c => c is LastLaughCard);
        if (lastLaughCard != null && shooter != null)
        {
            Debug.Log("<color=magenta>LAST LAUGH!</color> " + participantName + " supraviețuiește și Grim trage în " + shooter.name + "!");
            UIManager.Instance.AddLog(participantName + " used LAST LAUGH!");
            RemoveCardFromHand(lastLaughCard);

            Animator shooterAnim = shooter.GetComponent<Animator>();
            if (shooterAnim != null) shooterAnim.SetTrigger("ReturnToSit");

            GameObject dealer = GameObject.Find("grim");
            if (dealer != null && GameManager.Instance.laughSound != null)
            {
                AudioSource grimSource = dealer.GetComponent<AudioSource>();
                if (grimSource != null)
                {
                    grimSource.PlayOneShot(GameManager.Instance.laughSound, GameManager.Instance.laughVolume);
                }
            }

            GameManager.Instance.StartCoroutine(LastLaughRetaliation(shooter));
            return;
        }

        if (hasShield)
        {
            Debug.Log(participantName + " s-a salvat folosind Shield!");
            UIManager.Instance.AddLog(participantName + " used SHIELD!");
            CardData shieldCard = hand.Find(c => c.cardName == "Shield");
            if (shieldCard != null) RemoveCardFromHand(shieldCard);

            if (shieldVfxPrefab != null)
            {
                Vector3 spawnPos;
                Quaternion spawnRot;

                if (shieldPoint != null)
                {
                    spawnPos = shieldPoint.position;
                    spawnRot = shieldPoint.rotation;
                }
                else
                {

                    spawnPos = transform.position + Vector3.up * 1.7f + transform.forward * 1.0f;
                    Transform spine = null;
                    Transform[] bones = GetComponentsInChildren<Transform>();
                    foreach (var b in bones) if (b.name.ToLower().Contains("spine02")) { spine = b; break; }
                    if (spine != null) spawnPos = spine.position + transform.forward * 1.1f + Vector3.up * 0.6f;
                    spawnRot = transform.rotation * Quaternion.Euler(0, 90, 0);
                }

                GameObject shieldInstance = Instantiate(shieldVfxPrefab, spawnPos, spawnRot);
                shieldInstance.transform.SetParent(this.transform);
                
                StartCoroutine(ShieldEffectRoutine(shieldInstance.transform));
            }

            return;
        }

        isAlive = false;
isTargetable = false;

        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name == "lid")
            {
                t.gameObject.SetActive(true);
            }
        }

        Animator anim = GetComponent<Animator>();
if (anim == null) anim = GetComponentInChildren<Animator>();

        if (isBullet)
        {
            if (anim != null)
            {
                anim.SetTrigger("DieBullet");
            }

        }
        else
        {
            if (anim != null)
            {
                anim.SetTrigger("GrenadeDeath");
            }
        }

        GameManager.Instance.RemoveParticipant(this.gameObject);
    }

    private IEnumerator ShieldEffectRoutine(Transform shield)
    {
        if (shield == null) yield break;
        
        Vector3 targetScale = shield.localScale;
        shield.localScale = Vector3.zero;
        
        float scaleDuration = 0.4f;
        float stayDuration = 2.7f;
        
        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            if (shield == null) yield break;
            elapsed += Time.deltaTime;
            shield.localScale = Vector3.Lerp(Vector3.zero, targetScale, Mathf.SmoothStep(0, 1, elapsed / scaleDuration));
            yield return null;
        }
        shield.localScale = targetScale;

        yield return new WaitForSeconds(stayDuration);

        elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            if (shield == null) yield break;
            elapsed += Time.deltaTime;
            shield.localScale = Vector3.Lerp(targetScale, Vector3.zero, Mathf.SmoothStep(0, 1, elapsed / scaleDuration));
            yield return null;
        }
        
        if (shield != null) Destroy(shield.gameObject);
    }

public void AnimEvent_OnShot()
    {
        Transform targetChest = null;
        Transform[] allBones = GetComponentsInChildren<Transform>();
        foreach (Transform bone in allBones)
        {

            if (bone.name.ToLower().Contains("spine02"))
            {
                targetChest = bone;
                break;
            }
        }

        if (targetChest == null)
        {
            foreach (Transform bone in allBones)
            {
                if (bone.name.ToLower().Contains("spine01"))
                {
                    targetChest = bone;
                    break;
                }
            }
        }

        if (targetChest != null)
        {
            GameObject bloodPrefab = Resources.Load<GameObject>("BloodHole");
            if (bloodPrefab != null)
            {
                GameObject blood = Instantiate(bloodPrefab, targetChest);

                blood.transform.localPosition = bloodHoleOffset;
                blood.transform.localRotation = Quaternion.Euler(bloodHoleRotation);
                Debug.Log("Bullet hole created on chest of " + participantName);
            }
        }
    }

    private System.Collections.IEnumerator LastLaughRetaliation(GameObject shooter)
    {

        yield return new WaitForSeconds(1.5f);

        GameObject dealer = GameObject.Find("grim");
        if (dealer == null)
        {

            shooter.GetComponent<Participant>()?.ReceiveShot();
            yield break;
        }

        EntityController entityCtrl = GameManager.Instance.entityVisual;
        if (entityCtrl != null)
        {
            entityCtrl.StopAndLookAt(shooter.transform.position);

            yield return new WaitForSeconds(1.2f);
        }

        yield return new WaitForSeconds(0.5f);

        ShootAction shootAction = null;
        if (GameManager.Instance != null && GameManager.Instance.possibleActions != null)
        {
            shootAction = GameManager.Instance.possibleActions.Find(a => a is ShootAction) as ShootAction;
        }

        if (shootAction != null)
        {
            yield return GameManager.Instance.StartCoroutine(
                shootAction.ExecuteActionAnimated(dealer, shooter, true, 1.5f));
        }
        else
        {

            Animator grimAnim = dealer.GetComponent<Animator>();
            if (grimAnim != null) grimAnim.SetTrigger("PrepareShot");
            yield return new WaitForSeconds(3f);

            GrimWeaponHandler weaponHandler = Object.FindFirstObjectByType<GrimWeaponHandler>();
            if (weaponHandler != null) weaponHandler.SetTarget(shooter);

            if (grimAnim != null) grimAnim.SetTrigger("FireShot");
            
            if (weaponHandler == null)
            {
                yield return new WaitForSeconds(1.5f);
                Participant shooterPart = shooter.GetComponent<Participant>();
                if (shooterPart != null && shooterPart.isAlive)
                    shooterPart.ReceiveShot();
            }
            else
            {

                yield return new WaitForSeconds(2.0f);
            }

            if (grimAnim != null) grimAnim.SetTrigger("ReturnIdle");
            yield return new WaitForSeconds(1.5f);
        }
    }

    public Vector3 GetCardTargetPosition(int cardIndex, int totalCards)
    {
        float halfCount = (totalCards - 1) * 0.5f;
        float indexFactor = cardIndex - halfCount;
        Vector3 localOffset = new Vector3(
            indexFactor * worldCardSpacing,           
            cardIndex * worldCardStackDepth,          
            -Mathf.Abs(indexFactor) * worldCardCurve  
        );

        if (cardTablePoint != null) return cardTablePoint.TransformPoint(localOffset);
        Vector3 fallbackBase = transform.position + transform.forward * 0.35f + Vector3.up * 0.78f;
        return fallbackBase + localOffset;
    }

    public Quaternion GetCardTargetRotation(int cardIndex, int totalCards)
    {
        float halfCount = (totalCards - 1) * 0.5f;
        float indexFactor = cardIndex - halfCount;
        Quaternion fanYaw = Quaternion.Euler(0f, indexFactor * worldCardYawStep, 0f);
        if (cardTablePoint != null) return cardTablePoint.rotation * Quaternion.Euler(cardFlatRotationEuler) * fanYaw;
        return Quaternion.LookRotation(-transform.forward, Vector3.up) * Quaternion.Euler(cardFlatRotationEuler) * fanYaw;
    }
}
