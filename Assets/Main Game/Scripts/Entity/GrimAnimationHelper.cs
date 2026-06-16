using UnityEngine;

public class GrimAnimationHelper : MonoBehaviour
{
    public Transform handPoint;
    public GameObject pistolPrefab;
    public GameObject bombPrefab;

    [HideInInspector] public GameObject currentItem;
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
        anim.ResetTrigger("StartAction");
    }

    public void SpawnItemInHand()
    {
        int type = anim.GetInteger("ItemType");
        GameObject prefab = (type == 0) ? pistolPrefab : bombPrefab;

        if (currentItem != null) Destroy(currentItem);

        currentItem = Instantiate(prefab, handPoint);
        currentItem.transform.localPosition = Vector3.zero;
        currentItem.transform.localRotation = Quaternion.identity;

        currentItem.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        Debug.Log("Grim a scos: " + (type == 0 ? "Pistol" : "Grenada"));
    }
    public void ReleaseItem()
    {
        if (currentItem != null)
        {
            currentItem.transform.SetParent(null);

        }
    }

    public void ClearItem()
    {
        if (currentItem != null)
        {
            Destroy(currentItem);
            currentItem = null;
        }
    }
}
