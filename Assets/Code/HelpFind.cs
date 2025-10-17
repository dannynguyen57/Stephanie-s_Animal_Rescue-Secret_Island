using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimalFinder : MonoBehaviour
{
    [Tooltip("Spotlight prefab (must have a Light component)")]
    public GameObject spotlightPrefab;

    [Tooltip("UI Button to trigger the highlight")]
    public Button helpFindButton;

    [Tooltip("All animal Transforms go here")]
    public Transform[] animals;

    // Working list of animals not yet highlighted
    private List<Transform> remainingAnimals;
    private GameObject activeSpotlight;

    void Awake()
    {
        // Sanity checks
        if (spotlightPrefab == null) Debug.LogError("Assign spotlightPrefab!", this);
        if (helpFindButton == null) Debug.LogError("Assign helpFindButton!", this);
        if (animals == null || animals.Length == 0)
            Debug.LogError("Populate the animals array!", this);
    }

    void Start()
    {
        ResetRemainingList();
        helpFindButton.onClick.AddListener(OnHelpFindClicked);
    }

    private void ResetRemainingList()
    {
        // Start (or restart) with all animals available
        remainingAnimals = new List<Transform>(animals);
        Debug.Log("AnimalFinder: reset list of animals.");
    }

    void OnHelpFindClicked()
    {
        // If we’ve run out, reset so we can go again
        if (remainingAnimals.Count == 0)
        {
            Debug.Log("All animals highlighted—resetting list.");
            ResetRemainingList();
        }

        // Choose and remove
        int idx = Random.Range(0, remainingAnimals.Count);
        Transform chosen = remainingAnimals[idx];
        remainingAnimals.RemoveAt(idx);

        Debug.Log($"Highlighting: {chosen.name}");

        // Remove old spotlight
        if (activeSpotlight != null)
            Destroy(activeSpotlight);

        // Spawn new spotlight above the chosen animal
        activeSpotlight = Instantiate(
            spotlightPrefab,
            chosen.position + Vector3.up * 2f,
            Quaternion.identity,
            chosen   // parent so it follows the animal
        );

        // Configure the Light component
        Light lightComp = activeSpotlight.GetComponent<Light>();
        if (lightComp == null)
            Debug.LogError("Your prefab needs a Light component!", activeSpotlight);
        else
        {
            lightComp.enabled = true;
            lightComp.type = LightType.Spot;
            lightComp.spotAngle = 45f;
            lightComp.range = 10f;
            lightComp.intensity = 2f;
        }

        // Aim the spotlight at the animal
        Vector3 direction = (chosen.position - activeSpotlight.transform.position).normalized;
        activeSpotlight.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
}
