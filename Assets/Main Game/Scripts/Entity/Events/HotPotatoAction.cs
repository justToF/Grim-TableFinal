using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; 

[CreateAssetMenu(fileName = "Hot Potato Action", menuName = "Entity/Actions/HotPotato")]
public class HotPotatoAction : EntityAction
{
    private static bool isHotPotatoRunning = false;
    public static GameObject ActiveBomb { get; private set; }

    [Header("Settings")]
    public GameObject bombPrefab;

    [Header("3D Audio")]
    public AudioClip tickClip;
    public AudioClip explosionClip;
    [Min(0.05f)] public float normalTickInterval = 0.65f;
    [Min(0.03f)] public float finalTickInterval = 0.14f;
    [Range(0f, 1f)] public float tickVolume = 0.9f;
    [Range(0f, 1f)] public float explosionVolume = 1f;
    [Min(1f)] public float bombAudioMaxDistance = 18f;

    [Header("Explosion VFX")]
    public GameObject explosionVfxPrefab;
    public Vector3 explosionVfxOffset = Vector3.zero;
    [Min(0.1f)] public float explosionVfxLifetime = 3f;

    private GameObject visualBomb;
    private float detonationTimer;
    private float initialDetonationTimer;
    private GameObject currentHolder;
    private bool exploded = false;
    private AudioSource tickAudioSource;
    private GameObject tickAudioEmitter;
    private Coroutine tickingCoroutine;
    private int audioSessionId = 0;

    public override void ExecuteAction(GameObject startingTarget)
    {
        if (isHotPotatoRunning)
        {
            Debug.LogWarning("HotPotatoAction ignored: already running.");
            return;
        }
        isHotPotatoRunning = true;
        Debug.Log("<color=orange>[HotPotato]</color> ExecuteAction started.");

        StopTickingLoop();
        ActiveBomb = null;
        visualBomb = null;
        CleanupTickAudio();
        exploded = false;

        ResetAllParticipantsLook();
        ResetAllParticipantsAnimators(null, false);

        detonationTimer = UnityEngine.Random.Range(10f, 20f);
        initialDetonationTimer = detonationTimer;
        currentHolder = startingTarget;

        GameObject dealer = GameObject.Find("grim");
        if (dealer != null)
        {
            Animator anim = dealer.GetComponent<Animator>();
            if (anim != null)
            {
                anim.ResetTrigger("StartAction");
                anim.ResetTrigger("PrepareShot");
                anim.ResetTrigger("FireShot");
                anim.ResetTrigger("ReturnIdle");
                
                anim.SetInteger("ItemType", 1); 
                anim.SetTrigger("StartAction");
            }
        }

        GameManager.Instance.StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        Debug.Log("<color=orange>[HotPotato]</color> Waiting for intro animation...");
        yield return new WaitForSeconds(2.0f);

        GameObject dealer = GameObject.Find("grim");
        GrimAnimationHelper helper = dealer != null ? dealer.GetComponent<GrimAnimationHelper>() : null;

        if (helper != null && helper.currentItem != null)
        {
            visualBomb = helper.currentItem;
            visualBomb.transform.SetParent(null);
        }
        else
        {
            Debug.LogWarning("<color=orange>[HotPotato]</color> Item not found in Grim's hand, instantiating fallback.");
            visualBomb = Instantiate(bombPrefab);
        }
        ActiveBomb = visualBomb;

        SetupTickAudioSource();
        if (tickClip != null)
        {
            audioSessionId++;
            tickingCoroutine = GameManager.Instance.StartCoroutine(PlayTickingLoop(audioSessionId));
        }

        Transform dealerTransform = dealer != null ? dealer.transform : null;
        Debug.Log("<color=orange>[HotPotato]</color> Initial throw from Grim to " + currentHolder.name);
        yield return MoveBomb(currentHolder, false, dealerTransform);

        Debug.Log("<color=red>HOT POTATO A ÎNCEPUT!</color>");
        GameManager.Instance.StartCoroutine(BombLoop());
    }

    private IEnumerator BombLoop()
    {
        while (!exploded)
        {
            if (visualBomb == null) break;

            Animator currentAnim = currentHolder.GetComponent<Animator>();
            ParticipantAnimationHelper currentHelper = currentHolder.GetComponent<ParticipantAnimationHelper>();

            if (currentAnim != null && currentHelper != null)
            {
                Debug.Log("<color=orange>[HotPotato]</color> " + currentHolder.name + " is catching the bomb.");
                
                bool animationGrabbed = visualBomb.transform.parent != null;
                Action onGrab = () => {
                    Debug.Log("<color=orange>[HotPotato]</color> Grab confirmed via event for " + currentHolder.name);
                    animationGrabbed = true;
                };
                currentHelper.OnGrabConfirmed += onGrab;

                if (visualBomb.transform.parent != null) animationGrabbed = true;

                while (!animationGrabbed && detonationTimer > 0)
                {
                    detonationTimer -= Time.deltaTime;
                    yield return null;
                }

                currentHelper.ResetLook();
                currentHelper.OnGrabConfirmed -= onGrab;
                Debug.Log("<color=orange>[HotPotato]</color> Grab phase finished for " + currentHolder.name);
            }
            else
            {
                visualBomb.transform.position = GetBombPosition(currentHolder);
            }

            GameObject nextTarget = null;
            bool choiceMade = false;

            GameManager.Instance.StartCoroutine(GetTargetChoice(currentHolder, (chosen) => {
                nextTarget = chosen;
                choiceMade = true;
            }));

            while (!choiceMade && detonationTimer > 0)
            {
                detonationTimer -= Time.deltaTime;
                yield return null;
            }

            if (detonationTimer <= 0)
            {
                Explode(currentHolder);
                break;
            }

            if (choiceMade && nextTarget != null)
            {
                if (detonationTimer < 1.0f)
                {
                    Explode(currentHolder);
                    break;
                }

                if (currentAnim != null && currentHelper != null)
                {
                    Debug.Log("<color=orange>[HotPotato]</color> " + currentHolder.name + " is throwing to " + nextTarget.name);
                    currentHelper.LookAtTarget(nextTarget.transform);
                    yield return new WaitForSeconds(0.2f);
                    currentAnim.SetTrigger("t_Throw");

                    bool animationReleased = false;
                    Action onRelease = () => {
                        Debug.Log("<color=orange>[HotPotato]</color> Release confirmed for " + currentHolder.name);
                        animationReleased = true;
                    };
                    currentHelper.OnReleaseConfirmed += onRelease;

                    while (!animationReleased && detonationTimer > 0)
                    {
                        detonationTimer -= Time.deltaTime;
                        yield return null;
                    }

                    currentHelper.OnReleaseConfirmed -= onRelease;
                    currentHelper.ResetLook();

                    if (detonationTimer <= 0)
                    {
                        Explode(currentHolder);
                        break;
                    }
                }

                yield return MoveBomb(nextTarget, true, currentHolder.transform);

                if (exploded) break;

                BotGrudgeSystem nextGrudge = nextTarget.GetComponent<BotGrudgeSystem>();
                if (nextGrudge != null) nextGrudge.OnBombReceived(currentHolder);

                currentHolder = nextTarget;
            }
        }

        StopTickingLoop();
        ActiveBomb = null;
        CleanupTickAudio();
        isHotPotatoRunning = false;
        ResetAllParticipantsLook();
        if (visualBomb != null) Destroy(visualBomb);
        GameManager.Instance.StartPostEventWindow();
    }

    private IEnumerator MoveBomb(GameObject targetHolder, bool reduceTimer, Transform throwerTransform)
    {
        if (visualBomb == null) yield break;

        float elapsed = 0;
        float duration = 0.45f;
        Vector3 startPos = visualBomb.transform.position;

        Transform targetHand = null;
        ParticipantAnimationHelper targetHelper = targetHolder.GetComponent<ParticipantAnimationHelper>();
        if (targetHelper != null && targetHelper.handPoint != null)
        {
            targetHand = targetHelper.handPoint;
        }

        float arcHeight = 1.0f;
        Vector3 spinAxis = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
        float spinSpeed = 400f;

        Animator targetAnim = targetHolder.GetComponent<Animator>();
        bool catchTriggered = false;

        float timeToTriggerCatch = duration - 0.35f; 

        while (elapsed < duration)
        {
            if (visualBomb == null) yield break;

            if (visualBomb.transform.parent != null)
            {
                break;
            }

            if (!catchTriggered && elapsed >= timeToTriggerCatch)
            {
                catchTriggered = true;
                if (targetAnim != null) 
                {
                    Debug.Log("<color=orange>[HotPotato]</color> Firing t_Catch for " + targetHolder.name);
                    targetAnim.SetTrigger("t_Catch");
                }
                if (targetHelper != null)
                {
                    Transform lookTarget = throwerTransform != null ? throwerTransform : visualBomb.transform;
                    targetHelper.LookAtTarget(lookTarget);
                }
            }

            Vector3 currentTargetPos = (targetHand != null) ? targetHand.position : GetBombPosition(targetHolder);

            float t = elapsed / duration;

            Vector3 linearPos = Vector3.Lerp(startPos, currentTargetPos, t);
            linearPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            visualBomb.transform.position = linearPos;
            visualBomb.transform.Rotate(spinAxis * spinSpeed * Time.deltaTime, Space.World);

            elapsed += Time.deltaTime;

            if (reduceTimer)
            {
                detonationTimer -= Time.deltaTime;
                if (detonationTimer <= 0)
                {
                    if (targetAnim != null)
                    {
                        targetAnim.ResetTrigger("t_Catch");
                        targetAnim.SetTrigger("ReturnToSit");
                    }
                    Explode(currentHolder);
                    yield break;
                }
            }

            yield return null;
        }

        if (visualBomb != null && visualBomb.transform.parent == null)
        {
            visualBomb.transform.position = (targetHand != null) ? targetHand.position : GetBombPosition(targetHolder);
        }
    }

    private void Explode(GameObject victim)
    {
        if (exploded) return;
        exploded = true;
        Debug.Log("<color=orange>[HotPotato]</color> Exploding on " + victim.name);

        Participant p = victim.GetComponent<Participant>();
        if (p != null) p.ReceiveShot(null, false);

        ActiveBomb = null;
        StopTickingLoop();
        isHotPotatoRunning = false;

        if (explosionClip != null)
        {
            Vector3 explosionPos = (visualBomb != null) ? visualBomb.transform.position : victim.transform.position + Vector3.up;
            if (explosionVfxPrefab != null)
            {
                GameObject vfxInstance = Instantiate(explosionVfxPrefab, explosionPos + explosionVfxOffset, Quaternion.identity);
                Destroy(vfxInstance, explosionVfxLifetime);
            }
            Play3DSoundAtPosition(explosionClip, explosionPos, explosionVolume, 1f);
        }

        ResetAllParticipantsLook();
        ResetAllParticipantsAnimators(victim, true);
        if (visualBomb != null) Destroy(visualBomb);
        
        Debug.Log("<color=black>BOOM!</color> Bomba a explodat pe " + victim.name);
        GameManager.Instance.Invoke("StartPostEventWindow", 0.5f);
    }

    private void ResetAllParticipantsAnimators(GameObject victim = null, bool forceSit = false)
    {
        if (GameManager.Instance == null || GameManager.Instance.participants == null) return;

        foreach (GameObject pObj in GameManager.Instance.participants)
        {
            if (pObj == null) continue;
            Animator anim = pObj.GetComponent<Animator>();
            if (anim != null)
            {

                anim.ResetTrigger("t_Catch");
                anim.ResetTrigger("t_Throw");
                anim.ResetTrigger("t_Reset");
                anim.ResetTrigger("ReturnToSit");
                
                foreach (AnimatorControllerParameter param in anim.parameters)
                {
                    if (param.name == "isLookingAtCards") anim.SetBool("isLookingAtCards", false);
                }

                if (forceSit)
                {

                    Participant pPart = pObj.GetComponent<Participant>();
                    bool shouldSit = (pObj != victim) || (pPart != null && pPart.isAlive);

                    if (shouldSit)
                    {
                        anim.SetTrigger("ReturnToSit");
                    }
                }
            }
        }
    }

    private IEnumerator GetTargetChoice(GameObject holder, System.Action<GameObject> callback)
    {
        BotBrain bot = holder.GetComponent<BotBrain>();
        if (bot != null)
        {
            float reactionTime = UnityEngine.Random.Range(1.0f, 3.0f);
            float elapsed = 0;
            while (elapsed < reactionTime && detonationTimer > 0)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (detonationTimer > 0 && !exploded)
                callback(bot.ChooseTarget(GameManager.Instance.participants));
        }
        else
        {
            UIManager.Instance.ShowSelectionUI(true);
            GameObject selected = null;
            int participantLayerMask = LayerMask.GetMask("Participant");

            while (selected == null && detonationTimer > 0)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                    if (Physics.Raycast(ray, out RaycastHit hit, 25f, participantLayerMask))
                    {
                        Participant targetPart = hit.collider.GetComponentInParent<Participant>();
                        if (targetPart != null && targetPart.gameObject != holder && targetPart.isTargetable)
                        {
                            selected = targetPart.gameObject;
                        }
                    }
                }
                yield return null;
            }
            UIManager.Instance.ShowSelectionUI(false);
            if (selected != null) callback(selected);
        }
    }

    private Vector3 GetBombPosition(GameObject holder)
    {
        if (holder.GetComponent<BotBrain>() == null)
        {
            if (Camera.main != null) return Camera.main.transform.position + Camera.main.transform.forward * 0.8f + Vector3.down * 0.3f;
        }

        Transform headBone = FindBone(holder.transform, "CC_Base_NeckTwist02");
        if (headBone != null) return headBone.position + Vector3.up * 0.5f;

        return holder.transform.position + Vector3.up * 1.5f;
    }

    private Transform FindBone(Transform parent, string boneName)
    {
        if (parent.name == boneName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindBone(child, boneName);
            if (result != null) return result;
        }
        return null;
    }

    private void ResetAllParticipantsLook()
    {
        if (GameManager.Instance == null || GameManager.Instance.participants == null) return;

        foreach (GameObject participantObj in GameManager.Instance.participants)
        {
            if (participantObj == null) continue;
            ParticipantAnimationHelper helper = participantObj.GetComponent<ParticipantAnimationHelper>();
            if (helper != null) helper.ResetLook();
        }
    }

    private void SetupTickAudioSource()
    {
        if (visualBomb == null) return;

        CleanupTickAudio();
        tickAudioEmitter = new GameObject("HotPotatoTickAudioEmitter");
        tickAudioEmitter.transform.position = visualBomb.transform.position;

        tickAudioSource = tickAudioEmitter.AddComponent<AudioSource>();

        tickAudioSource.playOnAwake = false;
        tickAudioSource.loop = false;
        tickAudioSource.spatialBlend = 1f;
        tickAudioSource.rolloffMode = AudioRolloffMode.Linear;
        tickAudioSource.minDistance = 1f;
        tickAudioSource.maxDistance = bombAudioMaxDistance;
        tickAudioSource.dopplerLevel = 0f;
    }

    private IEnumerator PlayTickingLoop(int sessionId)
    {
        while (
            !exploded &&
            sessionId == audioSessionId &&
            visualBomb != null &&
            tickAudioSource != null)
        {
            float duration = Mathf.Max(0.01f, initialDetonationTimer);
            float progress = Mathf.Clamp01(1f - (Mathf.Max(0f, detonationTimer) / duration));
            float interval = Mathf.Lerp(normalTickInterval, finalTickInterval, progress);
            float pitch = Mathf.Lerp(0.70f, 1f, progress);

            if (tickAudioEmitter != null && visualBomb != null)
                tickAudioEmitter.transform.position = visualBomb.transform.position;

            tickAudioSource.Stop();
            tickAudioSource.pitch = pitch;
            tickAudioSource.clip = tickClip;
            tickAudioSource.volume = tickVolume;
            tickAudioSource.Play();

            float elapsed = 0f;
            while (
                elapsed < interval &&
                !exploded &&
                sessionId == audioSessionId &&
                visualBomb != null &&
                tickAudioSource != null)
            {
                elapsed += Time.deltaTime;
                if (tickAudioEmitter != null && visualBomb != null)
                    tickAudioEmitter.transform.position = visualBomb.transform.position;
                yield return null;
            }
        }
    }

    private void StopTickingLoop()
    {
        if (tickingCoroutine != null && GameManager.Instance != null)
        {
            GameManager.Instance.StopCoroutine(tickingCoroutine);
            tickingCoroutine = null;
        }
        audioSessionId++;
    }

    private void CleanupTickAudio()
    {
        if (tickAudioEmitter != null)
        {
            Destroy(tickAudioEmitter);
            tickAudioEmitter = null;
        }
        tickAudioSource = null;
    }

    private void Play3DSoundAtPosition(AudioClip clip, Vector3 worldPosition, float volume, float pitch)
    {
        GameObject tempAudio = new GameObject("HotPotatoExplosionAudio");
        tempAudio.transform.position = worldPosition;

        AudioSource source = tempAudio.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = bombAudioMaxDistance;
        source.dopplerLevel = 0f;
        source.volume = volume;
        source.pitch = pitch;
        source.clip = clip;
        source.Play();

        Destroy(tempAudio, clip.length + 0.15f);
    }

    private void SpawnExplosionVfx(Vector3 worldPosition)
    {
        if (explosionVfxPrefab == null) return;

        Vector3 spawnPos = worldPosition + explosionVfxOffset;
        GameObject vfxInstance = Instantiate(explosionVfxPrefab, spawnPos, Quaternion.identity);
        Destroy(vfxInstance, explosionVfxLifetime);
    }
}
