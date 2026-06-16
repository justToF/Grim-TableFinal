using UnityEngine;
using System;

public class ParticipantAnimationHelper : MonoBehaviour
{
    public Transform handPoint;

    [Header("Bones for Generic IK")]
    public Transform headBone;
    public Transform spineBone;
    public Vector3 headOffset = new Vector3(0, 0, 0);
    public Vector3 spineOffset = new Vector3(0, 0, 0);
    [Header("Look Tuning")]
    [Range(0f, 1f)] public float headLookAmount = 0.35f;
    [Range(0f, 1f)] public float spineLookAmount = 0.20f;
    public float maxHeadYaw = 20f;
    public float maxHeadPitch = 12f;
    public float maxSpineYaw = 10f;
    public float maxSpinePitch = 7f;
    public float lookBlendSpeed = 8f;
    public float returnToInitialSpeed = 10f;

    private GameObject attachedObject;

    public event Action OnReleaseConfirmed;
    public event Action OnGrabConfirmed;

    private Transform lookTarget;
    private Vector3 smoothedTargetPos;
    private float currentLookWeight = 0f;
    private float targetLookWeight = 0f;

    private Quaternion initialHeadLocalRotation = Quaternion.identity;
    private Quaternion initialSpineLocalRotation = Quaternion.identity;

    private void Awake()
    {

        if (headBone != null) initialHeadLocalRotation = headBone.localRotation;
        if (spineBone != null) initialSpineLocalRotation = spineBone.localRotation;
    }

    public void OnGrabItem()
    {
        GameObject grenade = HotPotatoAction.ActiveBomb != null
            ? HotPotatoAction.ActiveBomb
            : GameObject.FindWithTag("Grenade");
        if (grenade != null)
        {
            attachedObject = grenade;
            attachedObject.transform.SetParent(handPoint);
            attachedObject.transform.localPosition = Vector3.zero;
            attachedObject.transform.localRotation = Quaternion.identity;

            OnGrabConfirmed?.Invoke();
        }
    }

    public void OnReleaseItem()
    {
        if (attachedObject != null)
        {
            attachedObject.transform.SetParent(null);
            attachedObject = null;
            OnReleaseConfirmed?.Invoke();
        }
    }

    public void LookAtTarget(Transform target)
    {
        lookTarget = target;
        targetLookWeight = 1f; 

        if (target != null) smoothedTargetPos = target.position + Vector3.up * 1.4f;
    }

    public void ResetLook()
    {
        targetLookWeight = 0f; 
    }

    private void LateUpdate()
    {

        currentLookWeight = Mathf.MoveTowards(currentLookWeight, targetLookWeight, Time.deltaTime * lookBlendSpeed);

        if (currentLookWeight > 0.001f && lookTarget != null)
        {
            Vector3 rawTargetPos = lookTarget.position + Vector3.up * 1.4f;
            
            smoothedTargetPos = Vector3.Lerp(smoothedTargetPos, rawTargetPos, Time.deltaTime * 12f);

            if (spineBone != null)
            {
                Vector3 dirSpine = smoothedTargetPos - spineBone.position;
                if (dirSpine != Vector3.zero)
                {
                    Quaternion targetRotation = GetClampedTargetLocalRotation(
                        spineBone,
                        initialSpineLocalRotation,
                        dirSpine,
                        spineOffset,
                        maxSpineYaw,
                        maxSpinePitch
                    );
                    
                    float t = spineLookAmount * currentLookWeight;
                    spineBone.localRotation = Quaternion.Slerp(spineBone.localRotation, targetRotation, t);
                }
            }

            if (headBone != null)
            {
                Vector3 dirHead = smoothedTargetPos - headBone.position;
                if (dirHead != Vector3.zero)
                {
                    Quaternion targetRotation = GetClampedTargetLocalRotation(
                        headBone,
                        initialHeadLocalRotation,
                        dirHead,
                        headOffset,
                        maxHeadYaw,
                        maxHeadPitch
                    );
                    
                    float t = headLookAmount * currentLookWeight;
                    headBone.localRotation = Quaternion.Slerp(headBone.localRotation, targetRotation, t);
                }
            }
        }
else if (currentLookWeight > 0)
        {

            if (spineBone != null)
            {
                spineBone.localRotation = Quaternion.Slerp(
                    spineBone.localRotation,
                    initialSpineLocalRotation,
                    1f - currentLookWeight
                );
            }

            if (headBone != null)
            {
                headBone.localRotation = Quaternion.Slerp(
                    headBone.localRotation,
                    initialHeadLocalRotation,
                    1f - currentLookWeight
                );
            }
        }
    }

    private Quaternion GetClampedTargetLocalRotation(
        Transform bone,
        Quaternion initialLocalRotation,
        Vector3 worldDirectionToTarget,
        Vector3 offsetEuler,
        float maxYaw,
        float maxPitch)
    {
        Quaternion worldLook = Quaternion.LookRotation(worldDirectionToTarget) * Quaternion.Euler(offsetEuler);
        Quaternion localLook = bone.parent != null
            ? Quaternion.Inverse(bone.parent.rotation) * worldLook
            : worldLook;
        Quaternion deltaFromInitial = Quaternion.Inverse(initialLocalRotation) * localLook;

        Vector3 deltaEuler = NormalizeEuler(deltaFromInitial.eulerAngles);
        float clampedPitch = Mathf.Clamp(deltaEuler.x, -maxPitch, maxPitch);
        float clampedYaw = Mathf.Clamp(deltaEuler.y, -maxYaw, maxYaw);

        Quaternion clampedDelta = Quaternion.Euler(clampedPitch, clampedYaw, 0f);
        return initialLocalRotation * clampedDelta;
    }

    private static Vector3 NormalizeEuler(Vector3 euler)
    {
        return new Vector3(
            NormalizeAngle(euler.x),
            NormalizeAngle(euler.y),
            NormalizeAngle(euler.z)
        );
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
