// BallMagnetism.cs

using UnityEngine;

public class BallMagnetism : MonoBehaviour
{
    // COMMENTIAMO O RIMUOVIAMO QUESTI, SARANNO PRESI DAL DIFFICULTY MANAGER
    // [Tooltip("Costante proporzionale per la correzione PD")]
    // public float correctionKp = 10f;

    // [Tooltip("Costante derivativa per la correzione PD")]
    // public float correctionKd = 5f;

    private DifficultyManager difficultyManager;
    private bool magnetismActive = false;
    private Transform magnetTarget; // Centro della piattaforma target
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        difficultyManager = Object.FindFirstObjectByType<DifficultyManager>();
        if (difficultyManager == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("BallMagnetism: DifficultyManager non trovato! Il magnetismo potrebbe non funzionare come previsto con la progressione della difficoltà per Kp e Kd.", this);
#endif
        }
    }

    void FixedUpdate()
    {
        if (magnetismActive && magnetTarget != null && rb != null)
        {
            float currentKp;
            float currentKd;
            float effectiveAttractionRadius;
            float effectiveMagneticForceMultiplier;

            if (difficultyManager != null)
            {
                float normalizedDifficulty = difficultyManager.GetNormalizedDifficulty();

                effectiveAttractionRadius = Mathf.Lerp(
                    difficultyManager.initialAttractionRadius,
                    difficultyManager.initialAttractionRadius * difficultyManager.minAttractionRadiusMultiplier,
                    normalizedDifficulty
                );
                effectiveMagneticForceMultiplier = Mathf.Lerp(
                    difficultyManager.initialMagneticForceMultiplier,
                    difficultyManager.initialMagneticForceMultiplier * difficultyManager.minMagneticForceMultiplier,
                    normalizedDifficulty
                );

                currentKp = Mathf.Lerp(
                    difficultyManager.initialCorrectionKp,
                    difficultyManager.ultimateCorrectionKp,
                    normalizedDifficulty
                );
                currentKd = Mathf.Lerp(
                    difficultyManager.initialCorrectionKd,
                    difficultyManager.ultimateCorrectionKd,
                    normalizedDifficulty
                );
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("BallMagnetism: DifficultyManager non trovato in FixedUpdate. Usando valori di Kp/Kd di fallback (10, 5).");
#endif
                effectiveAttractionRadius = 50f; 
                effectiveMagneticForceMultiplier = 1f; 
                currentKp = 10f; 
                currentKd = 5f;  
            }

            Vector3 error = new Vector3(
                magnetTarget.position.x - transform.position.x,
                0f, 
                magnetTarget.position.z - transform.position.z
            );
            float distance = error.magnitude;

            if (distance <= effectiveAttractionRadius && distance > 0.01f) 
            {
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                Vector3 correctionForce = effectiveMagneticForceMultiplier * ((currentKp * error) - (currentKd * horizontalVelocity));
                rb.AddForce(correctionForce, ForceMode.Acceleration);
            }
        }
    }

    /// <summary>
    /// Attiva il magnetismo indirizzando la palla verso il centro del target specificato.
    /// </summary>
    public void ActivateMagnetism(Transform target)
    {
        magnetTarget = target;
        magnetismActive = true;
    }

    /// <summary>
    /// Disattiva il magnetismo, tipicamente all'atterraggio.
    /// </summary>
    public void DeactivateMagnetism()
    {
        magnetismActive = false;
        magnetTarget = null; 
    }

    /// <summary>
    /// Quando la palla collide con una piattaforma valida, il magnetismo si disattiva.
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("Planet") || collision.gameObject.CompareTag("InitialPlatform"))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                // Nessuna azione qui ora
            }
            DeactivateMagnetism();
        }
    }
}