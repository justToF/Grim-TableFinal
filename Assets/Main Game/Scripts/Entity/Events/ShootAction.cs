using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Shoot Action", menuName = "Entity/Actions/Shoot")]
public class ShootAction : EntityAction
{
    [Header("Sunete")]
    public AudioClip emptyClickSound;
    [Range(0f, 1f)] public float emptyClickVolume = 0.6f;

    public override void ExecuteAction(GameObject target)
    {
        GameObject entity = GameManager.Instance.entityVisual.gameObject;

        GameManager.Instance.StartCoroutine(ExecuteActionAnimated(entity, target, false));
    }

    public IEnumerator ExecuteActionAnimated(GameObject entity, GameObject target, bool forceShoot = false, float aimDuration = 3f)
    {
        GrimWeaponHandler weaponHandler = Object.FindFirstObjectByType<GrimWeaponHandler>();
        if (weaponHandler != null) weaponHandler.SetPlayReloadSoundOnDraw(false);
        
        Animator grimAnim = weaponHandler != null ? weaponHandler.GetComponent<Animator>() : null;
        
        if (grimAnim != null) 
        {
            grimAnim.SetTrigger("PrepareShot");
        }
        
        yield return new WaitForSeconds(aimDuration); 

        bool willShoot = forceShoot || (Random.value < 0.5f);

        if (willShoot)
        {

            if (weaponHandler != null) weaponHandler.SetTarget(target);

            Debug.Log("<color=red>[ShootAction] BANG!</color>");
            Participant p = target.GetComponent<Participant>();
            string pName = p != null ? p.participantName : target.name;
            UIManager.Instance.AddLog("BANG! " + pName + " was shot!");

            if (grimAnim != null) 
            {
                grimAnim.SetTrigger("FireShot");
            }

            yield return new WaitForSeconds(2f);
        }
        else
        {
            Debug.Log("<color=green>[ShootAction] CLICK (Gol)!</color>");
            Participant p = target.GetComponent<Participant>();
            string pName = p != null ? p.participantName : target.name;
            UIManager.Instance.AddLog("CLICK! " + pName + " survived!");

            if (weaponHandler != null && weaponHandler.gunAudioSource != null && emptyClickSound != null)
            {
                weaponHandler.gunAudioSource.PlayOneShot(emptyClickSound, emptyClickVolume);
            }
            yield return new WaitForSeconds(1f);
        }

        if (grimAnim != null) grimAnim.SetTrigger("ReturnIdle");

        yield return new WaitForSeconds(1.5f);

        GameManager.Instance.Invoke("StartPostEventWindow", 0f);
    }
}
