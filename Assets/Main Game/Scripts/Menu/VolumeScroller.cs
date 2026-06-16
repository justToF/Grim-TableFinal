using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class VolumeScroller : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private float scrollSensitivity = 1f;
    [SerializeField] private int minValue = 0;
    [SerializeField] private int maxValue = 100;

    private bool isHovered = false;
    private MainMenuController mainMenuController;

    private void Awake()
    {
        mainMenuController = Object.FindFirstObjectByType<MainMenuController>();
        if (inputField == null) inputField = GetComponent<TMP_InputField>();
        
        inputField.onEndEdit.AddListener(OnValueEntered);
        
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        inputField.text = Mathf.RoundToInt(savedVolume * 100f).ToString();
    }

    private void Update()
    {
        if (isHovered && !inputField.isFocused)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (int.TryParse(inputField.text, out int currentVal))
                {
                    int newVal = Mathf.Clamp(currentVal + Mathf.RoundToInt(scroll * scrollSensitivity), minValue, maxValue);
                    inputField.text = newVal.ToString();
                    UpdateVolume(newVal);
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    private void OnValueEntered(string value)
    {
        if (int.TryParse(value, out int result))
        {
            int clamped = Mathf.Clamp(result, minValue, maxValue);
            inputField.text = clamped.ToString();
            UpdateVolume(clamped);
        }
        else
        {

            float currentVol = AudioListener.volume * 100f;
            inputField.text = Mathf.RoundToInt(currentVol).ToString();
        }
    }

    private void UpdateVolume(int value)
    {
        if (mainMenuController != null)
        {
            mainMenuController.SetMainVolume(value / 100f);
        }
    }
}
