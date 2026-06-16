using UnityEngine;

[DefaultExecutionOrder(100)]
public class LateWeaponConstraint : MonoBehaviour
{
    public Transform targetBone; 

    [Tooltip("Dacă e true, calculează offset-uri din Awake (pentru poziționare fină). Dacă e false, urmărește exact osul.")]
    public bool useAwakeOffsets = true;

    private Vector3 posOffset;
    private Quaternion rotOffset;

    void Awake()
    {
        if (targetBone != null && useAwakeOffsets)
        {
            posOffset = targetBone.InverseTransformPoint(transform.position);
            rotOffset = Quaternion.Inverse(targetBone.rotation) * transform.rotation;
        }
        else
        {
            posOffset = Vector3.zero;
            rotOffset = Quaternion.identity;
        }
    }

    public void RecalculateOffsets()
    {
        if (targetBone != null)
        {
            posOffset = targetBone.InverseTransformPoint(transform.position);
            rotOffset = Quaternion.Inverse(targetBone.rotation) * transform.rotation;
        }
    }

    public void SnapToTarget()
    {
        posOffset = Vector3.zero;
        rotOffset = Quaternion.identity;
    }

    void LateUpdate()
    {
        if (targetBone != null)
        {
            transform.position = targetBone.TransformPoint(posOffset);
            transform.rotation = targetBone.rotation * rotOffset;
        }
    }
}
