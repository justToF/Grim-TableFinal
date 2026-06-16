using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Russian Roulette Action", menuName = "Entity/Actions/RussianRoulette")]
public class RussianRouletteAction : EntityAction
{
    public GameObject pistolPrefab;
    public AudioClip bangSound;
    public AudioClip clickSound;
    public AudioClip spinSound;
    public GameObject muzzleFlashPrefab;

    private GameObject visualPistol;
    private int bulletChamber;
    private int currentChamber = 1;
    private GameObject currentHolder;
    private bool isGameOver = false;

    private float itemHeightOffset = 1.1f;
    private Vector3 originalPistolScale;

    private Vector3 GetSpawnPos(GameObject holder)
    {
        Transform head = holder.transform.Find("CC_Base_NeckTwist02");
        if (head == null) head = holder.transform;

        return head.position + Vector3.up * 0.5f;
    }

    public override void ExecuteAction(GameObject startingTarget)
    {
        isGameOver = false;
        currentChamber = 1;
        
        if (GameManager.Instance.activeSpecialRoundID == "RUSSIAN_ROULETTE_PRESENTATION")
        {
            bulletChamber = 2;
        }
        else
        {
            bulletChamber = Random.Range(1, 7);
        }
        
        currentHolder = startingTarget;

        GameObject dealer = GameObject.Find("grim");
        if (dealer != null)
        {
            Animator anim = dealer.GetComponent<Animator>();
            anim.SetInteger("ItemType", 0);
            anim.SetTrigger("PlayRoulette");
        }

        if (currentHolder != null)
        {
            Animator playerAnim = currentHolder.GetComponent<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("CatchPistol");
        }

        GameManager.Instance.StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        GrimWeaponHandler weaponHandler = Object.FindFirstObjectByType<GrimWeaponHandler>();
        if (weaponHandler != null) weaponHandler.SetPlayReloadSoundOnDraw(true);
        
        bool hasThrown = false;

        if (weaponHandler != null)
        {
            weaponHandler.OnThrowGun = () => hasThrown = true;
        }
        else
        {
            yield return new WaitForSeconds(5f);
            hasThrown = true;
        }

        while (!hasThrown)
        {
            yield return null;
        }

        if (weaponHandler != null) weaponHandler.OnThrowGun = null;

        if (weaponHandler != null && weaponHandler.revolverInHand != null)
        {
            visualPistol = Instantiate(pistolPrefab, weaponHandler.revolverInHand.transform.position, weaponHandler.revolverInHand.transform.rotation);
            visualPistol.transform.localScale = weaponHandler.revolverInHand.transform.lossyScale;
            originalPistolScale = visualPistol.transform.lossyScale;
            weaponHandler.HideGun();
        }
        else
        {
            visualPistol = Instantiate(pistolPrefab);
            originalPistolScale = visualPistol.transform.lossyScale;
        }

        Transform targetGunPoint = GetGunPoint(currentHolder);

        if (targetGunPoint != null)
        {
            yield return ThrowPistolToHand(targetGunPoint, 0.6f);
            
            Animator playerAnim = currentHolder.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetTrigger("HoldPistol");
            }
        }
        else
        {
            Debug.LogError("[ATENȚIE] Nu am găsit obiectul 'PlayerGunPoint' pe jucător!");
            Vector3 targetPos = currentHolder.transform.position + Vector3.up * itemHeightOffset;
            yield return MovePistolTo(targetPos);

            Animator playerAnim = currentHolder.GetComponent<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("HoldPistol");
        }

        GameManager.Instance.StartCoroutine(RouletteLoop());
    }

    private IEnumerator RouletteLoop()
    {
        while (!isGameOver)
        {
            Participant holderPart = currentHolder.GetComponent<Participant>();
            bool isBot = holderPart != null && holderPart.isBot;

            Animator currentHolderAnim = currentHolder.GetComponent<Animator>();
            PlayerRouletteAnimEvents animEvents = currentHolder.GetComponent<PlayerRouletteAnimEvents>();

            GameObject target = null;

            if (isBot)
            {

                if (GameManager.Instance.activeSpecialRoundID == "RUSSIAN_ROULETTE_PRESENTATION" && holderPart.participantName == "Amy")
                {
                    yield return new WaitForSeconds(Random.Range(2f, 3f));
                    GameObject scriptedTarget = GameManager.Instance.participants.Find(p => p.GetComponent<Participant>().participantName == "Isabelle");
                    
                    if (scriptedTarget != null && scriptedTarget.GetComponent<Participant>().isTargetable)
                    {
                        target = scriptedTarget;
                    }
                    else
                    {

                        yield return GetTargetChoice(currentHolder, (chosen) => target = chosen);
                    }
                }
                else
                {
                    yield return GetTargetChoice(currentHolder, (chosen) => target = chosen);
                }

                if (currentHolderAnim != null) currentHolderAnim.SetTrigger("AimPistol");
                yield return new WaitForSeconds(0.6f);

                if (target != null)
                {
                    yield return RotateHolderUpperBody(currentHolder, target, 1f, 0.5f);
                }

                yield return new WaitForSeconds(1.0f);
            }
            else
            {

                if (currentHolderAnim != null) currentHolderAnim.SetTrigger("AimPistol");
                yield return new WaitForSeconds(0.6f);

                GameObject dummyAimTarget = new GameObject("PlayerDynamicAimTarget");

                float charY = currentHolder.transform.position.y;
                Plane horizontalPlane = new Plane(Vector3.up, new Vector3(0, charY, 0));
                
                {
                    Ray initRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                    if (horizontalPlane.Raycast(initRay, out float initDist))
                        dummyAimTarget.transform.position = initRay.GetPoint(initDist);
                    else
                        dummyAimTarget.transform.position = currentHolder.transform.position + currentHolder.transform.forward * 5f;
                }

                if (animEvents != null) animEvents.lookTarget = dummyAimTarget.transform;

                UIManager.Instance.ShowSelectionUI(true);
                int participantLayerMask = LayerMask.GetMask("Participant");

                float rampDuration = 0.5f;
                float rampElapsed = 0f;
                float startWeight = animEvents != null ? animEvents.lookWeight : 0f;

                while (target == null)
                {

                    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                    if (horizontalPlane.Raycast(ray, out float dist))
                    {
                        dummyAimTarget.transform.position = ray.GetPoint(dist);
                    }

                    if (rampElapsed < rampDuration && animEvents != null)
                    {
                        rampElapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(rampElapsed / rampDuration);
                        t = t * t * (3f - 2f * t);
                        animEvents.lookWeight = Mathf.Lerp(startWeight, 1f, t);
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (Physics.Raycast(ray, out RaycastHit clickHit, 25f, participantLayerMask))
                        {
                            Participant targetPart = clickHit.collider.GetComponentInParent<Participant>();
                            if (targetPart != null && targetPart.gameObject != currentHolder && targetPart.isTargetable)
                            {
                                target = targetPart.gameObject;
                            }
                        }
                    }

                    yield return null;
                }

                UIManager.Instance.ShowSelectionUI(false);

                if (animEvents != null)
                {
                    animEvents.lookTarget = dummyAimTarget.transform;
                    animEvents.lookWeight = 1f;
                }
                
                Destroy(dummyAimTarget, 5f);

                yield return new WaitForSeconds(0.3f);
            }

            if (target != null)
            {
                BotGrudgeSystem targetGrudge = target.GetComponent<BotGrudgeSystem>();
                if (targetGrudge != null) targetGrudge.OnShotAt(currentHolder);
            }

            bool hasPulledTrigger = false;
            if (animEvents != null)
            {
                animEvents.OnPullTrigger = () => hasPulledTrigger = true;
            }
            else
            {
                Debug.LogWarning("Jucătorul nu are scriptul PlayerRouletteAnimEvents! Vom folosi un timer de rezervă.");
            }

            if (currentHolderAnim != null) currentHolderAnim.SetTrigger("ShootPistol");

            float timeout = 3f;
            while (!hasPulledTrigger && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (animEvents != null) animEvents.OnPullTrigger = null;

            if (currentChamber == bulletChamber)
            {
                if (bangSound != null) AudioSource.PlayClipAtPoint(bangSound, currentHolder.transform.position);
                
                if (muzzleFlashPrefab != null && visualPistol != null)
                {
                    Transform barrelEnd = visualPistol.transform.Find("BarrelEnd");
                    Vector3 vfxPos = barrelEnd != null ? barrelEnd.position : visualPistol.transform.position;
                    Instantiate(muzzleFlashPrefab, vfxPos, visualPistol.transform.rotation);
                }

                if (currentHolderAnim != null) currentHolderAnim.SetTrigger("ReturnToSit");

                target.GetComponent<Participant>().ReceiveShot(currentHolder);
                isGameOver = true;
                
                yield return RotateHolderUpperBody(currentHolder, null, 0f, 1.2f);
            }
            else
            {
                if (clickSound != null) AudioSource.PlayClipAtPoint(clickSound, currentHolder.transform.position);
                
                yield return new WaitForSeconds(0.5f);

                if (currentHolderAnim != null) currentHolderAnim.SetTrigger("ThrowPistol");

                bool hasThrownGun = false;
                if (animEvents != null)
                {
                    animEvents.OnThrowPistol = () => hasThrownGun = true;
                }

                float throwTimeout = 2f;
                while (!hasThrownGun && throwTimeout > 0f)
                {
                    throwTimeout -= Time.deltaTime;
                    yield return null;
                }
                
                if (animEvents != null) animEvents.OnThrowPistol = null; 

                currentChamber++;
                
                GameObject previousShooter = currentHolder;
                currentHolder = target;

                GameManager.Instance.StartCoroutine(RotateHolderUpperBody(previousShooter, null, 0f, 0.8f));

                Animator nextHolderAnim = currentHolder.GetComponent<Animator>();
                if (nextHolderAnim != null) nextHolderAnim.SetTrigger("CatchPistol");

                Transform nextGunPoint = GetGunPoint(currentHolder);
                if (nextGunPoint != null)
                {
                    yield return ThrowPistolToHand(nextGunPoint, 0.6f);
                    if (nextHolderAnim != null) nextHolderAnim.SetTrigger("HoldPistol");
                }
                else
                {
                    Debug.LogError("[ATENȚIE] Nu am găsit obiectul 'PlayerGunPoint' pe jucător!");
                    Vector3 targetPos = currentHolder.transform.position + Vector3.up * itemHeightOffset;
                    yield return MovePistolTo(targetPos);
                    
                    if (nextHolderAnim != null) nextHolderAnim.SetTrigger("HoldPistol");
                }
            }
        }

        if (visualPistol != null) Destroy(visualPistol);
        GameManager.Instance.StartPostEventWindow();
    }

    private Transform GetGunPoint(GameObject holder)
    {
        Transform[] allBones = holder.GetComponentsInChildren<Transform>();
        foreach (Transform bone in allBones)
        {
            if (bone.name == "PlayerGunPoint") return bone;
        }
        return null;
    }

    private IEnumerator GetTargetChoice(GameObject holder, System.Action<GameObject> callback)
    {
        BotBrain bot = holder.GetComponent<BotBrain>();
        if (bot != null)
        {
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            callback(bot.ChooseTarget(GameManager.Instance.participants));
        }
        else
        {
            UIManager.Instance.ShowSelectionUI(true);
            GameObject selected = null;
            int participantLayerMask = LayerMask.GetMask("Participant");

            while (selected == null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                    if (Physics.Raycast(ray, out RaycastHit hit, 25f, participantLayerMask))
                    {
                        Participant targetPart = hit.collider.GetComponentInParent<Participant>();
                        if (targetPart != null && targetPart.gameObject != holder && targetPart.isTargetable)
                            selected = targetPart.gameObject;
                    }
                }
                yield return null;
            }
            UIManager.Instance.ShowSelectionUI(false);
            callback(selected);
        }
    }

    private IEnumerator MovePistolTo(Vector3 targetPos)
    {
        if (visualPistol == null) yield break;
        float elapsed = 0;
        float duration = 0.5f;
        Vector3 startPos = visualPistol.transform.position;
        while (elapsed < duration)
        {
            if (visualPistol == null) yield break;
            visualPistol.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        visualPistol.transform.position = targetPos;
    }

    private IEnumerator ThrowPistolToHand(Transform targetPoint, float duration = 0.5f)
    {
        if (visualPistol == null || targetPoint == null) yield break;

        visualPistol.transform.SetParent(null, true);

        Vector3 startPos = visualPistol.transform.position;
        Quaternion startRot = visualPistol.transform.rotation;
        Vector3 endPos = targetPoint.position;
        Quaternion endRot = targetPoint.rotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (visualPistol == null) yield break;
            
            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t);
            float height = Mathf.Sin(t * Mathf.PI) * 0.6f; 
            
            visualPistol.transform.position = Vector3.Lerp(startPos, endPos, smoothT) + Vector3.up * height;
            visualPistol.transform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        visualPistol.transform.SetParent(targetPoint, true);

        visualPistol.transform.localPosition = Vector3.zero;
        visualPistol.transform.localRotation = Quaternion.identity;

        if (targetPoint.lossyScale.x > 0.001f && targetPoint.lossyScale.y > 0.001f && targetPoint.lossyScale.z > 0.001f)
        {
            visualPistol.transform.localScale = new Vector3(
                originalPistolScale.x / targetPoint.lossyScale.x,
                originalPistolScale.y / targetPoint.lossyScale.y,
                originalPistolScale.z / targetPoint.lossyScale.z
            );
        }
    }

    private IEnumerator RotateHolderUpperBody(GameObject holder, GameObject target, float targetWeight, float duration)
    {
        PlayerRouletteAnimEvents animEvents = holder.GetComponent<PlayerRouletteAnimEvents>();
        if (animEvents == null) yield break;

        if (target != null) animEvents.lookTarget = target.transform;

        float startWeight = animEvents.lookWeight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            animEvents.lookWeight = Mathf.Lerp(startWeight, targetWeight, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        animEvents.lookWeight = targetWeight;
        if (targetWeight <= 0.01f) animEvents.lookTarget = null;
    }
}
