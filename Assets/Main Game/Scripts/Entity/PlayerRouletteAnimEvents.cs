using UnityEngine;
using System;

[DefaultExecutionOrder(-50)] 
public class PlayerRouletteAnimEvents : MonoBehaviour
{
    public Action OnPullTrigger;
    public Action OnThrowPistol;

    [HideInInspector] public Transform lookTarget;
    [HideInInspector] public float lookWeight = 0f;
    
    [Tooltip("Ajustează stânga-dreapta DOAR pentru braț/pistol (ex: 90 sau -90)")]
    public float aimAngleOffset = 0f;
    
    [Tooltip("Cât de puternică e rotația. (Normal e 1)")]
    public float aimMultiplier = 1f;

    [Header("Oase")]
    public Transform armBone;
    public Transform clavicleBone;
    public Transform neckBone;
    public Transform headBone;

    private LateWeaponConstraint gunPointConstraint;
    
    private bool isPlayer = false;

    void Start()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null && anim.isHuman)
        {
            if (headBone == null) headBone = anim.GetBoneTransform(HumanBodyBones.Head);
            if (neckBone == null) neckBone = anim.GetBoneTransform(HumanBodyBones.Neck);
        }

        if (headBone == null) headBone = RecursiveFindChild(transform, "CC_Base_Head");
        
        if (neckBone == null) 
        {
            neckBone = RecursiveFindChild(transform, "CC_Base_NeckTwist01");
            if (neckBone == null) neckBone = RecursiveFindChild(transform, "CC_Base_NeckTwist02");
        }

        if (armBone == null) FindDynamicArm();
        if (clavicleBone == null && armBone != null) clavicleBone = armBone.parent;

        Transform gunPoint = RecursiveFindChild(transform, "PlayerGunPoint");
        if (gunPoint != null)
        {
            gunPointConstraint = gunPoint.GetComponent<LateWeaponConstraint>();
        }

        if (headBone == null) Debug.LogWarning($"[{gameObject.name}] Nu am găsit osul capului!");
        if (neckBone == null) Debug.LogWarning($"[{gameObject.name}] Nu am găsit osul gâtului!");

        Participant part = GetComponent<Participant>();
        if (part != null && !part.isBot)
        {
            isPlayer = true;
        }
    }

    private void FindDynamicArm()
    {
        Transform gunPoint = RecursiveFindChild(transform, "PlayerGunPoint");
        if (gunPoint != null)
        {
            Transform curr = gunPoint;
            while (curr != null && curr != transform)
            {
                if (curr.name.ToLower().Contains("upperarm"))
                {
                    armBone = curr;
                    clavicleBone = armBone.parent;
                    return;
                }
                curr = curr.parent;
            }

            LateWeaponConstraint constraint = gunPoint.GetComponent<LateWeaponConstraint>();
            if (constraint != null && constraint.targetBone != null)
            {
                Transform handBone = constraint.targetBone;
                curr = handBone;
                
                while (curr != null && curr != transform)
                {
                    if (curr.name.ToLower().Contains("upperarm"))
                    {
                        armBone = curr;
                        clavicleBone = armBone.parent;
                        return;
                    }
                    curr = curr.parent;
                }
            }
        }
    }

    private Transform RecursiveFindChild(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        foreach (Transform child in parent)
        {
            Transform found = RecursiveFindChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    public void AnimEvent_PullTrigger()
    {
        OnPullTrigger?.Invoke();
    }
    public void AnimEvent_ThrowPistol() 
    {
        OnThrowPistol?.Invoke();
    }

    void LateUpdate()
    {

        if (lookWeight <= 0f || lookTarget == null) return;

        Vector3 dir = lookTarget.position - transform.position;
        dir.y = 0;

        if (dir != Vector3.zero)
        {
            float baseAngle = 0f;

            if (isPlayer)
            {

                Vector3 armForward = armBone != null ? armBone.transform.up : transform.forward;
                armForward.y = 0;
                if (armForward.sqrMagnitude < 0.001f) armForward = transform.forward;
                armForward.Normalize();

                baseAngle = Vector3.SignedAngle(armForward, dir, Vector3.up);
            }
            else
            {

                baseAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
            }

            float armAngle = (baseAngle * aimMultiplier) + aimAngleOffset;
            float headAngle = baseAngle * aimMultiplier;

            Quaternion addHead = Quaternion.AngleAxis(headAngle * 0.6f * lookWeight, Vector3.up);
            Quaternion addNeck = Quaternion.AngleAxis(headAngle * 0.4f * lookWeight, Vector3.up);
            
            Quaternion addClavicle = Quaternion.AngleAxis(armAngle * 0.5f * lookWeight, Vector3.up);
            Quaternion addArm = Quaternion.AngleAxis(armAngle * 0.5f * lookWeight, Vector3.up);

            if (clavicleBone != null) clavicleBone.rotation = addClavicle * clavicleBone.rotation;
            if (armBone != null) armBone.rotation = addArm * armBone.rotation;
            
            if (neckBone != null) neckBone.rotation = addNeck * neckBone.rotation;
            if (headBone != null) headBone.rotation = addHead * headBone.rotation;
        }
    }
}
