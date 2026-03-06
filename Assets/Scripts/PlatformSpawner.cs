using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necessario per LINQ (Where, Concat, Distinct, ToList)

public enum SpawnOrientation { Forward, Left, Right }
public class PlatformSpawner : MonoBehaviour
{
    #region Public Inspector Variables

    [Header("Elenco Prefab Piattaforme 'Planet'")]
    [Tooltip("Lista dei prefab con tag 'Planet'. Assegnali nell'Inspector.")]
    public List<GameObject> planetPrefabs = new List<GameObject>();

    [Header("Elenco Prefab Piattaforme 'Platform'")]
    [Tooltip("Lista dei prefab con tag 'Platform'. Assegnali nell'Inspector.")]
    public List<GameObject> platformPrefabsList = new List<GameObject>();

    [Header("Parametri di spawn")]
    public float minDistance = 10f;
    public float maxDistanceForward = 20f;
    public float maxDistanceLateral = 15f;

    [Header("Parametri Aggiuntivi")]
    public float targetTopY = 0f;
    public int maxConsecutiveForwardPlatforms = 3;
    public int platformsLimit = 5;

    [Header("Random Size Variation")]
    public float scaleVariation = 0f;

    [Header("Random Rotation")]
    public bool randomYRotationEnabled = true;

    [Header("Spawn Logic")]
    public int planetPlatformsBeforeForcedPlatform = 9;

    #endregion

    #region Private Constants

    private const string PLANET_TAG = "Planet";
    private const string PLATFORM_TAG = "Platform";

    #endregion

    #region Private Runtime Variables

    private GameObject GetInitialPlatformReference()
    {
        if (spawnedPlatforms != null && spawnedPlatforms.Count > 0)
        {
            return spawnedPlatforms[0];
        }
        return null;
    }

    private DifficultyManager difficultyManager;
    private SkyboxController skyboxController;

    private GameObject currentPlatform;
    private Vector3 currentDirection = Vector3.forward;
    private bool firstNonForwardPlatformHasBeenSpawned = false;
    private SpawnOrientation? lastLateral = null;
    private bool isFirstJump = true;
    private int forwardStreakCount = 0;
    private int consecutivePlanetCount = 0;

    private List<GameObject> availablePlanetPrefabsForCycle = new List<GameObject>();
    private List<GameObject> availablePlatformPrefabsForCycle = new List<GameObject>();
    private System.Random rng = new System.Random();

    public List<GameObject> spawnedPlatforms = new List<GameObject>();

    private Dictionary<GameObject, float> prefabBaseTopOffsets = new Dictionary<GameObject, float>();

    private GameObject lastSpawnedPlatformPrefab = null;
    private GameObject lastSpawnedPlanetPrefab = null;
    #endregion

    #region Unity Lifecycle Methods

    void Awake()
    {
        difficultyManager = FindFirstObjectByType<DifficultyManager>();
        if (difficultyManager == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("DifficultyManager non trovato.");
#endif
        }

        skyboxController = FindAnyObjectByType<SkyboxController>();
        if (skyboxController == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("SkyboxController non trovato.");
#endif
        }

        CachePrefabTopOffsets();
        firstNonForwardPlatformHasBeenSpawned = false;

        availablePlanetPrefabsForCycle.Clear();
        availablePlatformPrefabsForCycle.Clear();
    }

    #endregion

    #region Public Methods

    public void SetInitialPlatform(GameObject platform)
    {
        if (platform == null) { return; }
        currentPlatform = platform;
        if (!spawnedPlatforms.Contains(platform)) { spawnedPlatforms.Add(platform); }

        consecutivePlanetCount = 0;
        isFirstJump = true;
        forwardStreakCount = 0;
        currentDirection = Vector3.forward;
        lastLateral = null;
        firstNonForwardPlatformHasBeenSpawned = false;

        availablePlanetPrefabsForCycle.Clear();
        availablePlatformPrefabsForCycle.Clear();
    }

    public void SpawnNextPlatform()
    {
        if (currentPlatform == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("SpawnNextPlatform: currentPlatform è nullo, esco.");
#endif
            return;
        }

        List<GameObject> allValidPrefabs = GetAllValidPrefabs();
        if (allValidPrefabs.Count == 0)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("SpawnNextPlatform: Nessun prefab valido trovato, esco.");
#endif
            return;
        }

        GameObject selectedPrefab = SelectPrefabUsingShuffleBag(allValidPrefabs);

        if (selectedPrefab == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("SpawnNextPlatform: Selezione prefab fallita, esco.");
#endif
            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        float scaleMultiplier;
        SpawnOrientation chosenOrientation;

        CalculateNextTransform(selectedPrefab, out chosenOrientation, out spawnPosition, out spawnRotation, out scaleMultiplier);
        InstantiateAndFinalize(selectedPrefab, chosenOrientation, spawnPosition, spawnRotation, scaleMultiplier);
        CleanupOldPlatforms();
        UpdateManagers();
    }

    public GameObject GetNextPlatform()
    {
        return currentPlatform;
    }

    #endregion

    #region Private Helper Methods: Initialization

    void CachePrefabTopOffsets()
    {
        prefabBaseTopOffsets.Clear();
        var allUniquePrefabs = planetPrefabs.Concat(platformPrefabsList)
                                           .Where(p => p != null)
                                           .Distinct();
        foreach (GameObject prefab in allUniquePrefabs)
        {
            if (!prefabBaseTopOffsets.ContainsKey(prefab))
            {
                GameObject tempInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                tempInstance.hideFlags = HideFlags.HideAndDontSave;
                tempInstance.SetActive(false);
                Collider prefabCollider = tempInstance.GetComponentInChildren<Collider>();
                if (prefabCollider != null)
                {
                    float topOffset = prefabCollider.bounds.max.y;
                    prefabBaseTopOffsets[prefab] = topOffset;
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"Prefab '{prefab.name}' non ha Collider valido.");
#endif
                }
                Destroy(tempInstance);
            }
        }
    }

    #endregion

    #region Private Helper Methods: Spawning Logic

    private List<GameObject> GetAllValidPrefabs()
    {
        List<GameObject> validPrefabs = new List<GameObject>();
        var allSourcePrefabs = planetPrefabs.Concat(platformPrefabsList).Where(p => p != null).Distinct();
        foreach (GameObject prefab in allSourcePrefabs)
        {
            if (prefabBaseTopOffsets.ContainsKey(prefab)) { validPrefabs.Add(prefab); }
        }
        return validPrefabs;
    }

    private GameObject SelectPrefabUsingShuffleBag(List<GameObject> allValidPrefabs)
    {
        List<GameObject> currentValidPlanets = allValidPrefabs.Where(p => p.CompareTag(PLANET_TAG)).ToList();
        List<GameObject> currentValidPlatforms = allValidPrefabs.Where(p => p.CompareTag(PLATFORM_TAG)).ToList();

        GameObject selectedPrefab = null;

        bool forcePlatform = planetPlatformsBeforeForcedPlatform > 0 && consecutivePlanetCount >= planetPlatformsBeforeForcedPlatform;

        if (forcePlatform)
        {
            if (availablePlatformPrefabsForCycle.Count == 0)
            {
                if (currentValidPlatforms.Count > 0)
                {
                    availablePlatformPrefabsForCycle.AddRange(currentValidPlatforms);
                    ShuffleList(availablePlatformPrefabsForCycle);
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("Forza Platform richiesta, ma non ci sono Platform valide!");
#endif
                    forcePlatform = false;
                }
            }

            if (forcePlatform && availablePlatformPrefabsForCycle.Count > 0)
            {
                int indexToPick = availablePlatformPrefabsForCycle.Count - 1;
                selectedPrefab = availablePlatformPrefabsForCycle[indexToPick];
                availablePlatformPrefabsForCycle.RemoveAt(indexToPick);
            }
        }

        if (selectedPrefab == null)
        {
            if (availablePlanetPrefabsForCycle.Count == 0)
            {
                if (currentValidPlanets.Count > 0)
                {
                    availablePlanetPrefabsForCycle.AddRange(currentValidPlanets);
                    ShuffleList(availablePlanetPrefabsForCycle);
                }
            }

            if (availablePlanetPrefabsForCycle.Count > 0)
{
    // Evita planet duplicati consecutivi
    for (int i = availablePlanetPrefabsForCycle.Count - 1; i >= 0; i--)
    {
        if (availablePlanetPrefabsForCycle[i] != lastSpawnedPlanetPrefab)
        {
            selectedPrefab = availablePlanetPrefabsForCycle[i];
            availablePlanetPrefabsForCycle.RemoveAt(i);
            break;
        }
    }

    if (selectedPrefab == null)
    {
        int fallbackIndex = availablePlanetPrefabsForCycle.Count - 1;
        selectedPrefab = availablePlanetPrefabsForCycle[fallbackIndex];
        availablePlanetPrefabsForCycle.RemoveAt(fallbackIndex);
    }
}
            else
            {
                if (availablePlatformPrefabsForCycle.Count == 0)
                {
                    if (currentValidPlatforms.Count > 0)
                    {
                        availablePlatformPrefabsForCycle.AddRange(currentValidPlatforms);
                        ShuffleList(availablePlatformPrefabsForCycle);
                    }
                }

                if (availablePlatformPrefabsForCycle.Count > 0)
                {
                    // Prova a evitare duplicati consecutivi
                    for (int i = availablePlatformPrefabsForCycle.Count - 1; i >= 0; i--)
                    {
                        if (availablePlatformPrefabsForCycle[i] != lastSpawnedPlatformPrefab)
                        {
                            selectedPrefab = availablePlatformPrefabsForCycle[i];
                            availablePlatformPrefabsForCycle.RemoveAt(i);
                            break;
                        }
                    }

                    // Se non è riuscito (es. solo uno rimasto), prendilo comunque
                    if (selectedPrefab == null)
                    {
                        int fallbackIndex = availablePlatformPrefabsForCycle.Count - 1;
                        selectedPrefab = availablePlatformPrefabsForCycle[fallbackIndex];
                        availablePlatformPrefabsForCycle.RemoveAt(fallbackIndex);
                    }
                }

            }
        }

        if (selectedPrefab == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("SelectPrefabUsingShuffleBag NON è riuscito a selezionare un prefab!");
#endif
            if (allValidPrefabs.Count > 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("Seleziono il primo prefab valido trovato come fallback estremo.");
#endif
                selectedPrefab = allValidPrefabs[0];
            }
        }
        return selectedPrefab;
    }

    private void CalculateNextTransform(GameObject prefabToSpawn, out SpawnOrientation orientation, out Vector3 position, out Quaternion rotation, out float scaleMultiplier)
    {
        orientation = DetermineSpawnOrientation();
        Vector3 spawnDirection = CalculateSpawnDirection(orientation);
        float distance = CalculateSpawnDistance(orientation);
        float currentScaleVariation = CalculateCurrentScaleVariation();
        scaleMultiplier = (currentScaleVariation > 0) ? UnityEngine.Random.Range(1f - currentScaleVariation, 1f + currentScaleVariation) : 1f;
        position = CalculateSpawnPosition(prefabToSpawn, spawnDirection, distance, scaleMultiplier);
        rotation = CalculateSpawnRotation(prefabToSpawn);
    }

    private void InstantiateAndFinalize(GameObject prefab, SpawnOrientation orientation, Vector3 position, Quaternion rotation, float scaleMultiplier)
    {
        GameObject newPlatform = Instantiate(prefab, position, rotation);
        if (newPlatform == null) { return; }

        Transform platformThatSpawnedIt = this.currentPlatform?.transform;

        OrientTowardsPreviousPlatform orienter = newPlatform.GetComponent<OrientTowardsPreviousPlatform>();
        if (orienter != null)
        {
            orienter.InitializeAndOrient(platformThatSpawnedIt);
        }

        spawnedPlatforms.Add(newPlatform);
        currentPlatform = newPlatform;

        if (scaleMultiplier != 1f)
        {
            newPlatform.transform.localScale *= scaleMultiplier;
        }

        UpdateDirectionalState(orientation, currentPlatform.transform.forward);

        if (newPlatform.CompareTag(PLANET_TAG)) { consecutivePlanetCount++; }
        else { consecutivePlanetCount = 0; }

        PlatformEntryAnimation entryAnim = newPlatform.GetComponent<PlatformEntryAnimation>();
        if (entryAnim != null) { entryAnim.Initialize(position); }
    }

    #endregion

    #region Private Helper Methods: Transform Calculations

    private SpawnOrientation DetermineSpawnOrientation()
    {
        if (isFirstJump) { isFirstJump = false; return SpawnOrientation.Forward; }
        SpawnOrientation proposedOrientation;
        if (forwardStreakCount >= maxConsecutiveForwardPlatforms)
        {
            proposedOrientation = (lastLateral == SpawnOrientation.Left) ? SpawnOrientation.Right : SpawnOrientation.Left;
        }
        else if (lastLateral.HasValue)
        {
            proposedOrientation = (UnityEngine.Random.value > 0.5f) ? SpawnOrientation.Forward : ((lastLateral == SpawnOrientation.Left) ? SpawnOrientation.Right : SpawnOrientation.Left);
        }
        else
        {
            float rnd = UnityEngine.Random.value;
            if (rnd < 0.33f) { proposedOrientation = SpawnOrientation.Forward; }
            else if (rnd < 0.66f) { proposedOrientation = SpawnOrientation.Left; }
            else { proposedOrientation = SpawnOrientation.Right; }
        }
        if (proposedOrientation != SpawnOrientation.Forward)
        {
            if (!this.firstNonForwardPlatformHasBeenSpawned)
            {
                this.firstNonForwardPlatformHasBeenSpawned = true;
                return SpawnOrientation.Right;
            }
        }
        return proposedOrientation;
    }

    private Vector3 CalculateSpawnDirection(SpawnOrientation orientation)
    {
        switch (orientation)
        {
            case SpawnOrientation.Forward: return currentDirection.normalized;
            case SpawnOrientation.Left: return Quaternion.AngleAxis(-90, Vector3.up) * currentDirection.normalized;
            case SpawnOrientation.Right: return Quaternion.AngleAxis(90, Vector3.up) * currentDirection.normalized;
            default: return currentDirection.normalized;
        }
    }

    private float CalculateSpawnDistance(SpawnOrientation orientation)
    {
        float baseMin = minDistance;
        float baseMax = (orientation == SpawnOrientation.Forward) ? maxDistanceForward : maxDistanceLateral;

        float ultimateMin = baseMin;
        float ultimateMax = baseMax;
        if (difficultyManager != null)
        {
            ultimateMin = difficultyManager.ultimateMinDistance;
            ultimateMax = (orientation == SpawnOrientation.Forward) ? difficultyManager.ultimateMaxDistanceForward : difficultyManager.ultimateMaxDistanceLateral;
        }
        float t = (difficultyManager != null) ? difficultyManager.GetNormalizedDifficulty() : 0f;
        float currentMin = Mathf.Lerp(baseMin, ultimateMin, t);
        float currentMax = Mathf.Lerp(baseMax, ultimateMax, t);
        currentMin = Mathf.Min(currentMin, currentMax - 0.1f);
        return UnityEngine.Random.Range(currentMin, currentMax);
    }

    private float CalculateCurrentScaleVariation()
    {
        float baseVariation = scaleVariation;
        float ultimateVariation = baseVariation;
        if (difficultyManager != null)
        {
            ultimateVariation = difficultyManager.minScaleVariation;
        }
        float t = (difficultyManager != null) ? difficultyManager.GetNormalizedDifficulty() : 0f;
        return Mathf.Lerp(baseVariation, ultimateVariation, t);
    }

    private Vector3 CalculateSpawnPosition(GameObject prefabToSpawn, Vector3 spawnDirection, float distance, float scaleMultiplier)
    {
        Vector3 basePosition = currentPlatform.transform.position + spawnDirection * distance;
        if (prefabBaseTopOffsets.TryGetValue(prefabToSpawn, out float baseTopOffset))
        {
            float effectiveTopOffset = baseTopOffset * scaleMultiplier;
            basePosition.y = targetTopY - effectiveTopOffset;
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError($"Offset non trovato per {prefabToSpawn.name}! Posiziono a Y={targetTopY}.");
#endif
            basePosition.y = targetTopY;
        }
        return basePosition;
    }

    private Quaternion CalculateSpawnRotation(GameObject prefabToSpawn)
    {
        Quaternion baseRotation = prefabToSpawn.transform.localRotation;
        Quaternion randomYRotation = Quaternion.identity;
        if (randomYRotationEnabled)
        {
            randomYRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        }
        return randomYRotation * baseRotation;
    }

    #endregion

    #region Private Helper Methods: State Update & Cleanup

    private void UpdateDirectionalState(SpawnOrientation orientation, Vector3 newForward)
    {
        currentDirection = CalculateSpawnDirection(orientation);
        if (orientation == SpawnOrientation.Forward)
        {
            forwardStreakCount++;
        }
        else
        {
            forwardStreakCount = 0;
            lastLateral = orientation;
        }
    }

    private void CleanupOldPlatforms()
    {
        while (spawnedPlatforms.Count > platformsLimit)
        {
            int removeIndex = 0;
            GameObject initialPlatformRef = GetInitialPlatformReference();

            if (spawnedPlatforms.Count > 0 && initialPlatformRef != null && spawnedPlatforms[0] == initialPlatformRef)
            {
                if (spawnedPlatforms.Count > 1)
                {
                    removeIndex = 1;
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("Limite piattaforme raggiunto, ma esiste solo la piattaforma iniziale. Non verrà rimossa.");
#endif
                    break;
                }
            }

            if (removeIndex < spawnedPlatforms.Count)
            {
                GameObject oldPlatform = spawnedPlatforms[removeIndex];
                spawnedPlatforms.RemoveAt(removeIndex);

                if (oldPlatform != null)
                {
                    PlatformExitAnimation exitAnim = oldPlatform.GetComponent<PlatformExitAnimation>();
                    if (exitAnim != null)
                    {
                        exitAnim.AnimateExit();
                    }
                    else
                    {
                        Destroy(oldPlatform);
                    }
                }
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"Cleanup loop - Indice {removeIndex} non valido per lista di size {spawnedPlatforms.Count}. Interruzione.");
#endif
                break;
            }
        }
    }

    private void UpdateManagers()
    {
        if (difficultyManager != null)
        {
            difficultyManager.IncrementPlatformCount();
        }
        if (skyboxController != null)
        {
            skyboxController.AddPlatform();
        }
    }

    #endregion

    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}