using UnityEngine;
using UnityEngine.EventSystems;

public class CardFanController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Setari Animatie")]
    public float scaleFactor = 1.15f;
    public float liftAmount = 30f;
    public float smoothSpeed = 10f;

    private Vector3 initialScale;
    private Vector3 targetScale;

    private Vector3 initialPosition;
    private Vector3 targetPosition;

    private bool isSelected = false;
    private bool isHovered = false;
    private bool hasSavedSiblingIndex = false;
    private int initialSiblingIndex;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialScale = transform.localScale;
        targetScale = initialScale;

        initialPosition = rectTransform.anchoredPosition;
        targetPosition = initialPosition;
    }

    void Update()
    {

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smoothSpeed);
        rectTransform.anchoredPosition = Vector3.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * smoothSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateState();
    }

    private void UpdateState()
    {
        if (isSelected || isHovered)
        {
            targetScale = initialScale * scaleFactor;
            targetPosition = initialPosition + new Vector3(0, liftAmount, 0);

            if (!hasSavedSiblingIndex)
            {
                initialSiblingIndex = transform.GetSiblingIndex();
                hasSavedSiblingIndex = true;
            }
            transform.SetAsLastSibling();
        }
        else
        {
            targetScale = initialScale;
            targetPosition = initialPosition;

            if (hasSavedSiblingIndex)
            {
                transform.SetSiblingIndex(initialSiblingIndex);
                hasSavedSiblingIndex = false;
            }
        }
    }
}
