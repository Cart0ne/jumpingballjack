using UnityEngine;
using System.Collections;

public class PlatformCompression : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assegna qui il Transform del GameObject figlio che contiene la mesh (es. Platform_Visuals)")]
    [SerializeField] private Transform visualTransform; // Riferimento al figlio con la mesh

    [Header("Scaling Settings")]
    public float maxCompressionFactor = 0.6f;
    public float compressionSpeed = 5f;

    [Header("Upward Jerk Settings")]
    [Tooltip("Quantità di spostamento verso l'alto della piattaforma quando la palla salta")]
    public float upwardJerkStrength = 0.1f;
    [Tooltip("Durata del rimbalzo verso l'alto della piattaforma")]
    public float bounceDuration = 0.2f; // Nuova variabile per la durata del rimbalzo
    [Tooltip("Ritardo (in secondi) prima di abilitare il rimbalzo dopo l'atterraggio")]
    public float bounceActivationDelay = 0.1f; // Nuovo ritardo
    [Tooltip("Ritardo (in secondi) tra la fine della decompressione e l'inizio del salto")]
    public float decompressionBounceDelay = 0.05f; // Nuovo ritardo per la decompressione

    private BallController ballController;
    private bool isCompressing = false;
    private bool isPlayerOnThisPlatform = false;
    private bool isBouncing = false; // Nuova variabile per controllare se la piattaforma sta rimbalzando
    private bool canBounce = false; // Nuova variabile per controllare se il rimbalzo può essere attivato
    private bool originalPositionSet = false; // Nuova variabile per controllare se la posizione originale è stata impostata
    private bool isDecompressing = false; // Nuova variabile per controllare se la piattaforma sta decomprimendo

    private Vector3 visualOriginalScale;
    private Vector3 visualCurrentScale;
    private float visualOriginalPosY;
    private Vector3 platformOriginalPosition; // Memorizza la posizione originale della piattaforma

    void Start()
    {
        if (visualTransform == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILDtrue
                Debug.LogError("PlatformCompression: 'visualTransform' non assegnato nell'Inspector!", this.gameObject);
#endif
            enabled = false;
            return;
        }

        visualOriginalScale = visualTransform.localScale;
        visualCurrentScale = visualOriginalScale;
        visualOriginalPosY = visualTransform.localPosition.y;

        ballController = FindFirstObjectByType<BallController>();
        if (ballController == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("PlatformCompression: BallController non trovato nella scena!");
 #endif
        }
    }

    void Update()
    {
        if (visualTransform == null) return;

        float targetVisualScaleY = visualOriginalScale.y;
        isDecompressing = false; // Resetta lo stato di decompressione all'inizio di ogni frame

        if (isPlayerOnThisPlatform && Input.GetMouseButton(0) && ballController != null)
        {
            isCompressing = true;
            float chargePercentage = Mathf.Clamp01(ballController.chargeTime / ballController.maxChargeTime);
            targetVisualScaleY = visualOriginalScale.y * Mathf.Lerp(1f, maxCompressionFactor, chargePercentage);
        }
        else
        {
            if (isCompressing)
            {
                isDecompressing = true; // Imposta lo stato di decompressione se era in compressione
            }
            isCompressing = false;
            targetVisualScaleY = visualOriginalScale.y;
        }

        visualCurrentScale.y = Mathf.Lerp(visualCurrentScale.y, targetVisualScaleY, Time.deltaTime * compressionSpeed);
        visualTransform.localScale = visualCurrentScale;

        float heightDifference = visualOriginalScale.y - visualCurrentScale.y;
        float newLocalPosY = visualOriginalPosY + heightDifference / 5f; // Mantiene la compressione dal basso

        visualTransform.localPosition = new Vector3(
            visualTransform.localPosition.x,
            newLocalPosY,
            visualTransform.localPosition.z
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOnThisPlatform = true;
            canBounce = false; // Disabilita temporaneamente il rimbalzo all'atterraggio
            StartCoroutine(EnableBounce()); // Avvia il timer per abilitare il rimbalzo
            if (!originalPositionSet)
            {
                platformOriginalPosition = transform.position;
                originalPositionSet = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOnThisPlatform = false;
            if (canBounce && !isBouncing)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
                StartCoroutine(DecompressionBounceDelay()); // Avvia il ritardo dopo la decompressione
            }
        }
    }

    private IEnumerator EnableBounce()
    {
        yield return new WaitForSeconds(bounceActivationDelay);
        canBounce = true; // Abilita il rimbalzo dopo un breve ritardo
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }

    private IEnumerator DecompressionBounceDelay()
    {
        // Attendi un breve periodo per assicurarsi che la piattaforma sia decompressa
        yield return new WaitUntil(() => !isDecompressing);
        yield return new WaitForSeconds(decompressionBounceDelay);
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        StartCoroutine(BounceUp());
    }

    private IEnumerator BounceUp()
    {
        isBouncing = true;
        float timeElapsed = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + Vector3.up * upwardJerkStrength;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        while (timeElapsed < bounceDuration / 2f)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / (bounceDuration / 2f));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        timeElapsed = 0f;
        while (timeElapsed < bounceDuration / 2f)
        {
            transform.position = Vector3.Lerp(targetPosition, platformOriginalPosition, timeElapsed / (bounceDuration / 2f));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = platformOriginalPosition; // Assicura che la posizione finale sia quella originale
        isBouncing = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }
}