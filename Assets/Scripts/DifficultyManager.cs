using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    // Incremento della difficoltà basato sul tempo (per secondo)
    public float difficultyIncreaseRate = 0.01f;
    // Incremento della difficoltà per ogni piattaforma generata
    public float platformIncreaseFactor = 0.05f;
    // Valore massimo di difficoltà da usare per normalizzare (0 - maxDifficulty -> 0..1)
    public float maxDifficulty = 1f;

    // Contributo cumulativo: quello derivante dal tempo e da quante piattaforme sono generate
    private float timeDifficulty = 0f;
    private float platformDifficulty = 0f;
    // Contatore delle piattaforme
    public int platformCount = 0;

    private float timeElapsedSinceStart = 0f;
    public bool firstJumpOccurred = false; // Nuova variabile

    // Opzionale: usa una curva per ottenere un incremento non lineare della difficoltà
    public bool useCurve = false;
    public AnimationCurve difficultyCurve;

    [Header("Platform Spawner Settings")]
    [Tooltip("Percentuale minima di variazione uniforme per la scala alla massima difficoltà (es. 0.1 = ±10%)")]
    public float minScaleVariation = 0.2f;
    [Tooltip("Distanza minima iniziale per lo spawn delle piattaforme")]
    public float initialMinDistance = 3f;

    [Tooltip("Distanza minima per lo spawn delle piattaforme alla massima difficoltà")]
    public float ultimateMinDistance = 5f; // Imposta qui un valore appropriato
    [Tooltip("Distanza massima iniziale per piattaforme Forward")]
    public float initialMaxDistanceForward = 6f;

    [Tooltip("Distanza massima iniziale per piattaforme Left/Right")]
    public float initialMaxDistanceLateral = 4f;

    [Tooltip("Distanza massima forward alla massima difficoltà")]
    public float ultimateMaxDistanceForward = 9f;

    [Tooltip("Distanza massima laterale alla massima difficoltà")]
    public float ultimateMaxDistanceLateral = 6f;

    [Header("Gravity Settings")]
    [Tooltip("Moltiplicatore per l'incremento della gravità in base alla difficoltà (es. 0.1 = incremento del 10%)")]
    public float gravityIncreaseMultiplier = 0.1f;
    [Tooltip("Valore iniziale del moltiplicatore di gravità sulla piattaforma (0 - 1)")]
    public float initialOnPlatformGravityScale = 0.1f;
    [Header("Magnetism Settings")]
    [Tooltip("Raggio di attrazione iniziale per il magnetismo")]
    public float initialAttractionRadius = 50f;

    [Tooltip("Moltiplicatore iniziale per la forza magnetica")]
    public float initialMagneticForceMultiplier = 1f;

    [Tooltip("Moltiplicatore minimo per il raggio di attrazione alla massima difficoltà (es. 0.2 riduce il raggio al 20%)")]
    public float minAttractionRadiusMultiplier = 0.2f;

    [Tooltip("Moltiplicatore minimo per la forza magnetica a massima difficoltà (es. 0.2 riduce la forza al 20%)")]
    public float minMagneticForceMultiplier = 0.2f;
    [Header("Magnetism PD Controller Settings")]
    [Tooltip("Valore iniziale per la costante proporzionale (Kp) del magnetismo")]
    public float initialCorrectionKp = 10f; // Valore di default simile a quello attuale in BallMagnetism

    [Tooltip("Valore per la costante proporzionale (Kp) del magnetismo alla massima difficoltà")]
    public float ultimateCorrectionKp = 5f;  // Esempio: Kp potrebbe diminuire con la difficoltà

    [Tooltip("Valore iniziale per la costante derivativa (Kd) del magnetismo")]
    public float initialCorrectionKd = 5f;  // Valore di default simile a quello attuale in BallMagnetism

    [Tooltip("Valore per la costante derivativa (Kd) del magnetismo alla massima difficoltà")]
    public float ultimateCorrectionKd = 2.5f; // Esempio: Kd potrebbe diminuire con la difficoltà


    [Header("Flight Speed Settings")]
    [Tooltip("Moltiplicatore velocita palla in volo a difficolta 0")]
    public float initialFlightSpeedMultiplier = 1f;
    [Tooltip("Moltiplicatore velocita palla in volo alla massima difficolta")]
    public float ultimateFlightSpeedMultiplier = 1.5f;

    [Header("Platform Vertical Movement Settings")]
    [Tooltip("Probabilità (da 0 a 1) che una piattaforma si muova verticalmente")]
    [Range(0f, 1f)]
    public float verticalMovementProbability = 0.5f;

    [Tooltip("Velocità minima del movimento verticale")]
    public float minVerticalMoveSpeed = 1f;
    [Tooltip("Velocità massima del movimento verticale")]
    public float maxVerticalMoveSpeed = 3f;

    [Tooltip("Distanza minima del movimento verticale")]
    public float minVerticalMoveDistance = 2f;
    [Tooltip("Distanza massima del movimento verticale")]
    public float maxVerticalMoveDistance = 5f;

    private float debugTimer = 0f; // Timer per il debug

    void Start()
    {
        timeElapsedSinceStart = 0f;
        firstJumpOccurred = false; // Assicurati che sia inizializzata a false all'inizio
    }

    void Update()
    {
        if (firstJumpOccurred) // Incrementa il timer solo se il primo salto è avvenuto
        {
            timeElapsedSinceStart += Time.deltaTime;
            timeDifficulty = difficultyIncreaseRate * timeElapsedSinceStart;

            debugTimer += Time.deltaTime;
            if (debugTimer >= 10f) // Ogni 10 secondi
            {
                debugTimer = 0f;

                float normalizedDifficulty = GetNormalizedDifficulty();
                float currentKp = Mathf.Lerp(initialCorrectionKp, ultimateCorrectionKp, normalizedDifficulty);
                float currentKd = Mathf.Lerp(initialCorrectionKd, ultimateCorrectionKd, normalizedDifficulty);

#if UNITY_EDITOR || DEVELOPMENT_BUILD


#endif
            }
        }
    }

    // Metodo da chiamare ogni volta che viene generata una nuova piattaforma
    public void IncrementPlatformCount()
    {
        platformCount++;
        platformDifficulty = platformCount * platformIncreaseFactor;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }

    // Restituisce la difficoltà combinata normalizzata (valore compreso tra 0 e 1)
    public float GetNormalizedDifficulty()
    {
        // Calcola la difficoltà combinata dal tempo e dal numero di piattaforme
        float combinedDifficulty = timeDifficulty + platformDifficulty;
        // Normalizza il valore secondo maxDifficulty
        float normalized = Mathf.Clamp01(combinedDifficulty / maxDifficulty);

        // Se usare la curva è abilitato, valuta il valore normalizzato sul grafico della curva
        if (useCurve && difficultyCurve != null)
        {
            normalized = difficultyCurve.Evaluate(normalized);
        }

        return normalized;
    }

    // Metodo per resettare la difficoltà ai valori iniziali
    public void ResetDifficulty()
    {
        timeDifficulty = 0f;
        platformDifficulty = 0f;
        platformCount = 0;
        timeElapsedSinceStart = 0f;
        firstJumpOccurred = false; // Resetta anche il flag del primo salto

#if UNITY_EDITOR || DEVELOPMENT_BUILD


#endif
    }
}