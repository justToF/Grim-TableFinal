using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Lights Out Duel Action", menuName = "Entity/Actions/LightsOutDuel")]
public class LightsOutDuel : EntityAction
{
    [Header("Pistol")]
    public GameObject pistolPrefab;
    public AudioClip bangSound;
    public AudioClip clickSound;
    public GameObject muzzleFlashPrefab;

    [Header("Lumini")]
    [Tooltip("Numele obiectelor din scenă cu Light-ul special (ShooterSpotlight)")]
    public string spotlightObjectName = "ShooterSpotlight";

    [Tooltip("Dacă un Light are acest text în nume, NU se va stinge la beznă")]
    public string keepLightsOnName = "CandleLight";

    [Header("Muzică & Sunete")]
    public AudioClip suspenseMusic;
    public AudioClip lightsOffSound;
    public AudioClip lightsOnSound;

    [Header("Timing")]
    [Tooltip("Câte secunde de suspans înainte de aprinderea luminilor (random între min și max)")]
    public float suspenseTimeMin = 3f;
    public float suspenseTimeMax = 5f;

    private GameObject pistolA;
    private GameObject pistolB;
    private GameObject duelistA;
    private GameObject duelistB;

    private List<Light> spotlightsForShooter = new List<Light>();
    private List<Light> allSceneLights = new List<Light>();
    private List<bool> originalLightStates = new List<bool>();
    private List<float> originalLightIntensities = new List<float>();

    private float originalAmbientIntensity;
    private float originalReflectionIntensity;
    private Color originalAmbientLight;

    private AudioSource suspenseAudioSource;

    public override void OnActionSelected()
    {
        spotlightsForShooter.Clear();
        GrimPlayerManager.BlockInventory = true;

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

        if (suspenseMusic != null)
        {
            GameObject audioObj = new GameObject("DuelSuspenseMusic");
            suspenseAudioSource = audioObj.AddComponent<AudioSource>();
            suspenseAudioSource.clip = suspenseMusic;
            suspenseAudioSource.loop = true;
            suspenseAudioSource.Play();
        }
    }

    public override void ExecuteAction(GameObject startingTarget)
    {
        duelistA = startingTarget;

        List<GameObject> validTargets = GameManager.Instance.participants.FindAll(p =>
            p != duelistA && p.GetComponent<Participant>().isAlive && p.GetComponent<Participant>().isTargetable);

        if (validTargets.Count == 0)
        {
            Debug.LogWarning("[LightsOutDuel] Nu există al doilea participant! Anulăm.");
            RestoreAllLights();
            GameManager.Instance.StartPostEventWindow();
            return;
        }

        duelistB = GameManager.Instance.forcedSecondaryTarget != null ? GameManager.Instance.forcedSecondaryTarget : validTargets[Random.Range(0, validTargets.Count)];

        Debug.Log($"<color=red><b>--- [LightsOutDuel] DUELIȘTI ALEȘI: {duelistA.name} VS {duelistB.name} ---</b></color>");

        GameManager.Instance.StartCoroutine(DuelSequence());
    }

    private IEnumerator DuelSequence()
    {
        Debug.Log($"<color=orange>[Duel]</color> Sequence started. A: {duelistA.name}, B: {duelistB.name}");
        
        GrimWeaponHandler weaponHandler = Object.FindFirstObjectByType<GrimWeaponHandler>();
        if (weaponHandler != null) weaponHandler.SetPlayReloadSoundOnDraw(true);

        GameObject dummyAimTarget = null;

        yield return new WaitForSeconds(1.5f);

        Animator animA = duelistA.GetComponent<Animator>();
        Animator animB = duelistB.GetComponent<Animator>();

        if (animA == null) Debug.LogWarning($"<color=yellow>[Duel]</color> Animator missing on {duelistA.name}");
        if (animB == null) Debug.LogWarning($"<color=yellow>[Duel]</color> Animator missing on {duelistB.name}");

        if (animA != null) animA.SetTrigger("CatchPistol");
        if (animB != null) animB.SetTrigger("CatchPistol");
        yield return new WaitForSeconds(0.6f);

        if (animA != null) animA.SetTrigger("HoldPistol");
        if (animB != null) animB.SetTrigger("HoldPistol");

        pistolA = SpawnPistolInHand(duelistA);
        pistolB = SpawnPistolInHand(duelistB);

        if (pistolA != null) Debug.Log($"<color=orange>[Duel]</color> Pistol A spawned on {duelistA.name}");
        if (pistolB != null) Debug.Log($"<color=orange>[Duel]</color> Pistol B spawned on {duelistB.name}");

        yield return new WaitForSeconds(0.6f);

        if (animA != null) animA.SetTrigger("AimPistol");
        if (animB != null) animB.SetTrigger("AimPistol");

        float suspenseTime = Random.Range(suspenseTimeMin, suspenseTimeMax);
        yield return new WaitForSeconds(suspenseTime);

        if (suspenseAudioSource != null)
        {
            suspenseAudioSource.Stop();
            Destroy(suspenseAudioSource.gameObject);
            suspenseAudioSource = null;
        }

        bool playerInDuel = IsPlayer(duelistA) || IsPlayer(duelistB);

        if (playerInDuel)
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

        bool aIsBot = IsBot(duelistA);
        bool bIsBot = IsBot(duelistB);

        GameObject winner = null;
        GameObject loser = null;

        if (aIsBot && bIsBot)
        {

            yield return new WaitForSeconds(Random.Range(1.5f, 3f));
            if (Random.value < 0.5f) { winner = duelistA; loser = duelistB; }
            else { winner = duelistB; loser = duelistA; }
        }
        else if (!aIsBot && !bIsBot)
        {

            yield return WaitForPlayerShoot();
            winner = duelistA; loser = duelistB;
        }
        else
        {

            GameObject playerDuelist = aIsBot ? duelistB : duelistA;
            GameObject botDuelist = aIsBot ? duelistA : duelistB;

            dummyAimTarget = new GameObject("LightsOutDuelAimTarget");
            float charY = playerDuelist.transform.position.y;
            Plane horizontalPlane = new Plane(Vector3.up, new Vector3(0, charY, 0));

            PlayerRouletteAnimEvents pEvents = playerDuelist.GetComponent<PlayerRouletteAnimEvents>();
            if (pEvents != null)
            {
                pEvents.lookTarget = dummyAimTarget.transform;

                pEvents.lookWeight = 1f;
            }

            dummyAimTarget.transform.position = playerDuelist.transform.position + playerDuelist.transform.forward * 5f;

            UIManager.Instance.ShowSelectionUI(true);
            int participantLayerMask = LayerMask.GetMask("Participant");

            float botReactionTime = Random.Range(1.5f, 3f);
            float elapsed = 0f;
            bool playerShot = false;

            while (elapsed < botReactionTime)
            {

                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (horizontalPlane.Raycast(ray, out float dist))
                {
                    dummyAimTarget.transform.position = ray.GetPoint(dist);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (Physics.Raycast(ray, out RaycastHit clickHit, 25f, participantLayerMask))
                    {
                        Participant targetPart = clickHit.collider.GetComponentInParent<Participant>();
                        if (targetPart != null && targetPart.gameObject == botDuelist)
                        {
                            playerShot = true;
                            break;
                        }
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            UIManager.Instance.ShowSelectionUI(false);

            if (playerShot)
            {
                winner = playerDuelist;
                loser = botDuelist;
            }
            else
            {

                yield return RotateHolderUpperBody(botDuelist, playerDuelist, 1f, 0.15f);
                winner = botDuelist;
                loser = playerDuelist;
            }
        }

        Animator winnerAnim = winner.GetComponent<Animator>();
        PlayerRouletteAnimEvents winnerEvents = winner.GetComponent<PlayerRouletteAnimEvents>();

        bool hasPulledTrigger = false;
        if (winnerEvents != null)
        {
            winnerEvents.OnPullTrigger = () => hasPulledTrigger = true;
        }
        if (winnerAnim != null) winnerAnim.SetTrigger("ShootPistol");

        float timeout = 3f;
        while (!hasPulledTrigger && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        if (winnerEvents != null) winnerEvents.OnPullTrigger = null;

        if (muzzleFlashPrefab != null)
        {
            GameObject winnerPistol = (winner == duelistA) ? pistolA : pistolB;
            if (winnerPistol != null)
            {
                Transform barrelEnd = winnerPistol.transform.Find("BarrelEnd");
                Vector3 vfxPos = barrelEnd != null ? barrelEnd.position : winnerPistol.transform.position;
                Instantiate(muzzleFlashPrefab, vfxPos, winnerPistol.transform.rotation);
            }
        }

        if (bangSound != null)
            AudioSource.PlayClipAtPoint(bangSound, winner.transform.position);

        Participant loserPart = loser.GetComponent<Participant>();
        if (loserPart != null) loserPart.ReceiveShot(winner);

        Debug.Log($"[LightsOutDuel] {winner.name} CÂȘTIGĂ! {loser.name} a pierdut.");

        yield return new WaitForSeconds(1f);

        if (pistolA != null) Destroy(pistolA);
        if (pistolB != null) Destroy(pistolB);

        Animator animDuelistA = duelistA.GetComponent<Animator>();
        Animator animDuelistB = duelistB.GetComponent<Animator>();
        Participant partA = duelistA.GetComponent<Participant>();
        Participant partB = duelistB.GetComponent<Participant>();

        if (animDuelistA != null && partA != null && partA.isAlive) animDuelistA.SetTrigger("ReturnToSit");
        if (animDuelistB != null && partB != null && partB.isAlive) animDuelistB.SetTrigger("ReturnToSit");

        Coroutine resetA = GameManager.Instance.StartCoroutine(RotateHolderUpperBody(duelistA, null, 0f, 1f));
        Coroutine resetB = GameManager.Instance.StartCoroutine(RotateHolderUpperBody(duelistB, null, 0f, 1f));
yield return resetA;
        yield return resetB;

        if (dummyAimTarget != null) Destroy(dummyAimTarget);

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

        if (lightsOnSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(lightsOnSound, Camera.main.transform.position);

        yield return new WaitForSeconds(0.5f);

        GrimPlayerManager.BlockInventory = false;
        GameManager.Instance.StartPostEventWindow();
    }

    private bool IsBot(GameObject obj)
    {
        Participant p = obj.GetComponent<Participant>();
        return p != null && p.isBot;
    }

    private bool IsPlayer(GameObject obj)
    {
        Participant p = obj.GetComponent<Participant>();
        return p != null && !p.isBot;
    }

    private GameObject SpawnPistolInHand(GameObject holder)
    {
        Transform gunPoint = GetGunPoint(holder);
        if (gunPoint == null)
        {
            Debug.LogWarning($"[LightsOutDuel] Nu am găsit PlayerGunPoint pe {holder.name}!");
            return null;
        }

        GameObject pistol = Instantiate(pistolPrefab);
        
        GrimWeaponHandler grimWeapons = Object.FindFirstObjectByType<GrimWeaponHandler>();
        if (grimWeapons != null && grimWeapons.revolverInHand != null)
        {
            pistol.transform.localScale = grimWeapons.revolverInHand.transform.lossyScale;
        }

        pistol.transform.SetParent(gunPoint, true);
        pistol.transform.localPosition = Vector3.zero;
        pistol.transform.localRotation = Quaternion.identity;
        
        return pistol;
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

    private IEnumerator WaitForPlayerShoot()
    {
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }
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

    private IEnumerator RotateHolderUpperBody(GameObject holder, GameObject target, float targetWeight, float duration)
    {
        if (holder == null) yield break;
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
