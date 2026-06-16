using UnityEngine;
using System.Collections;

public class GrimWeaponHandler : MonoBehaviour
{
    [Header("References")]
    public GameObject revolverInHand; 
    public Animator revolverAnimator; 

    [Header("Efecte Tragere")]
    public Transform barrelEnd;
    public GameObject muzzleFlashPrefab;
    public AudioSource gunAudioSource;
    public AudioClip shootSound;
    public AudioClip drawSound;
    public AudioClip spinSound;
    [Range(0f, 1f)] public float shootVolume = 0.4f;
    [Range(0f, 1f)] public float drawVolume = 0.4f;
    [Range(0f, 1f)] public float spinVolume = 0.4f;

    [Header("Glont")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 25f;
    public float bulletScale = 1f;
    public Vector3 bulletRotationOffset = new Vector3(90f, 0f, 0f);

    private GameObject currentTarget;
    public System.Action OnThrowGun;

    private bool playReloadSoundOnDraw = false;

    public void SetTarget(GameObject target)
    {
        currentTarget = target;
    }

    public void SetPlayReloadSoundOnDraw(bool play)
    {
        playReloadSoundOnDraw = play;
    }

    public void ShowGun()
    {
        if (revolverInHand != null) revolverInHand.SetActive(true);
    }

    public void HideGun()
    {
        if (revolverInHand != null) revolverInHand.SetActive(false);
    }

    public void AnimEvent_DrawGun()
    {
        ShowGun();
        if (gunAudioSource != null && drawSound != null && playReloadSoundOnDraw)
        {
            gunAudioSource.pitch = 1.0f;
            gunAudioSource.PlayOneShot(drawSound, drawVolume);
        }
    }

    public void AnimEvent_OpenReloader()
    {
        if (revolverAnimator != null) revolverAnimator.SetTrigger("OpenReloader");
    }

    public void AnimEvent_SpinCylinder()
    {
        if (revolverAnimator != null) revolverAnimator.SetTrigger("SpinCylinder");
        
        if (gunAudioSource != null && spinSound != null)
        {
            gunAudioSource.pitch = 0.7f;
            gunAudioSource.PlayOneShot(spinSound, spinVolume);
        }
    }

    public void AnimEvent_StopSpinCylinder()
    {

        if (revolverAnimator != null) revolverAnimator.speed = 0f; 
    }

    public void AnimEvent_CloseReloader()
    {
        if (revolverAnimator != null) 
        {
            revolverAnimator.speed = 1f;
            revolverAnimator.SetTrigger("CloseReloader");
        }
    }

    public void AnimEvent_ThrowGun()
    {

        OnThrowGun?.Invoke();
    }

    public void AnimEvent_FireGun()
    {

        if (revolverAnimator != null) revolverAnimator.SetTrigger("Shot1"); 

        if (muzzleFlashPrefab != null && barrelEnd != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, barrelEnd.position, barrelEnd.rotation);

            ParticleSystem ps = flash.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.loop = false;
            }
            Destroy(flash, 1.5f);
        }

        if (gunAudioSource != null && shootSound != null) 
        {
            gunAudioSource.pitch = 1.0f;
            gunAudioSource.PlayOneShot(shootSound, shootVolume);
        }

        if (currentTarget != null)
        {
            Participant targetParticipant = currentTarget.GetComponent<Participant>();
            if (targetParticipant != null) 
            {

                targetParticipant.ReceiveShot();
            }

            if (bulletPrefab != null && barrelEnd != null)
            {

                GameObject bullet = Instantiate(bulletPrefab, barrelEnd.position, barrelEnd.rotation);
                StartCoroutine(AnimateBulletFlightVisual(bullet, currentTarget));
            }
        }
    }

    private IEnumerator AnimateBulletFlightVisual(GameObject bullet, GameObject target)
    {
        Vector3 startPos = bullet.transform.position;
        Vector3 targetPos = target.transform.position + Vector3.up * 1.5f; 

        Transform[] allBones = target.GetComponentsInChildren<Transform>();
        foreach (Transform bone in allBones)
        {
            if (bone.name.ToLower().Contains("head"))
            {
                targetPos = bone.position;
                break;
            }
        }

        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / bulletSpeed;
        float elapsed = 0f;

        bullet.transform.localScale = Vector3.one * bulletScale;

        while (elapsed < duration)
        {
            if (bullet == null) yield break; 
            
            elapsed += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            
            bullet.transform.LookAt(targetPos);
            bullet.transform.Rotate(bulletRotationOffset, Space.Self);
            
            yield return null; 
        }

        if (bullet != null) Destroy(bullet); 
    }

    public void AnimEvent_HideGun()
    {
        HideGun();
    }
}
