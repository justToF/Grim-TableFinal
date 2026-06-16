using UnityEngine;
using System.Collections.Generic;

public partial class DeckVisualizer : MonoBehaviour
{
    [Header("Referințe")]
    public GameObject cardPrefab;

    [Header("Setări Pachet")]
    public int cardCount = 40;
    public float cardThickness = 0.002f;
    
    [Header("Aspect Natural")]
    public Vector3 baseRotation = new Vector3(-90f, 0f, 0f);
    public float rotationJitter = 1.5f;
    public float positionJitter = 0.001f;

    private List<GameObject> spawnedCards = new List<GameObject>();

    void Start()
    {
        GenerateDeck();
    }

    public void GenerateDeck()
    {
        foreach (GameObject c in spawnedCards) Destroy(c);
        spawnedCards.Clear();

        for (int i = 0; i < cardCount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-positionJitter, positionJitter),
                i * cardThickness,
                Random.Range(-positionJitter, positionJitter)
            );

            Quaternion randomRot = Quaternion.Euler(
                baseRotation.x, 
                baseRotation.y + Random.Range(-rotationJitter, rotationJitter), 
                baseRotation.z
            );
            
            GameObject card = Instantiate(cardPrefab, transform.position + offset, transform.rotation * randomRot, transform);
            
            MonoBehaviour[] scripts = card.GetComponents<MonoBehaviour>();
            foreach (var s in scripts) s.enabled = false;

            spawnedCards.Add(card);
        }
    }
}
