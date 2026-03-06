using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Per HashSet
using TMPro; // Per TextMeshPro (se usato nel prefab FloatingScore)

public class BallScoreTracker : MonoBehaviour
{
    private ScoreManager scoreManager;
    private HashSet<GameObject> scoredPlatforms = new HashSet<GameObject>(); // Tiene traccia delle piattaforme già contate per evitare punteggi multipli

    [Header("Perfect Center Settings")]
    [Tooltip("Distanza massima dal centro della piattaforma per contare il 'centro perfetto'.")]
    public float centerHitActivationDistance = 0.5f;

    // Stato interno per la logica del moltiplicatore Planet
    private int planetCenterStreak = 0;      // Conta i centri perfetti consecutivi sui pianeti
    private int accumulatedPerfectPoints = 0; // Accumula i punti base (SOLO dai centri Planet) per il moltiplicatore
    private bool multiplierHit = false;       // Flag per colorare l'effetto visivo (attivo solo per moltiplicatore Planet x3)

    [Header("Floating Score Settings")]
    [Tooltip("Prefab dell'oggetto di testo fluttuante per mostrare i punti guadagnati.")]
    public GameObject floatingScorePrefab;
    [Tooltip("Altezza sopra la piattaforma a cui appare il testo fluttuante.")]
    public float floatingScoreHeight = 2f;

    [Header("Center Hit Effect")]
    [Tooltip("Prefab dell'effetto da istanziare all'impatto al centro.")]
    public GameObject centerHitEffectPrefab;
    [Tooltip("Durata totale dell'effetto dell'anello.")]
    public float ringDuration = 1.5f;
    [Tooltip("Scala massima dell'anello in espansione.")]
    public float maxScale = 20f;

    [Header("Multiplier Color Settings")]
    [Tooltip("Colore dell'effetto visivo quando si attiva il moltiplicatore Planet x3.")]
    public Color multiplierEffectColor = new Color(1f, 0f, 0f, 1f); // Unito in un unico campo Color

    void Start()
    {
        scoreManager = Object.FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("BallScoreTracker: ScoreManager non trovato nella scena!", this);
#endif
        }
        planetCenterStreak = 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject collidedObject = collision.gameObject;

        if (collidedObject.CompareTag("InitialPlatform"))
        {
            // Aggiungi la piattaforma iniziale al set per evitare punteggio prima dell'inizio
            scoredPlatforms.Add(collidedObject);
            // Resetta la serie di centri perfetti e l'accumulatore se si torna all'inizio
            planetCenterStreak = 0;
            accumulatedPerfectPoints = 0;
            multiplierHit = false;
        }
        else if (collidedObject.CompareTag("Platform") || collidedObject.CompareTag("Planet"))
        {
            // Controlla se questa piattaforma è già stata contata in questo "salto"
            if (!scoredPlatforms.Contains(collidedObject))
            {
                // Calcola i punti (la logica interna aggiorna anche multiplierHit)
                int pointsToAdd = CalculatePoints(collidedObject);

                if (pointsToAdd > 0)
                {
                    if (scoreManager != null)
                    {
                        scoreManager.AddScore(pointsToAdd);
                    }

                    scoredPlatforms.Add(collidedObject); // Segna come contata

                    // Mostra il punteggio fluttuante
                    SpawnFloatingScore(pointsToAdd, collidedObject.transform.position);

                    // Mostra effetto visivo se è un colpo al centro
                    float distanceToCenter = GetHorizontalDistance(transform.position, collidedObject.transform.position);
                    if (distanceToCenter <= centerHitActivationDistance)
                    {
                        SpawnCenterHitEffect(transform.position, multiplierHit); // Passa il flag per la colorazione
                    }
                }
                // Se pointsToAdd è 0, non fare nulla (es. colpo non al centro su un tipo di piattaforma senza punteggio base)
            }
            // Se la piattaforma è già in scoredPlatforms, ignora la collisione per il punteggio
        }
    }

    // Calcola i punti per l'atterraggio su una piattaforma specifica
    private int CalculatePoints(GameObject platform)
    {
        float distanceToCenter = GetHorizontalDistance(transform.position, platform.transform.position);
        bool isCenterHit = distanceToCenter <= centerHitActivationDistance;
        int points = 0;
        bool isMultiplierHitForEffect = false; // Flag locale per questo calcolo

        this.multiplierHit = false; // Reset flag globale

        if (isCenterHit)
        {
            // L'incremento dello streak ora avviene SOLO se è un Planet
            if (platform.CompareTag("Platform"))
            {
                points = 10; // Punti fissi per centro Platform
                accumulatedPerfectPoints = 0; // Resetta accumulatore Planet
                // --- NUOVO: Resetta anche lo streak dei Planet ---
                planetCenterStreak = 0;
                // --- FINE NUOVO ---
            }
            else if (platform.CompareTag("Planet"))
            {
                // --- MODIFICA: Incrementa lo streak SOLO QUI ---
                planetCenterStreak++;
                // --- FINE MODIFICA ---
                accumulatedPerfectPoints += 2; // Aggiunge 2 punti all'accumulatore
                points = accumulatedPerfectPoints; // Punti base = accumulati

                // Applica moltiplicatore x3 se si raggiunge il 5° centro consecutivo SOLO SU PLANET
                if (planetCenterStreak >= 5) // Usa la variabile rinominata
                {
                    points *= 3;
                    isMultiplierHitForEffect = true;
                    accumulatedPerfectPoints = 0; // Resetta accumulatore DOPO moltiplicatore
                    planetCenterStreak = 0;      // Resetta streak DOPO moltiplicatore (usa variabile rinominata)
                }
            }
            // Altri tag: points = 0, nessun effetto su streak/accumulatore
        }
        else // Non è un colpo al centro
        {
            // --- MODIFICA: Resetta lo streak rinominato ---
            planetCenterStreak = 0;      // Resetta streak Planet
            // --- FINE MODIFICA ---
            accumulatedPerfectPoints = 0; // Resetta accumulatore
            isMultiplierHitForEffect = false; // Assicura flag resettato

            if (platform.CompareTag("Platform"))
            {
                points = 5; // Punti base per colpo normale Platform
            }
            else if (platform.CompareTag("Planet"))
            {
                points = 1; // Punti base per colpo normale Planet
            }
            // Altri tag: points = 0
        }

        this.multiplierHit = isMultiplierHitForEffect;
        return points;
    }
    // Calcola la distanza sul piano XZ
    private float GetHorizontalDistance(Vector3 posA, Vector3 posB)
    {
        return Vector2.Distance(new Vector2(posA.x, posA.z), new Vector2(posB.x, posB.z));
    }

    // Istanzia il testo fluttuante
    private void SpawnFloatingScore(int score, Vector3 platformPosition)
    {
        if (floatingScorePrefab == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("Nessun prefab FloatingScore assegnato a BallScoreTracker!", this);
#endif
            return;
        }

        Vector3 spawnPosition = platformPosition + Vector3.up * floatingScoreHeight;
        GameObject floatingScoreInstance = Instantiate(floatingScorePrefab, spawnPosition, Quaternion.identity);
        FloatingScoreController scoreController = floatingScoreInstance.GetComponent<FloatingScoreController>();

        if (scoreController != null)
        {
            scoreController.Initialize(score, spawnPosition);
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("Il prefab FloatingScore non ha uno script FloatingScoreController!", floatingScoreInstance);
#endif
        }
    }

    // Istanzia e anima l'effetto visivo per il colpo al centro
    private void SpawnCenterHitEffect(Vector3 spawnPosition, bool isMultiplier)
    {
        if (centerHitEffectPrefab == null) return;

        // Istanzia l'effetto sulla posizione della palla
        GameObject effectInstance = Instantiate(centerHitEffectPrefab, transform.position, Quaternion.identity);
        Renderer effectRenderer = effectInstance.GetComponentInChildren<Renderer>(); // Cerca anche nei figli

        // Colora l'effetto se è un colpo moltiplicatore
        if (isMultiplier && effectRenderer != null)
        {
             // Applica direttamente il colore definito nell'inspector
            Color colorToApply = multiplierEffectColor;

            // Crea una copia del materiale per non modificare l'asset originale
            // (Potrebbe non essere necessario se si usa MaterialPropertyBlock, ma questo è più semplice)
            Material instanceMaterial = new Material(effectRenderer.material);
            instanceMaterial.color = colorToApply;
            effectRenderer.material = instanceMaterial; // Assegna il nuovo materiale all'istanza
        }
        else if (isMultiplier && effectRenderer == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
             Debug.LogWarning("Center Hit Effect prefab non ha un Renderer per applicare il colore del moltiplicatore.", effectInstance);
#endif
        }

        // Avvia l'animazione dell'anello
        StartCoroutine(AnimateRing(effectInstance.transform));
    }

    // Coroutine per animare l'effetto (scala + dissolvenza)
    private IEnumerator AnimateRing(Transform ringTransform)
    {
        float timer = 0f;
        Vector3 initialScale = Vector3.one * 0.1f;
        ringTransform.localScale = initialScale;

        // Cerca il renderer (potrebbe essere MeshRenderer o SpriteRenderer)
        Renderer ringRenderer = ringTransform.GetComponentInChildren<Renderer>();
        Material materialInstance = null; // Per MeshRenderer
        SpriteRenderer spriteInstance = null; // Per SpriteRenderer
        Color startColor = Color.white;
        float initialAlpha = 1f;

        if (ringRenderer != null) {
            if (ringRenderer is SpriteRenderer) {
                spriteInstance = (SpriteRenderer)ringRenderer;
                startColor = spriteInstance.color;
            } else {
                // Assumiamo sia un MeshRenderer o simile con un material
                 materialInstance = ringRenderer.material; // Ottiene l'istanza del materiale
                 startColor = materialInstance.color;
            }
            initialAlpha = startColor.a;
        }

        while (timer < ringDuration)
        {
            float progress = timer / ringDuration; // Progresso lineare da 0 a 1

            // Scala: da initialScale a maxScale
            float scaleFactor = Mathf.Lerp(initialScale.x, maxScale, progress);
            ringTransform.localScale = Vector3.one * scaleFactor;

            // Alpha: da initialAlpha a 0
            float alpha = Mathf.Lerp(initialAlpha, 0f, progress);
            startColor.a = alpha; // Modifica solo l'alpha del colore iniziale

            // Applica il colore aggiornato
            if (materialInstance != null) {
                materialInstance.color = startColor;
            } else if (spriteInstance != null) {
                spriteInstance.color = startColor;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Distruggi l'effetto alla fine
        Destroy(ringTransform.gameObject);
    }
}