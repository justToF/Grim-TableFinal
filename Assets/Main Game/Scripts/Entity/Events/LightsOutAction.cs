using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Lights Out Action", menuName = "Entity/Actions/LightsOut")]
public class LightsOutAction : EntityAction
{
    [Header("Pistol")]
    public GameObject pistolPrefab;
    public AudioClip bangSound;
    public AudioClip clickSound;

    [Header("Lumini")]
    [Tooltip("Numele obiectului din scenă care are Light-ul special pentru shooter (ex: ShooterSpotlight)")]
    public string spotlightObjectName = "ShooterSpotlight";

    [Tooltip("Dacă un Light are acest text în nume, nu se va stinge la bezna (ex: 'CandleLight' sau 'StayOn')")]
    public string keepLightsOnName = "CandleLight";

    private List<Light> spotlightsForShooter = new List<Light>();

    [Header("Sunete Atmosferă")]
    public AudioClip lightsOffSound;
    public AudioClip lightsOnSound;

    private GameObject visualPistol;
    private GameObject currentHolder;
    private Vector3 originalPistolScale;
    private float itemHeightOffset = 1.1f;

    private List<Light> allSceneLights = new List<Light>();
    private List<bool> originalLightStates = new List<bool>();
    private List<float> originalLightIntensities = new List<float>();

    private float originalAmbientIntensity;
    private float originalReflectionIntensity;
    private Color originalAmbientLight;

    public override void OnActionSelected()
    {
        spotlightsForShooter.Clear();

        Light[] allLights = Resources.FindObjectsOfTypeAll<Light>();
        foreach (Light l in allLights)
        {
            if (l.gameObject.name == spotlightObjectName && l.gameObject.scene.isLoaded)
            {
                spotlightsForShooter.Add(l);
            }
        }

        SaveAndTurnOffAllLights();

        if (lightsOffSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(lightsOffSound, Camera.main.transform.position);
    }

    public override void ExecuteAction(GameObject startingTarget)
    {
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

        GameManager.Instance.StartCoroutine(LightsOutSequence());
    }

    private IEnumerator LightsOutSequence()
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
            if (playerAnim != null) playerAnim.SetTrigger("HoldPistol");
        }
        else
        {
            Debug.LogError("[LightsOut] Nu am găsit 'PlayerGunPoint' pe jucător!");
            Vector3 targetPos = currentHolder.transform.position + Vector3.up * itemHeightOffset;
            yield return MovePistolTo(targetPos);
            Animator playerAnim = currentHolder.GetComponent<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("HoldPistol");
        }

        yield return new WaitForSeconds(0.5f);

        Participant holderPart = currentHolder.GetComponent<Participant>();
        bool isBot = holderPart != null && holderPart.isBot;

        if (!isBot)
        {
            foreach (Light spotlight in spotlightsForShooter)
            {
                if (spotlight != null)
                {
                    spotlight.gameObject.SetActive(true);
                    spotlight.enabled = true;
                    spotlight.intensity = 2f;
                }
            }
        }

        yield return new WaitForSeconds(1f);

        Animator currentHolderAnim = currentHolder.GetComponent<Animator>();
        PlayerRouletteAnimEvents animEvents = currentHolder.GetComponent<PlayerRouletteAnimEvents>();

        GameObject target = null;

        if (isBot)
        {

            yield return GetTargetChoice(currentHolder, (chosen) => target = chosen);

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

            GameObject dummyAimTarget = new GameObject("LightsOutAimTarget");

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
                        if (targetPart != null && targetPart.gameObject != currentHolder)
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

        bool hasPulledTrigger = false;
        if (animEvents != null)
        {
            animEvents.OnPullTrigger = () => hasPulledTrigger = true;
        }

        if (currentHolderAnim != null) currentHolderAnim.SetTrigger("ShootPistol");

        float timeout = 3f;
        while (!hasPulledTrigger && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        if (animEvents != null) animEvents.OnPullTrigger = null;

        bool isTutorial = GameManager.Instance.activeTutorial != null;
        bool bulletFired = isTutorial || (Random.value < 0.5f);

        if (bulletFired)
        {

            if (bangSound != null) AudioSource.PlayClipAtPoint(bangSound, currentHolder.transform.position);

            if (target != null)
            {
                Participant targetPart = target.GetComponent<Participant>();
                if (targetPart != null) targetPart.ReceiveShot(currentHolder);
            }
        }
        else
        {

            if (clickSound != null) AudioSource.PlayClipAtPoint(clickSound, currentHolder.transform.position);
        }

        if (currentHolderAnim != null) currentHolderAnim.SetTrigger("ReturnToSit");

        if (visualPistol != null) Destroy(visualPistol);

        yield return RotateHolderUpperBody(currentHolder, null, 0f, 1.2f);

        foreach (Light spotlight in spotlightsForShooter)
        {
            if (spotlight != null)
            {
                spotlight.enabled = false;
                spotlight.gameObject.SetActive(false);
            }
        }

        yield return new WaitForSeconds(2f);

        RestoreAllLights();

        if (lightsOnSound != null)
            AudioSource.PlayClipAtPoint(lightsOnSound, currentHolder.transform.position);

        yield return new WaitForSeconds(0.5f);

        GameManager.Instance.StartPostEventWindow();
    }

    private void SaveAndTurnOffAllLights()
    {
        allSceneLights.Clear();
        originalLightStates.Clear();
        originalLightIntensities.Clear();

        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;
        originalAmbientLight = RenderSettings.ambientLight;

        RenderSettings.ambientIntensity = 0f;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.ambientLight = Color.black;

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {

            if (!string.IsNullOrEmpty(keepLightsOnName) && light.gameObject.name.Contains(keepLightsOnName))
            {
                continue;
            }

            allSceneLights.Add(light);
            originalLightStates.Add(light.enabled);
            originalLightIntensities.Add(light.intensity);

            light.enabled = false;
        }
    }

    private void RestoreAllLights()
    {

        RenderSettings.ambientIntensity = originalAmbientIntensity;
        RenderSettings.reflectionIntensity = originalReflectionIntensity;
        RenderSettings.ambientLight = originalAmbientLight;

        for (int i = 0; i < allSceneLights.Count; i++)
        {
            if (allSceneLights[i] != null)
            {
                allSceneLights[i].enabled = originalLightStates[i];
                allSceneLights[i].intensity = originalLightIntensities[i];
            }
        }

        allSceneLights.Clear();
        originalLightStates.Clear();
        originalLightIntensities.Clear();
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
            yield return new WaitForSeconds(Random.Range(1f, 2f));
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
                        if (targetPart != null && targetPart.gameObject != holder)
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
