using UnityEngine;

public class HandFanLayout : MonoBehaviour
{
    public float spacing = 150f;
    public float maxRotation = 15f;
    public float heightOffset = 30f;

    public void UpdateLayout()
    {
        int childCount = transform.childCount;
        float halfCount = (childCount - 1) / 2f;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null) continue;

            float indexFactor = i - halfCount;
            child.anchoredPosition = new Vector2(indexFactor * spacing, -Mathf.Abs(indexFactor) * heightOffset);
            child.localRotation = Quaternion.Euler(0, 0, -indexFactor * maxRotation);
        }
    }
}
