using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class GrimPlayerManager : MonoBehaviour
{
    public static GrimPlayerManager Instance;
    public static bool BlockInventory = false;
    public static bool BlockCameraRotation = false;

    public Animator playerAnimator;
    [Header("Referinte Camera")]
    public Transform mainCamera;
    public Transform cameraLookAt;
    public Vector3 cameraOffset = new Vector3(0.6f, 0.5f, -2.0f);
    public float sensitivity = 2.0f;

    [Header("Limite Camera")]
    public float minXAngle = -20f;
    public float maxXAngle = 30f;
    public float minYAngle = 130f;
    public float maxYAngle = 230f;

    [Header("Rotatie Gat (NeckTwist)")]
    public Transform neckBone;
    [Range(0f, 1f)] public float lookWeight = 0.8f;
    public Vector3 neckRotationOffset = new Vector3(0, 90, -90);

    [Header("Referinte Inventar")]
    public GameObject cardPrefab;
    public Transform handContainer;
    public GameObject uiPanel;
    [Header("World Card Visuals")]
    public Transform tableCardsAnchor;
    public Transform raisedCardsAnchor;
    public float worldCardMoveSpeed = 10f;
    public float worldCardSpacing = 0.08f;
    public float worldCardCurve = 0.03f;
    public Vector3 tableFanRotationEuler = new Vector3(90f, 0f, 0f);
    public Vector3 raisedRotationEuler = new Vector3(-20f, 0f, 0f);

    [Header("Animatie si Sunet")]
    public float transitionSpeed = 12f;
    public Vector2 hiddenPosition = new Vector2(0, -1100f);
    public Vector2 visiblePosition = new Vector2(0, 0);
    public AudioSource audioSource;
    public AudioClip swooshSound;

    private float xRotation = 0f;
    private float yRotation = 180f;
    private bool isInventoryOpen = false;
    private bool isInTargetMode = false;
    private CardUI highlightedCard;
    public Image crosshair;
    public LayerMask participantLayer;
    public GameObject useButton;

    private RectTransform panelRect;
    private Coroutine inventoryRoutine;
    private readonly List<Transform> worldDealtCards = new List<Transform>();
    private readonly Dictionary<Transform, int> worldCardSlots = new Dictionary<Transform, int>();
    private int worldCardsDealTotal = 0;

    private Participant participant;

    void Awake()
    {
        Instance = this;
        participant = GetComponent<Participant>();
    }

    void Start()
    {
        BlockCameraRotation = false;

        yRotation = transform.eulerAngles.y;

        panelRect = uiPanel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = hiddenPosition;
        uiPanel.SetActive(false);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        EnsureCardAnchors();

        SetInventoryMode(false);
        Invoke("RefreshHandUI", 0.2f);

        if (mainCamera != null && cameraLookAt != null)
        {
            HandleCameraRotation();
        }

        if (useButton != null)
        {
            Button btn = useButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnUseButtonClicked);
                btn.onClick.AddListener(OnUseButtonClicked);
            }
        }
    }

    void Update()
    {

        if (!isInventoryOpen && !BlockCameraRotation)
        {
            HandleCameraRotation();
        }

        GameState state = GameManager.Instance.currentState;
        bool canPlayCards = (state == GameState.CardsPlay || state == GameState.PostEventWindow);
        if (!canPlayCards && (isInventoryOpen || isInTargetMode))
        {
            SetInventoryMode(false);
            if (playerAnimator != null)
                playerAnimator.SetBool("isLookingAtCards", false);
            if (highlightedCard != null)
            {
                highlightedCard.ResetCard();
                highlightedCard = null;
                if (useButton != null) useButton.SetActive(false);
            }
            if (isInTargetMode)
            {
                isInTargetMode = false;
                if (UIManager.Instance != null) UIManager.Instance.ShowSelectionUI(false);
            }
        }

        if (state == GameState.DealingCards || BlockInventory) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetInventoryMode(true);
            
            if (playerAnimator != null) 
            {
                playerAnimator.SetBool("isLookingAtCards", true);
            }
            
            RefreshHandUI();
        }

        if (Input.GetKeyUp(KeyCode.Space) && !isInTargetMode)
        {
            SetInventoryMode(false);
            
            if (playerAnimator != null) 
            {
                playerAnimator.SetBool("isLookingAtCards", false);
            }

            if (highlightedCard != null)
            {
                highlightedCard.ResetCard();
                highlightedCard = null;
                if (useButton != null) useButton.SetActive(false);
            }
        }

        if (isInTargetMode && highlightedCard == null)
        {
            isInTargetMode = false;
            if (UIManager.Instance != null) UIManager.Instance.ShowSelectionUI(false);
            if (!isInventoryOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (isInTargetMode && Input.GetMouseButtonDown(1))
        {
            isInTargetMode = false;
            if (UIManager.Instance != null) UIManager.Instance.ShowSelectionUI(false);
            if (highlightedCard != null)
            {
                highlightedCard.SetRaised(false);
                highlightedCard = null;
            }
            if (!isInventoryOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (isInTargetMode || !isInventoryOpen)
        {
            HandleSelection();
        }
    }

    void LateUpdate()
    {
        if (neckBone != null && participant != null && participant.isAlive)
        {
            if (isInventoryOpen) HandleNeckLookAtCards();
            else HandleNeckRotation();
        }

        UpdateWorldCardsVisual(Time.deltaTime);
    }

    void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, minXAngle, maxXAngle);
        yRotation = Mathf.Clamp(yRotation, minYAngle, maxYAngle);

        Quaternion rotation = Quaternion.Euler(xRotation, 
            yRotation, 0);

        Vector3 targetPosition = cameraLookAt.position + (rotation * 
            cameraOffset);
        mainCamera.position = targetPosition;

        Vector3 lookAtTarget = cameraLookAt.position + (rotation * 
            new Vector3(cameraOffset.x, 0, 50f));
        mainCamera.LookAt(lookAtTarget);
    }

    void HandleNeckRotation()
    {
        Quaternion targetRotation = Quaternion.Euler(xRotation,
            yRotation, 0) * Quaternion.Euler(neckRotationOffset);
        neckBone.rotation = Quaternion.Slerp(neckBone.rotation,
            targetRotation, lookWeight);
    }

    void HandleNeckLookAtCards()
    {
        if (raisedCardsAnchor == null) return;
        Vector3 lookDir = raisedCardsAnchor.position - neckBone.position;
        if (lookDir.sqrMagnitude < 0.0001f) return;

        Quaternion lookRot = Quaternion.LookRotation(lookDir, Vector3.up) * Quaternion.Euler(neckRotationOffset);
        neckBone.rotation = Quaternion.Slerp(neckBone.rotation, lookRot, lookWeight * 0.7f);
    }

    void SetInventoryMode(bool open)
    {
        isInventoryOpen = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        if (audioSource != null && swooshSound != null)
            audioSource.PlayOneShot(swooshSound);

        if (inventoryRoutine != null) StopCoroutine(inventoryRoutine);
        inventoryRoutine = StartCoroutine(AnimateInventory(open));
    }

    void UpdateWorldCardsVisual(float deltaTime)
    {
        if (worldDealtCards.Count == 0) return;

        Transform anchor = isInventoryOpen && raisedCardsAnchor != null ? raisedCardsAnchor : tableCardsAnchor;
        if (anchor == null) return;

        float totalCount = Mathf.Max(worldCardsDealTotal, worldDealtCards.Count);
        float halfCount = (totalCount - 1f) * 0.5f;

        for (int i = worldDealtCards.Count - 1; i >= 0; i--)
        {
            Transform card = worldDealtCards[i];
            if (card == null) continue;

            int slot = worldCardSlots.TryGetValue(card, out int savedSlot) ? savedSlot : i;
            float indexFactor = slot - halfCount;

            Vector3 localOffset = new Vector3(
                indexFactor * worldCardSpacing, 
                slot * 0.005f, 
                -Mathf.Abs(indexFactor) * worldCardCurve
            );
            Vector3 worldTarget = anchor.TransformPoint(localOffset);

            Quaternion fanYaw = Quaternion.Euler(0f, indexFactor * 7f, 0f);
            Quaternion targetBaseRotation = anchor.rotation * fanYaw * Quaternion.Euler(isInventoryOpen ? raisedRotationEuler : tableFanRotationEuler);

            card.position = Vector3.Lerp(card.position, worldTarget, deltaTime * worldCardMoveSpeed);
            card.rotation = Quaternion.Slerp(card.rotation, targetBaseRotation, deltaTime * worldCardMoveSpeed);
        }
    }

    public void RegisterWorldDealtCard(GameObject dealtCardObj, int cardIndex, int totalCards)
    {
        if (dealtCardObj == null) return;
        if (tableCardsAnchor == null)
        {
            Debug.LogWarning("tableCardsAnchor nu este setat pe GrimPlayerManager.");
            return;
        }

        Transform cardTransform = dealtCardObj.transform;
        cardTransform.SetParent(null, true);
        if (!worldDealtCards.Contains(cardTransform))
            worldDealtCards.Add(cardTransform);
        worldCardSlots[cardTransform] = cardIndex;
        worldCardsDealTotal = Mathf.Max(worldCardsDealTotal, totalCards);

        float halfCount = (totalCards - 1f) * 0.5f;
        float indexFactor = cardIndex - halfCount;
        Vector3 localOffset = new Vector3(indexFactor * worldCardSpacing, -Mathf.Abs(indexFactor) * worldCardCurve, cardIndex * 0.0035f);
        cardTransform.position = tableCardsAnchor.TransformPoint(localOffset);
        cardTransform.rotation = tableCardsAnchor.rotation * Quaternion.Euler(tableFanRotationEuler);
    }

    public void RemoveWorldCard(int index)
    {
        if (index < 0 || index >= worldDealtCards.Count) return;

        Transform cardTransform = worldDealtCards[index];
        worldCardSlots.Remove(cardTransform);
        worldDealtCards.RemoveAt(index);

        if (cardTransform != null)
        {
            Destroy(cardTransform.gameObject);
        }

        worldCardsDealTotal = worldDealtCards.Count;
        for (int i = 0; i < worldDealtCards.Count; i++)
        {
            if (worldDealtCards[i] != null)
                worldCardSlots[worldDealtCards[i]] = i;
        }
    }

    public void ClearAllWorldCards()
    {
        foreach (Transform card in worldDealtCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        worldDealtCards.Clear();
        worldCardSlots.Clear();
        worldCardsDealTotal = 0;
    }

    public Vector3 GetWorldDealTargetPosition(int cardIndex, int totalCards)
    {
        if (tableCardsAnchor == null) return transform.position + Vector3.up * 0.9f;
        float halfCount = (totalCards - 1f) * 0.5f;
        float indexFactor = cardIndex - halfCount;
        
        Vector3 localOffset = new Vector3(
            indexFactor * worldCardSpacing, 
            cardIndex * 0.005f, 
            -Mathf.Abs(indexFactor) * worldCardCurve
        );
        return tableCardsAnchor.TransformPoint(localOffset);
    }

    public Quaternion GetWorldDealTargetRotation(int cardIndex, int totalCards)
    {
        if (tableCardsAnchor == null) return Quaternion.Euler(tableFanRotationEuler);
        float halfCount = (totalCards - 1f) * 0.5f;
        float indexFactor = cardIndex - halfCount;
        
        Quaternion fanYaw = Quaternion.Euler(0f, indexFactor * 7f, 0f);
        return tableCardsAnchor.rotation * fanYaw * Quaternion.Euler(tableFanRotationEuler);
    }

    private void EnsureCardAnchors()
    {
        if (tableCardsAnchor == null)
        {
            GameObject tableAnchorObj = new GameObject("TableCardsAnchor_Auto");
            tableAnchorObj.transform.SetParent(transform);
            tableAnchorObj.transform.localPosition = new Vector3(0f, 0.8f, 0.38f);
            tableAnchorObj.transform.localRotation = Quaternion.identity;
            tableCardsAnchor = tableAnchorObj.transform;
        }

        if (raisedCardsAnchor == null)
        {
            GameObject raisedAnchorObj = new GameObject("RaisedCardsAnchor_Auto");
            raisedAnchorObj.transform.SetParent(transform);
            raisedAnchorObj.transform.localPosition = new Vector3(0f, 1.12f, 0.32f);
            raisedAnchorObj.transform.localRotation = Quaternion.identity;
            raisedCardsAnchor = raisedAnchorObj.transform;
        }
    }

    IEnumerator AnimateInventory(bool open)
    {
        Vector2 targetPos = open ? visiblePosition : hiddenPosition;
        if (open) uiPanel.SetActive(true);

        while (Vector2.Distance(panelRect.anchoredPosition, targetPos) > 1f)
        {
            panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, targetPos, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        panelRect.anchoredPosition = targetPos;
        if (!open) uiPanel.SetActive(false);
    }

    void HandleSelection()
    {
        if (highlightedCard == null || !highlightedCard.assignedData.needsTarget)
        {
            crosshair.color = Color.white;
            return;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * 15f, Color.red);

        if (Physics.Raycast(ray, out hit, 20f, participantLayer))
        {

            Debug.Log("Raycast a lovit: " + hit.collider.gameObject.name);

            Participant targetPart = hit.collider.GetComponentInParent<Participant>();

            if (targetPart != null)
            {
                Debug.Log($"Target gasit: {targetPart.name}, Alive: {targetPart.isAlive}, Targetable: {targetPart.isTargetable}");
                
                if (targetPart.isAlive && targetPart.isTargetable)
                {
                    if (hit.collider.gameObject != this.gameObject)
                    {

                        bool needsTargetCards = highlightedCard.assignedData is ThiefCard || highlightedCard.assignedData is DeckSwapCard;
                        if (needsTargetCards && targetPart.hand.Count == 0)
                        {

                            crosshair.color = Color.white;
                            return;
                        }

                        crosshair.color = Color.red;
                        if (Input.GetMouseButtonDown(0))
                        {
                            Debug.Log("Click inregistrat pe tinta valida!");
                            ExecuteCardOnTarget(targetPart.gameObject);
                        }
                        return;
                    }
                    else
                    {
                        Debug.Log("Target-ul suntem chiar noi, ignoram click-ul.");
                    }
                }
                else
                {
                    Debug.Log("Target invalid! Trebuie sa fie Alive si Targetable.");
                }
            }
            else
            {
                Debug.Log("Obiectul lovit nu are componenta Participant!");
            }
        }

        crosshair.color = Color.white;
    }

    void ExecuteCardOnTarget(GameObject target)
    {
        Participant self = GetComponent<Participant>();
        CardData cardToExecute = highlightedCard.assignedData;
        if (cardToExecute.cardName != "Reload") self.lastPlayedCard = cardToExecute;

        if (UIManager.Instance != null) UIManager.Instance.ShowSelectionUI(false);
        isInTargetMode = false;

        cardToExecute.Execute(this.gameObject, target);
        self.RemoveCardFromHand(cardToExecute);

        if (highlightedCard != null) highlightedCard.ResetCard();
        highlightedCard = null;
        if (useButton != null) useButton.SetActive(false);
        RefreshHandUI();
    }

    public void OnCardClicked(CardUI card)
    {
        if (highlightedCard == card)
        {

            card.SetRaised(false);
            highlightedCard = null;
            if (useButton != null) useButton.SetActive(false);
        }
        else
        {

            if (highlightedCard != null)
            {
                highlightedCard.SetRaised(false);
            }

            highlightedCard = card;
            highlightedCard.SetRaised(true);
            if (useButton != null) useButton.SetActive(true);
        }
    }

    public void OnUseButtonClicked()
    {
        Debug.Log("OnUseButtonClicked apelat! Carte selectată: " + (highlightedCard != null ? highlightedCard.assignedData.cardName : "NULL"));
        if (highlightedCard == null) return;

        if (highlightedCard.assignedData.min3PlayersRequired)
        {
            int alivePlayers = GameManager.Instance.participants.FindAll(p =>
            {
                if (p == null) return false;
                Participant part = p.GetComponent<Participant>();
                return part != null && part.isAlive;
            }).Count;
            if (alivePlayers < 3)
            {
                Debug.Log("<color=yellow>Nu poți folosi " + highlightedCard.assignedData.cardName + " — trebuie minim 3 jucători vii!</color>");
                return;
            }
        }

        if (highlightedCard.assignedData is GhostCard && !GhostCard.CanUseGhost())
        {
            Debug.Log("<color=yellow>Nu poți folosi Ghost — prea puțini jucători targetable!</color>");
            return;
        }

        if (highlightedCard.assignedData is ReloadCard && !ReloadCard.CanUseReload(this.gameObject))
        {
            Debug.Log("<color=yellow>Nu poți folosi Reload — nu ai jucat nicio carte anterior!</color>");
            return;
        }

        if (highlightedCard.assignedData.needsTarget)
        {

            isInTargetMode = true;
            isInventoryOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (inventoryRoutine != null) StopCoroutine(inventoryRoutine);
            if (uiPanel != null)
            {
                panelRect.anchoredPosition = hiddenPosition;
                uiPanel.SetActive(false);
            }

            if (playerAnimator != null)
                playerAnimator.SetBool("isLookingAtCards", false);

            if (UIManager.Instance != null)
                UIManager.Instance.ShowSelectionUI(true);

            if (useButton != null) useButton.SetActive(false);
        }
        else
        {

            ExecuteCardOnTarget(this.gameObject);

            isInventoryOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (inventoryRoutine != null) StopCoroutine(inventoryRoutine);
            if (uiPanel != null)
            {
                panelRect.anchoredPosition = hiddenPosition;
                uiPanel.SetActive(false);
            }

            if (playerAnimator != null)
                playerAnimator.SetBool("isLookingAtCards", false);
        }
    }

    public void RefreshHandUI()
    {
        if (handContainer == null) return;
        highlightedCard = null;
        foreach (Transform child in handContainer) Destroy(child.gameObject);
        handContainer.DetachChildren();

        Participant self = GetComponent<Participant>();
        if (self == null) return;

        foreach (CardData card in self.hand)
        {
            GameObject newCardObj = Instantiate(cardPrefab, handContainer);
            newCardObj.transform.localScale = Vector3.one;
            newCardObj.GetComponent<CardUI>().UpdateDisplay(card);
        }

        var fan = handContainer.GetComponent<HandFanLayout>();
        if (fan != null) fan.UpdateLayout();
    }

    public void AddCard(CardData cardAsset) => GetComponent<Participant>().AddCardToHand(cardAsset);
    public void RemoveCard(CardData card) => GetComponent<Participant>().RemoveCardFromHand(card);
}
