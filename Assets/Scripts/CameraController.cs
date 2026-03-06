using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necessario se usi .ToList() o .Where() (anche se nel codice fornito non sono usati esplicitamente nel loop)

public class CameraController : MonoBehaviour
{
    [Tooltip("Transform della pallina (assegnato tramite Inspector)")]
    public Transform ballTransform;

    [Tooltip("Riferimento allo spawner delle piattaforme")]
    public PlatformSpawner platformSpawner;

    [Tooltip("Velocità di transizione della telecamera")]
    public float smoothSpeed = 2.0f;

    // Rimuoviamo verticalOffset, non è più usato per l'altezza fissa
    // [Tooltip("Offset verticale della telecamera rispetto al target calcolato")]
    // public float verticalOffset = 5.0f;

    [Tooltip("Quota Y desiderata per la telecamera")]
    public float cameraFixedY = 7.0f; // NUOVO parametro pubblico per impostare l'altezza fissa (settalo a 7.0f nell'Inspector)


    [Header("Look-Ahead Settings")]
    [Tooltip("Bias per il look-ahead verso la prossima piattaforma. 0.5 = centro esatto, > 0.5 = bias verso la prossima piattaforma")]
    [Range(0.0f, 1.0f)] // Limita il valore nell'Inspector tra 0 e 1
    public float lookAheadBias = 0.6f; // Valore di default leggermente spostato in avanti


    private Vector3 initialOffset; // Offset iniziale telecamera - pallina (questo cattura l'angolo e la distanza XZ iniziale)

    void Start()
    {
        if (ballTransform == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("Ball Transform non assegnato alla CameraController.");
#endif
            enabled = false; // Disabilita lo script se manca la pallina
            return;
        }

        // Cerchiamo lo spawner in Start() se non assegnato (più robusto)
        if (platformSpawner == null)
        {
             platformSpawner = FindFirstObjectByType<PlatformSpawner>();
             if (platformSpawner == null)
             {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                 Debug.LogWarning("Platform Spawner non assegnato e non trovato nella scena. La telecamera seguirà solo la pallina orizzontalmente alla quota fissa.");
#endif
             }
        }

        // Calcola l'offset iniziale 3D. Useremo le componenti XZ di questo offset
        // per mantenere la distanza e l'angolo orizzontale relativo INIZIALE.
        initialOffset = transform.position - ballTransform.position;
    }

    void LateUpdate()
    {
        if (ballTransform == null) return; // Assicurati che la pallina esista ancora

        GameObject nearestPlatform = null;
        GameObject nextPlatform = null;

        // Troviamo le piattaforme rilevanti solo se lo spawner è disponibile e ha piattaforme
        // Aggiunto controllo null anche su spawnedPlatforms per sicurezza
        if (platformSpawner != null && platformSpawner.spawnedPlatforms != null && platformSpawner.spawnedPlatforms.Count > 0)
        {
            // Trova la piattaforma più vicina orizzontalmente alla pallina
            nearestPlatform = FindCurrentPlatform();
            // Ottiene l'ultima piattaforma generata dallo spawner (la 'prossima' destinazione)
            nextPlatform = platformSpawner.GetNextPlatform();
        }

/*
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (nearestPlatform != null && nextPlatform != null)
        {

        }
        else if (nearestPlatform == null)
        {

        }
        else if (nextPlatform == null)
        {

        }
#endif
*/
        Vector3 horizontalTargetPoint; // Il punto sul piano XZ che la telecamera seguirà

        // Determina il punto target orizzontale in base al look-ahead o alla posizione della pallina
        // Applica la logica di look-ahead solo se abbiamo un punto di partenza (nearestPlatform)
        // e un punto di arrivo (nextPlatform) validi.
        if (nearestPlatform != null && nextPlatform != null)
        {
            // Calcola un punto interpolato tra la piattaforma più vicina e la prossima nel piano XZ.
            // Ignoriamo le differenze di altezza tra le piattaforme per il calcolo del punto target orizzontale.
            Vector3 nearestPosXZ = new Vector3(nearestPlatform.transform.position.x, 0, nearestPlatform.transform.position.z);
            Vector3 nextPosXZ = new Vector3(nextPlatform.transform.position.x, 0, nextPlatform.transform.position.z);

            horizontalTargetPoint = Vector3.Lerp(nearestPosXZ, nextPosXZ, lookAheadBias);

             // Debug visivo (opzionale)
             // Debug.DrawLine(nearestPosXZ + Vector3.up, nextPosXZ + Vector3.up, Color.yellow); // Segmento look-ahead orizzontale
             // Debug.DrawLine(horizontalTargetPoint + Vector3.up * 2, horizontalTargetPoint + Vector3.up * 3, Color.red); // Punto look-ahead orizzontale
        }
        else
        {
            // Fallback: se non ci sono abbastanza piattaforme (es. all'inizio), segui semplicemente la posizione orizzontale della pallina
            horizontalTargetPoint = new Vector3(ballTransform.position.x, 0, ballTransform.position.z);
        }

        // Calcola la posizione desiderata finale della telecamera.
        // Le componenti XZ sono basate sull'horizontalTargetPoint più l'offset XZ iniziale.
        // La componente Y è FISSATA al valore `cameraFixedY`.
        Vector3 desiredPosition = new Vector3(
            horizontalTargetPoint.x + initialOffset.x, // X del target + offset X iniziale
            cameraFixedY,                             // Y FISSATA al valore desiderato
            horizontalTargetPoint.z + initialOffset.z // Z del target + offset Z iniziale
        );

        // Applica la posizione desiderata con interpolazione fluida
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Opzionale: Mantenere la rotazione originale della telecamera se non è impostata altrove
        // (Se non vuoi che la telecamera ruoti per 'guardare' il target orizzontale)
        // transform.rotation = initialRotation; // Se memorizzi initialRotation in Start
    }

    // Trova la piattaforma più vicina orizzontalmente alla pallina tra quelle spawnate
    // NOTA: Con platformsLimit basso, questa ricerca è accettabile per performance.
    // Se platformsLimit fosse alto, servirebbe un metodo più efficiente (es. basato su griglia).
    GameObject FindCurrentPlatform()
    {
        GameObject nearestPlatform = null;
        // Inizializza la distanza minima a un valore molto alto
        // Usiamo distanza al quadrato per performance (evita sqrt)
        float minDistanceSqr = Mathf.Infinity;

        // Ottieni la posizione XZ della pallina, ignorando la sua Y per il confronto orizzontale
        Vector3 ballPositionXZ = new Vector3(ballTransform.position.x, 0, ballTransform.position.z);

        // Itera sulla lista delle piattaforme spawnate
        if (platformSpawner != null && platformSpawner.spawnedPlatforms != null)
        {
            foreach (GameObject platform in platformSpawner.spawnedPlatforms)
            {
                if (platform != null) // Aggiungi un controllo null per sicurezza
                {
                    // Ottieni la posizione XZ della piattaforma, ignorando la sua Y
                    Vector3 platformPosXZ = new Vector3(platform.transform.position.x, 0, platform.transform.position.z);
                    // Calcola la distanza al quadrato solo sul piano XZ
                    float distanceSqr = (ballPositionXZ - platformPosXZ).sqrMagnitude;

                    if (distanceSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distanceSqr;
                        nearestPlatform = platform;
                    }
                }
            }
        }

/*
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (nearestPlatform != null)
        {

        }
        else
        {

        }
#endif
*/
        // Puoi aggiungere qui una soglia di distanza massima per considerare solo piattaforme vicine
        // Es: public float maxRelevantDistance = 10f; if (minDistanceSqr > maxRelevantDistance * maxRelevantDistance) return null;

        return nearestPlatform;
    }

    // Metodo helper per trovare FindFirstObjectByType in modo più robusto (opzionale)
    // Non è usato nel codice sopra, ma era nel tuo snippet originale.
    // T FindComponent<T>() where T : Component { T obj = FindFirstObjectByType<T>(); if (obj == null) Debug.LogWarning($"'{typeof(T).Name}' non trovato nella scena."); return obj; }

}