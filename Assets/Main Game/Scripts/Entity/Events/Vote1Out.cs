using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Vote1Out Action", menuName = "Entity/Actions/Vote1Out")]
public class Vote1Out : EntityAction
{
    public float votingDuration = 10f;
    
    public override void ExecuteAction(GameObject startingTarget)
    {
        GameManager.Instance.StartCoroutine(VoteRoutine());
    }
    
    private IEnumerator VoteRoutine()
    {
        List<GameObject> allParticipants = GameManager.Instance.participants
            .Where(p => p.GetComponent<Participant>().isAlive)
            .ToList();
            
        GameObject dealer = GameObject.Find("grim");
        Animator grimAnim = dealer != null ? dealer.GetComponent<Animator>() : null;
        
        if (grimAnim != null)
        {
            grimAnim.SetInteger("ItemType", 0);
        }
        
        Dictionary<GameObject, PlayerRouletteAnimEvents> animEventsMap = new Dictionary<GameObject, PlayerRouletteAnimEvents>();
        Dictionary<GameObject, GameObject> dummyTargets = new Dictionary<GameObject, GameObject>();
        Dictionary<GameObject, GameObject> currentVotes = new Dictionary<GameObject, GameObject>();
        
        GameObject player = null;

        foreach (GameObject p in allParticipants)
        {
            Animator pAnim = p.GetComponent<Animator>();
            if (pAnim != null) pAnim.SetTrigger("PointFinger"); 
            
            PlayerRouletteAnimEvents animEvents = p.GetComponent<PlayerRouletteAnimEvents>();
            if (animEvents != null) animEvents.lookWeight = 0f;
            animEventsMap[p] = animEvents;
            
            GameObject dummy = new GameObject("DummyVoteAim_" + p.name);
            dummy.transform.position = p.transform.position + p.transform.forward * 5f;
            dummyTargets[p] = dummy;
            
            if (animEvents != null) animEvents.lookTarget = dummy.transform;
            currentVotes[p] = null;
            
            if (p.GetComponent<BotBrain>() == null) player = p;
        }

        yield return new WaitForSeconds(0.6f);

        if (player != null && UIManager.Instance != null)
        {
            UIManager.Instance.ShowSelectionUI(true);
        }

        float timer = votingDuration;
        int participantLayerMask = LayerMask.GetMask("Participant");
        
        float rampDuration = 0.5f;
        float rampElapsed = 0f;

        while (timer > 0)
        {
            timer -= Time.deltaTime;

            if (rampElapsed < rampDuration)
            {
                rampElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(rampElapsed / rampDuration);
                t = t * t * (3f - 2f * t);
                foreach (var ev in animEventsMap.Values)
                {
                    if (ev != null) ev.lookWeight = Mathf.Lerp(0f, 1f, t);
                }
            }

            foreach (GameObject p in allParticipants)
            {
                BotBrain botBrain = p.GetComponent<BotBrain>();
                Participant part = p.GetComponent<Participant>();
                
                if (botBrain != null)
                {

                    if (GameManager.Instance.activeSpecialRoundID == "VOTE_PRESENTATION")
                    {
                        GameObject scriptedTarget = null;
                        if (part.participantName == "Andrew")
                        {
                            scriptedTarget = player;
                        }
                        else if (part.participantName == "Isabelle")
                        {

                            if (votingDuration - timer < 2f)
                                scriptedTarget = player;
                            else
                                scriptedTarget = allParticipants.Find(t => t.GetComponent<Participant>().participantName == "Andrew");
                        }
                        else if (part.participantName == "Amy")
                        {
                            scriptedTarget = allParticipants.Find(t => t.GetComponent<Participant>().participantName == "Andrew");
                        }

                        if (scriptedTarget != null && scriptedTarget.GetComponent<Participant>().isTargetable)
                        {
                            currentVotes[p] = scriptedTarget;
                        }
                        else
                        {

                            List<GameObject> validTargets = allParticipants.Where(t => t != p && t.GetComponent<Participant>().isTargetable).ToList();
                            if (validTargets.Count > 0) currentVotes[p] = validTargets[Random.Range(0, validTargets.Count)];
                        }
                    }
                    else if (currentVotes[p] == null || Random.Range(0f, 100f) < 5f * Time.deltaTime)
                    {
                        List<GameObject> validTargets = allParticipants.Where(t => t != p && t.GetComponent<Participant>().isTargetable).ToList();
                        if (validTargets.Count > 0)
                        {
                            currentVotes[p] = validTargets[Random.Range(0, validTargets.Count)];
                        }
                    }

                    if (currentVotes[p] != null)
                    {
                        Vector3 targetPos = currentVotes[p].transform.position + Vector3.up * 1.2f;
                        dummyTargets[p].transform.position = Vector3.Lerp(dummyTargets[p].transform.position, targetPos, Time.deltaTime * 3f);
                    }
                }
                else
                {

                    float charY = p.transform.position.y + 1.2f;
                    Plane horizontalPlane = new Plane(Vector3.up, new Vector3(0, charY, 0));
                    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                    
                    if (horizontalPlane.Raycast(ray, out float dist))
                    {

                        dummyTargets[p].transform.position = ray.GetPoint(dist);
                    }

                    currentVotes[p] = null;
                    if (Physics.Raycast(ray, out RaycastHit hit, 25f, participantLayerMask))
                    {
                        Participant targetPart = hit.collider.GetComponentInParent<Participant>();
                        if (targetPart != null && targetPart.gameObject != p && targetPart.isTargetable)
                        {
                            currentVotes[p] = targetPart.gameObject;
                        }
                    }
                }
            }
            
            yield return null;
        }

        if (player != null)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowSelectionUI(false);
            
            if (currentVotes[player] == null)
            {
                List<GameObject> bots = allParticipants.Where(p => p != player && p.GetComponent<Participant>().isTargetable).ToList();
                if (bots.Count > 0)
                {
                    currentVotes[player] = bots[Random.Range(0, bots.Count)];
                }
            }
        }

        float lockTimer = 0.5f;
        while (lockTimer > 0)
        {
            lockTimer -= Time.deltaTime;
            foreach (GameObject p in allParticipants)
            {
                if (currentVotes[p] != null)
                {
                    Vector3 finalPos = currentVotes[p].transform.position + Vector3.up * 1.2f;
                    dummyTargets[p].transform.position = Vector3.Lerp(dummyTargets[p].transform.position, finalPos, Time.deltaTime * 10f);
                }
            }
            yield return null;
        }

        yield return new WaitForSeconds(1.5f); 

        Dictionary<GameObject, int> voteCount = new Dictionary<GameObject, int>();
        foreach (GameObject p in allParticipants) voteCount[p] = 0;
        
        foreach (var vote in currentVotes.Values)
        {
            if (vote != null && voteCount.ContainsKey(vote))
            {
                voteCount[vote]++;
            }
        }

        int maxVotes = voteCount.Values.Max();
        List<GameObject> mostVotedParticipants = voteCount.Where(kvp => kvp.Value == maxVotes).Select(kvp => kvp.Key).ToList();

        GameObject targetToEliminate = mostVotedParticipants[Random.Range(0, mostVotedParticipants.Count)];

        Debug.Log($"<color=yellow><b>[Vote1Out] ELIMINAT: {targetToEliminate.name} cu {maxVotes} voturi</b></color>");

        foreach (var kvp in currentVotes)
        {
            if (kvp.Value != null && kvp.Key != kvp.Value)
            {
                BotGrudgeSystem votedGrudge = kvp.Value.GetComponent<BotGrudgeSystem>();
                if (votedGrudge != null) votedGrudge.OnVotedAgainst(kvp.Key);
            }
        }

        foreach (GameObject p in allParticipants)
        {
            Animator pAnim = p.GetComponent<Animator>();
            if (pAnim != null) pAnim.SetTrigger("t_Reset");
            
            if (animEventsMap[p] != null)
            {
                GameManager.Instance.StartCoroutine(FadeOutLookWeight(animEventsMap[p], 0.5f));
            }
        }

        foreach (var dummy in dummyTargets.Values)
        {
            if (dummy != null) Destroy(dummy, 1f);
        }

        yield return new WaitForSeconds(1.0f);

        if (dealer != null)
        {

            EntityController entityCtrl = GameManager.Instance.entityVisual;
            if (entityCtrl != null)
            {
                entityCtrl.StopAndLookAt(targetToEliminate.transform.position);
                yield return new WaitForSeconds(1.6f);
            }

            ShootAction shootAction = null;
            if (GameManager.Instance != null && GameManager.Instance.possibleActions != null)
            {
                shootAction = GameManager.Instance.possibleActions.Find(a => a is ShootAction) as ShootAction;
            }

            if (shootAction != null)
            {
                GameManager.Instance.StartCoroutine(shootAction.ExecuteActionAnimated(dealer, targetToEliminate, true));
            }
            else
            {

                GameManager.Instance.StartCoroutine(ExecuteShootFallback(dealer, targetToEliminate));
            }
        }
        else
        {
            targetToEliminate.GetComponent<Participant>().ReceiveShot();
            GameManager.Instance.Invoke("StartPostEventWindow", 0f);
        }
    }

    private IEnumerator FadeOutLookWeight(PlayerRouletteAnimEvents animEvents, float duration)
    {
        float startWeight = animEvents.lookWeight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (animEvents != null) animEvents.lookWeight = Mathf.Lerp(startWeight, 0f, elapsed / duration);
            yield return null;
        }
        if (animEvents != null) 
        {
            animEvents.lookWeight = 0f;
            animEvents.lookTarget = null;
        }
    }

    private IEnumerator ExecuteShootFallback(GameObject grim, GameObject target)
    {
        GrimWeaponHandler weaponHandler = grim.GetComponent<GrimWeaponHandler>();
        Animator grimAnim = grim.GetComponent<Animator>();
        
        if (weaponHandler != null) weaponHandler.SetTarget(target);

        if (grimAnim != null) grimAnim.SetTrigger("PrepareShot");
        yield return new WaitForSeconds(3f); 

        if (grimAnim != null) grimAnim.SetTrigger("FireShot");
        yield return new WaitForSeconds(2f);

        if (grimAnim != null) grimAnim.SetTrigger("ReturnIdle");
        yield return new WaitForSeconds(1.5f);

        GameManager.Instance.Invoke("StartPostEventWindow", 0f);
    }
}
