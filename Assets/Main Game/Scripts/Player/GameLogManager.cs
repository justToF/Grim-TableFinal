using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameLogManager : MonoBehaviour
{
    public static GameLogManager Instance;

    [Header("Settings")]
    public GameObject logEntryPrefab;
    public Transform logContainer;
    public int maxMessages = 10;
    public float messageDuration = 5f;
    public float fadeDuration = 1f;

    private List<GameObject> activeMessages = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    public void AddLog(string message)
    {
        if (logEntryPrefab == null || logContainer == null) return;

        GameObject entry = Instantiate(logEntryPrefab, logContainer);
        TextMeshProUGUI textComp = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            textComp.text = message;
        }

        activeMessages.Add(entry);

        if (activeMessages.Count > maxMessages)
        {
            GameObject oldest = activeMessages[0];
            activeMessages.RemoveAt(0);
            Destroy(oldest);
        }

        StartCoroutine(FadeAndDestroy(entry));
    }

    private IEnumerator FadeAndDestroy(GameObject entry)
    {
        CanvasGroup cg = entry.GetComponent<CanvasGroup>();
        if (cg == null) cg = entry.AddComponent<CanvasGroup>();

        yield return new WaitForSeconds(messageDuration);

        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            if (entry == null) yield break;
            elapsed += Time.deltaTime;
            cg.alpha = 1 - (elapsed / fadeDuration);
            yield return null;
        }

        if (entry != null)
        {
            activeMessages.Remove(entry);
            Destroy(entry);
        }
    }
}
