using UnityEngine;

public class HeadFollow : MonoBehaviour
{
    public Transform headBone;
    public Transform playerCamera;

    [Range(0, 1)]
    public float followIntensity = 0.5f;
    public float maxRotation = 45f;

    private Participant participant;

    void Start()
    {
        participant = GetComponentInParent<Participant>();
    }

    void LateUpdate()
    {
        if (headBone == null || playerCamera == null) return;

        if (participant != null && !participant.isAlive) return;

        float targetY = playerCamera.localEulerAngles.y;
        float targetX = playerCamera.localEulerAngles.x;

        if (targetY > 180) targetY -= 360;
        if (targetX > 180) targetX -= 360;

        float finalY = Mathf.Clamp(targetY * followIntensity, -maxRotation, maxRotation);
        float finalX = Mathf.Clamp(targetX * followIntensity, -maxRotation, maxRotation);

        headBone.localRotation = Quaternion.Euler(finalX, finalY, 0);
    }
}
