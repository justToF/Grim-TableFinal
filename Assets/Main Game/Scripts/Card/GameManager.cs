using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum GameState { DealingCards, CardsPlay, EntitySpinning, EntityAction, PostEventWindow, Resolution }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameState currentState;
    public List<GameObject> participants;
    public GameObject currentTarget;
    public GameObject forcedSecondaryTarget;
    public string activeSpecialRoundID;

    [Header("Referințe Entitate (Cilindrul Negru)")]
    public EntityController entityVisual;
    public List<EntityAction> possibleActions;
    private EntityAction selectedAction;

    [Header("Tutorial")]
    public TutorialData activeTutorial;
    private int currentRoundIndex = 0;

    [Header("Setări Timp")]
    public float spinDuration = 6f;
    public float cardsPlayDuration = 15f;
    public float postEventDuration = 15f;
    public float timerRemaining;
    public int totalRoundsElapsed = 0;

    [Header("Sistem de Cărți")]
    public List<CardData> cardPool = new List<CardData>(); 
    public int startingCardsCount = 2;
[Header("Deal Animation")]
    public Transform deckPoint;
    public GameObject dealCardVisualPrefab;
    public float dealCardTravelTime = 0.35f;
    public float dealCardInterval = 0.08f;
    public float dealArcHeight = 0.25f;
    public AudioClip dealCardSound;
    [Range(0f, 1f)] public float dealCardVolume = 0.3f;

    [Header("Sunete Extra")]
    public AudioClip laughSound;
    [Range(0f, 1f)] public float laughVolume = 0.4f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {

        if (GameSettings.tutorialToLoad != null)
        {
            activeTutorial = GameSettings.tutorialToLoad;
        }

        if (activeTutorial != null)
        {
            GameSettings.IsManualMode = true;
            cardsPlayDuration = 120f;
            Debug.Log("<color=yellow>[Tutorial]</color> Manual Mode and 120s timer forced for tutorial.");
        }
        else
        {
            GameSettings.IsManualMode = false;
        }

        while (LoadingManager.Instance != null && LoadingManager.Instance.IsLoading)
        {
            yield return null;
        }

        if (!GameSettings.IsManualMode)
        {
            spinDuration = 6f;
            cardsPlayDuration = 15f;
            postEventDuration = 5f;
            timerRemaining = 0f;
            totalRoundsElapsed = 0;
            startingCardsCount = 2;

            cardPool = new List<CardData>(Resources.LoadAll<CardData>("Cards"));

            possibleActions = new List<EntityAction>(Resources.LoadAll<EntityAction>("Events"));

            participants = new List<GameObject>();
            foreach (Participant p in FindObjectsByType<Participant>(FindObjectsSortMode.None))
            {
                if (p != null && p.gameObject != null)
                    participants.Add(p.gameObject);
            }

            Debug.Log($"<color=cyan>NORMAL MODE:</color> {cardPool.Count} cărți, {possibleActions.Count} evenimente, {participants.Count} participanți încărcați.");
        }

        StartCoroutine(StartRoundWithDealAnimation());
    }

    private IEnumerator StartRoundWithDealAnimation()
    {
        ChangeState(GameState.DealingCards); 
        yield return DistributeInitialCardsRoutine();
        
        ChangeState(GameState.CardsPlay);  
    }

    public void DistributeInitialCards()
    {
        Debug.Log("<color=cyan>GAMEMANAGER:</color> Distribuire corectă pornită...");

        if (participants == null || participants.Count == 0)
        {
            Debug.LogError("EROARE: Lista de participanți din GameManager este GOALĂ!");
            return;
        }

        foreach (GameObject pObj in participants)
        {
            Participant p = pObj.GetComponent<Participant>();

            if (p != null)
            {
                p.hand.Clear();

                for (int i = 0; i < startingCardsCount; i++)
                {
                    if (cardPool.Count > 0)
                    {
                        CardData randomCard = cardPool[Random.Range(0, cardPool.Count)];
                        p.AddCardToHand(randomCard);
                    }
                }
                Debug.Log("Am dat " + startingCardsCount + " cărți lui: " + p.participantName);
            }
        }
    }

    private IEnumerator DistributeInitialCardsRoutine()
    {
        Debug.Log("<color=cyan>GAMEMANAGER:</color> Distribuire animată pornită...");
        if (participants == null || participants.Count == 0)
        {
            Debug.LogError("EROARE: Lista de participanți din GameManager este GOALĂ!");
            yield break;
        }

        Dictionary<GameObject, int> participantCardIndex = new Dictionary<GameObject, int>();
        foreach (GameObject pObj in participants)
        {
            Participant p = pObj.GetComponent<Participant>();
            if (p == null) continue;
            p.hand.Clear();
            participantCardIndex[pObj] = 0;
        }

        if (activeTutorial != null && activeTutorial.initialSetups != null)
        {
            int maxCards = 0;
            foreach (var setup in activeTutorial.initialSetups)
            {
                if (setup.initialCards.Count > maxCards) maxCards = setup.initialCards.Count;
            }

            for (int drawRound = 0; drawRound < maxCards; drawRound++)
            {
                foreach (GameObject pObj in participants)
                {
                    Participant p = pObj.GetComponent<Participant>();
                    if (p == null || !p.isAlive) continue;

                    var setup = activeTutorial.initialSetups.Find(s => s.participantName == p.participantName);
                    if (setup != null && drawRound < setup.initialCards.Count)
                    {
                        CardData card = setup.initialCards[drawRound];
                        p.AddCardToHand(card);

                        int cardIndex = participantCardIndex[pObj];
                        participantCardIndex[pObj] = cardIndex + 1;
                        yield return AnimateDealCardToParticipant(pObj, cardIndex, card, setup.initialCards.Count);
                        yield return new WaitForSeconds(dealCardInterval);
                    }
                }
            }
        }
        else
        {
            for (int drawRound = 0; drawRound < startingCardsCount; drawRound++)
            {
                foreach (GameObject pObj in participants)
                {
                    Participant p = pObj.GetComponent<Participant>();
                    if (p == null || !p.isAlive) continue;
                    if (cardPool.Count == 0) continue;

                    CardData randomCard = cardPool[Random.Range(0, cardPool.Count)];
                    p.AddCardToHand(randomCard);

                    int cardIndex = participantCardIndex[pObj];
                    participantCardIndex[pObj] = cardIndex + 1;
                    yield return AnimateDealCardToParticipant(pObj, cardIndex, randomCard, startingCardsCount);
                    yield return new WaitForSeconds(dealCardInterval);
                }
            }
        }
    }

    public IEnumerator AnimateDealCardToParticipant(GameObject participantObj, int cardIndex, CardData dealtCardData, int totalCards = -1)
    {
        if (dealCardVisualPrefab == null || deckPoint == null || participantObj == null) yield break;

        Participant participant = participantObj.GetComponent<Participant>();
        if (participant == null) yield break;

        if (totalCards == -1) totalCards = participant.hand.Count;

        Vector3 endPos = participant.GetCardTargetPosition(cardIndex, totalCards);
        Quaternion endRot = participant.GetCardTargetRotation(cardIndex, totalCards);

        GrimPlayerManager playerManager = participantObj.GetComponent<GrimPlayerManager>();
        if (playerManager != null)
        {
            endPos = playerManager.GetWorldDealTargetPosition(cardIndex, totalCards);
            endRot = playerManager.GetWorldDealTargetRotation(cardIndex, totalCards);
        }

        GameObject dealCard = Instantiate(dealCardVisualPrefab, deckPoint.position, endRot);

        if (dealCardSound != null)
            AudioSource.PlayClipAtPoint(dealCardSound, deckPoint.position, dealCardVolume);
        
        WorldCardVisual worldVisual = dealCard.GetComponent<WorldCardVisual>();
        if (worldVisual != null) worldVisual.ApplyCardData(dealtCardData);

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, dealCardTravelTime);
        Vector3 startPos = deckPoint.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * dealArcHeight;
            
            dealCard.transform.position = pos;
            yield return null;
        }

        if (playerManager != null)
        {
            playerManager.RegisterWorldDealtCard(dealCard, cardIndex, totalCards);
            yield break; 
        } 
        dealCard.transform.position = endPos + Vector3.up * (cardIndex * 0.005f);
        dealCard.transform.rotation = endRot;
        participant.RegisterPhysicalCard(dealCard);
    }

    public IEnumerator AnimateCardTransfer(GameObject sourceObj, GameObject destObj, int cardIndex, CardData cardData)
    {
        if (dealCardVisualPrefab == null || sourceObj == null || destObj == null) yield break;

        Participant destPart = destObj.GetComponent<Participant>();
        Participant srcPart = sourceObj.GetComponent<Participant>();
        if (destPart == null) yield break;

        Vector3 startPos;
        GrimPlayerManager srcGPM = sourceObj.GetComponent<GrimPlayerManager>();
        if (srcGPM != null && srcGPM.tableCardsAnchor != null)
            startPos = srcGPM.tableCardsAnchor.position;
        else if (srcPart != null && srcPart.cardTablePoint != null)
            startPos = srcPart.cardTablePoint.position;
        else
            startPos = sourceObj.transform.position + Vector3.up * 0.9f;

        Vector3 endPos;
        Quaternion endRot;
        GrimPlayerManager destGPM = destObj.GetComponent<GrimPlayerManager>();
        if (destGPM != null)
        {
            endPos = destGPM.GetWorldDealTargetPosition(cardIndex, destPart.hand.Count);
            endRot = destGPM.GetWorldDealTargetRotation(cardIndex, destPart.hand.Count);
        }
        else
        {
            endPos = destPart.GetCardTargetPosition(cardIndex, destPart.hand.Count);
            endRot = destPart.GetCardTargetRotation(cardIndex, destPart.hand.Count);
        }

        GameObject dealCard = Instantiate(dealCardVisualPrefab, startPos, endRot);
        WorldCardVisual worldVisual = dealCard.GetComponent<WorldCardVisual>();
        if (worldVisual != null) worldVisual.ApplyCardData(cardData);

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, dealCardTravelTime * 1.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * dealArcHeight * 1.5f;

            dealCard.transform.position = pos;
            yield return null;
        }

        if (destGPM != null)
        {
            destGPM.RegisterWorldDealtCard(dealCard, cardIndex, destPart.hand.Count);
        }
        else
        {
            dealCard.transform.position = endPos + Vector3.up * (cardIndex * 0.005f);
            dealCard.transform.rotation = endRot;
            destPart.RegisterPhysicalCard(dealCard);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentState == GameState.CardsPlay)
            {
                if (GameSettings.IsManualMode)
                {
                    ChangeState(GameState.EntitySpinning);
                }
            }
            else if (currentState == GameState.PostEventWindow)
            {
                if (GameSettings.IsManualMode)
                {
                    if (postEventCoroutine != null) StopCoroutine(postEventCoroutine);
                    StartCoroutine(CheckForNewCardsRoutine());
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && currentState == GameState.CardsPlay)
        {
            SimulateInstantTurn();
        }
    }

    private Coroutine postEventCoroutine;

    public void ChangeState(GameState newState)
    {
        if (currentState == newState && newState == GameState.PostEventWindow) return;

        currentState = newState;
        Debug.Log($"<color=orange>[GameManager]</color> State changed to: {newState}");

        switch (currentState)
        {
            case GameState.EntitySpinning:
                ResetTargeting();
                StartCoroutine(EntitySpinRoutine());
                break;

            case GameState.EntityAction:
                ExecuteEntityEvent();
                break;

            case GameState.PostEventWindow:
                if (postEventCoroutine != null) StopCoroutine(postEventCoroutine);
                if (!GameSettings.IsManualMode)
                    postEventCoroutine = StartCoroutine(PostEventRoutine());
                break;

            case GameState.CardsPlay:
                if (!GameSettings.IsManualMode)
                    StartCoroutine(CardsPlayTimerRoutine());
                break;

            case GameState.DealingCards:
            case GameState.Resolution:
if (participants.Count == 1)
                {
                    string winnerName = participants[0].GetComponent<Participant>().participantName;
                    UIManager.Instance.ShowWinUI(winnerName);
                    Debug.Log("<color=gold>VICTORIE:</color> " + winnerName);
                }
                else if (participants.Count == 0)
                {
                    UIManager.Instance.ShowWinUI("Nobody");
                    Debug.Log("Toată lumea a murit. Nimeni nu a câștigat.");
                }
                break;
        }
    }

    private IEnumerator EntitySpinRoutine()
    {
        timerRemaining = spinDuration;
        forcedSecondaryTarget = null;
        activeSpecialRoundID = null;

        if (activeTutorial != null && currentRoundIndex < activeTutorial.scriptedRounds.Count)
        {
            var round = activeTutorial.scriptedRounds[currentRoundIndex];
            selectedAction = round.action;
            activeSpecialRoundID = round.specialRoundID;
            if (selectedAction != null) selectedAction.OnActionSelected();

            participants.RemoveAll(p => p == null);
            GameObject scriptedTarget = participants.Find(p =>
            {
                Participant part = p.GetComponent<Participant>();
                return part != null && part.participantName == round.targetParticipantName;
            });

            if (scriptedTarget != null && scriptedTarget.GetComponent<Participant>().isTargetable)
            {
                currentTarget = scriptedTarget;
            }
            else
            {

                List<GameObject> validTargets = participants.FindAll(p =>
                {
                    Participant part = p.GetComponent<Participant>();
                    return part != null && part.isAlive && part.isTargetable;
                });
                if (validTargets.Count > 0)
                    currentTarget = validTargets[Random.Range(0, validTargets.Count)];
                else
                    currentTarget = scriptedTarget;
            }

            if (!string.IsNullOrEmpty(round.secondaryTargetParticipantName))
            {
                forcedSecondaryTarget = participants.Find(p =>
                {
                    Participant part = p.GetComponent<Participant>();
                    return part != null && part.participantName == round.secondaryTargetParticipantName;
                });
            }

            if (currentTarget == null)
            {
                Debug.LogWarning($"<color=yellow>[Tutorial]</color> Target {round.targetParticipantName} not found! Falling back to random.");
                List<GameObject> validTargets = participants.FindAll(p =>
                {
                    Participant part = p.GetComponent<Participant>();
                    return part != null && part.isAlive && part.isTargetable;
                });
                if (validTargets.Count > 0)
                    currentTarget = validTargets[Random.Range(0, validTargets.Count)];
            }
        }
        else
        {
            if (possibleActions.Count > 0)
            {
                selectedAction = possibleActions[Random.Range(0, possibleActions.Count)];
                selectedAction.OnActionSelected();
            }

            participants.RemoveAll(p => p == null);

            List<GameObject> validTargets = participants.FindAll(p =>
            {
                Participant part = p.GetComponent<Participant>();
                return part != null && part.isAlive && part.isTargetable;
            });

            if (validTargets.Count > 0)
            {
                currentTarget = validTargets[Random.Range(0, validTargets.Count)];
            }
        }

        if (currentTarget != null)
        {
            if (entityVisual != null) entityVisual.StartSpinning();

            if (selectedAction != null && selectedAction.eventVoice != null)
            {
                Vector3 soundPos = entityVisual != null && entityVisual.audioSource != null ? 
                    entityVisual.audioSource.transform.position : 
                    entityVisual.transform.position;
                    
                AudioSource.PlayClipAtPoint(selectedAction.eventVoice, soundPos, 0.6f);
            }

            while (timerRemaining > 0)
            {
                timerRemaining -= Time.deltaTime;
                yield return null;
            }

            if (entityVisual != null)
                entityVisual.StopAndLookAt(currentTarget.transform.position);

            yield return new WaitForSeconds(1.6f);

            if (GameSettings.IsManualMode)
            {
                while (!Input.GetKeyDown(KeyCode.E)) yield return null;
            }

            ChangeState(GameState.EntityAction);
        }
        else
        {
            Debug.Log("Nicio țintă validă!");
            ChangeState(GameState.PostEventWindow);
        }
    }

    void ExecuteEntityEvent()
    {
        if (selectedAction != null && currentTarget != null)
        {

            foreach (GameObject pObj in participants)
            {
                if (pObj != null)
                {
                    Participant p = pObj.GetComponent<Participant>();
                    if (p != null) p.ResetAnimTriggers();
                }
            }

            selectedAction.ExecuteAction(currentTarget);
        }
    }

    private void SimulateInstantTurn()
    {
        Debug.Log("--- SIMULARE TURĂ INSTANTANEE ---");

        if (possibleActions.Count > 0)
            selectedAction = possibleActions[Random.Range(0, possibleActions.Count)];

        participants.RemoveAll(p => p == null);
        List<GameObject> validTargets = participants.FindAll(p =>
        {
            Participant part = p.GetComponent<Participant>();
            return part != null && part.isAlive && part.isTargetable;
        });

        if (validTargets.Count > 0)
        {
            currentTarget = validTargets[Random.Range(0, validTargets.Count)];
            if (selectedAction != null) selectedAction.ExecuteAction(currentTarget);
        }

        StartCoroutine(CheckForNewCardsRoutine()); 
    }

    public void ResetTargeting()
    {
        foreach (GameObject pObj in participants)
        {
            if (pObj == null) continue;
            Participant p = pObj.GetComponent<Participant>();
            if (p != null && p.ghostRoundsRemaining <= 0)
                p.isTargetable = true;
        }
    }

    public void StartPostEventWindow() => ChangeState(GameState.PostEventWindow);

    private IEnumerator CardsPlayTimerRoutine()
    {
        timerRemaining = cardsPlayDuration;
        UIManager.Instance.ShowPlayCardsMessage(true);
        
        while (timerRemaining > 0)
        {
            timerRemaining -= Time.deltaTime;
            UIManager.Instance.UpdateTimerUI(timerRemaining);
            yield return null;
        }
        
        UIManager.Instance.ShowPlayCardsMessage(false);
        if (!GameSettings.IsManualMode)
            ChangeState(GameState.EntitySpinning);
    }

    private IEnumerator PostEventRoutine()
    {
        timerRemaining = postEventDuration;

        while (timerRemaining > 0)
        {
            timerRemaining -= Time.deltaTime;
            yield return null;
        }

        yield return CheckForNewCardsRoutine(); 
    }

    private IEnumerator CheckForNewCardsRoutine()
    {
        currentRoundIndex++;
        totalRoundsElapsed++;

        if (activeTutorial == null)
        {
            foreach (GameObject pObj in participants)
            {
                if (pObj == null) continue;
                Participant p = pObj.GetComponent<Participant>();
                if (p != null && p.isAlive)
                {
                    p.ProcessCardExpiry();
                }
            }
        }

        foreach (GameObject pObj in participants)
        {
            if (pObj == null) continue;
            Participant p = pObj.GetComponent<Participant>();
            if (p != null && p.isAlive && p.ghostRoundsRemaining > 0)
            {
                p.ghostRoundsRemaining--;
                if (p.ghostRoundsRemaining <= 0)
                {
                    p.isTargetable = true;
                    Debug.Log("<color=grey>[Ghost Expiry]</color> " + p.participantName + " nu mai este Ghost. Poate fi țintit din nou!");
                }
                else
                {
                    Debug.Log("<color=grey>[Ghost]</color> " + p.participantName + " mai are " + p.ghostRoundsRemaining + " runde de Ghost.");
                }
            }
        }
        if (activeTutorial == null && totalRoundsElapsed % 2 == 0)
        {
            ChangeState(GameState.DealingCards);
            Debug.Log("<color=cyan>Runda " + totalRoundsElapsed + ":</color> Se împarte câte o carte nouă!");

            List<GameObject> currentParticipants = new List<GameObject>(participants);

            foreach (GameObject pObj in currentParticipants)
            {
                if (pObj == null) continue;

                Participant p = pObj.GetComponent<Participant>();
                if (p != null && p.isAlive && cardPool.Count > 0)
                {
                    CardData randomCard = cardPool[Random.Range(0, cardPool.Count)];
                    p.AddCardToHand(randomCard);

                    int newCardIndex = p.hand.Count - 1; 
                    
                    yield return AnimateDealCardToParticipant(pObj, newCardIndex, randomCard);
                    yield return new WaitForSeconds(dealCardInterval);
                }
            }
}
        
        ChangeState(GameState.CardsPlay);
    }

    public void RemoveParticipant(GameObject loser)
    {
        participants.Remove(loser);
        if (participants.Count <= 1) ChangeState(GameState.Resolution);
    }
}
