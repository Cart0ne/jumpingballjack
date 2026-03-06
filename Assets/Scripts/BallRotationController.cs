using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BallController))]
public class BallRotationController : MonoBehaviour
{
    [Header("Rotazione")]
    [Tooltip("Velocità di rotazione in gradi al secondo durante il volo.")]
    public float rotationSpeed = 360f;

    [Tooltip("Riferimento al modello visuale da ruotare (l'oggetto figlio che rappresenta la mesh).")]
    public Transform visualModel;

    [Header("Allineamento Pre-Atterraggio (Discesa)")]
    [Tooltip("Soglia velocità verticale per considerare iniziata la discesa (valore negativo).")]
    public float descentVelocityThreshold = -0.1f;

    [Header("Orientamento All'Atterraggio")]
    [Tooltip("Velocità con cui il visuale si allinea durante l'atterraggio.")]
    public float landingAlignmentSpeed = 10f;

    [Tooltip("Direzione locale che deve puntare verso l'alto quando atterrato.")]
    public Vector3 upDirection = Vector3.up;

    [Header("Durate Allineamento")]
    [Tooltip("Durata in secondi dell'allineamento dopo l'inizio del rimbalzo.")]
    public float alignmentDuration = 0.5f;

    [Tooltip("Ritardo (secondi) dall'inizio del rimbalzo rilevato prima di iniziare l'allineamento post-rimbalzo.")]
    public float postBounceAlignmentDelay = 0.2f;

    // Riferimenti ai componenti e altri script
    private BallController ballController;
    private Rigidbody rb;
    private Transform targetPlatform;
    private StartGameActions startGameActions;

    // Stato interno
    //private bool isRotating = false;
    //private bool rotationEnabled = false;

    private bool isAligning = false;
    private float alignmentTimer = 0f;
    private float currentAlignmentDuration;
    private bool wasGrounded = false;
    private bool wasBouncing = false;
    private Vector3 directionToNextPlatform;
    private Coroutine alignmentDelayCoroutine = null;

    // Stato per l'allineamento pre-atterraggio
    private bool hasAlignedPreLanding = false;
    private bool wasDescending = false;
    private Quaternion preLandingTargetRotation;

    void Start()
    {
        ballController = GetComponent<BallController>();
        rb = GetComponent<Rigidbody>();
        startGameActions = FindFirstObjectByType<StartGameActions>();
        if (startGameActions == null)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("BallRotationController: StartGameActions script non trovato!", this);
    
#endif
        if (visualModel == null && ballController != null && ballController.ballVisual != null)
            visualModel = ballController.ballVisual;
        else if (visualModel == null)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("BallRotationController: Visual Model non assegnato!", this);
    
#endif
        currentAlignmentDuration = alignmentDuration;
        //rotationEnabled = false;

    }

    void Update()
    {
        if (startGameActions == null || !startGameActions.IsGameStartSequenceInitiated())
        {
            isAligning = false;
            return;
        }

        // Se rotationEnabled è false, non facciamo nulla di attivo.
        // Questo è il nostro interruttore principale per la rotazione di gioco.
        // if (!rotationEnabled) return;

        if (ballController == null || visualModel == null || rb == null) return;

        if (ballController.gameEnded)
        {
            isAligning = false;
            return;
        }

        targetPlatform = ballController.targetPlatform;
        UpdateDirectionToNextPlatform(); // Calcola la direzione verso la prossima piattaforma

        // Ottieni lo stato attuale della palla DIRETTAMENTE da BallController
        bool currentIsGrounded = ballController.IsGrounded();
        bool currentIsBouncing = ballController.isBouncing; // Usa il flag pubblico di BallController
        float verticalVelocity = rb.linearVelocity.y;

        // Gestione rotazione e allineamento pre-atterraggio
        bool isDescending = !currentIsGrounded && verticalVelocity < descentVelocityThreshold;

        if (!currentIsGrounded && !isAligning) // Se siamo in volo E non stiamo già forzando un allineamento
        {
            // Se non abbiamo ancora fatto l'allineamento pre-atterraggio per questa discesa specifica
            if (!hasAlignedPreLanding)
            {
                RotateDuringFlight(); // Fai ruotare la palla mentre vola verso la piattaforma

                // Se iniziamo a scendere (eravamo in salita o stabili, e ora scendiamo)
                // E NON siamo in fase di "bouncing" attivo (il saltino iniziale)
                // allora prepariamo l'allineamento pre-atterraggio.
                // Durante il primo "bouncing", vogliamo che RotateDuringFlight faccia il suo corso.
                if (isDescending && !wasDescending && !currentIsBouncing)
                {
                    AlignPreLanding(); // Memorizza la rotazione target per l'atterraggio
                    hasAlignedPreLanding = true;
                }
            }
            // Se abbiamo già fatto l'allineamento pre-atterraggio, manteniamo quella rotazione.
            else if (hasAlignedPreLanding)
            {
                visualModel.rotation = preLandingTargetRotation; // Mantiene la rotazione di pre-atterraggio
            }
        }
        else if (currentIsGrounded && !currentIsBouncing) // Se siamo a terra E non stiamo facendo il saltino
        {
            // Siamo atterrati dopo un volo normale (non il bounce iniziale)
            // Esegui l'allineamento finale dolce verso la piattaforma
            AlignOnLandingTowardsPlatform(); // Allinea dolcemente al suolo
            hasAlignedPreLanding = false; // Resetta per il prossimo salto
            isAligning = false; // Finito l'allineamento forzato se c'era
        }
        else if (currentIsGrounded && currentIsBouncing)
        {
            // Siamo appena atterrati dal saltino (isBouncing è true ma isGrounded è true)
            // o siamo nella fase di "stabilizzazione" a terra del bounce.
            // Potremmo voler un allineamento specifico qui o lasciare che AlignOnLanding lo gestisca
            // quando isBouncing diventa false.
            AlignOnLandingTowardsPlatform();
            hasAlignedPreLanding = false;
        }


        // Logica di allineamento forzato (come quella chiamata da PreGameBouncer o per il primo bounce)
        // Questa parte era problematica se bloccava la rotazione in volo.
        // La `ForceAlignment` ora è più un "orientati rapidamente e poi lascia fare".
        if (isAligning) // Questo 'isAligning' è quello impostato da ForceAlignment
        {
            AlignOnLandingTowardsPlatform(); // Usa questo per l'allineamento attivo
            alignmentTimer += Time.deltaTime;
            if (alignmentTimer >= currentAlignmentDuration)
            {
                isAligning = false; // Termina l'allineamento forzato
            }
        }

        wasDescending = isDescending;
        wasGrounded = currentIsGrounded; // Ricorda lo stato precedente di grounded
        wasBouncing = currentIsBouncing; // Ricorda lo stato precedente di bouncing
    }

    // ForceAlignment ora serve per iniziare un periodo di allineamento attivo.
    // Non dovrebbe impedire RotateDuringFlight se la palla è poi di nuovo in aria.
    public void ForceAlignment(float duration = 0.5f)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        isAligning = true; // Attiva la modalità di allineamento
        alignmentTimer = 0f;
        currentAlignmentDuration = duration;
        // Non impostare direttamente la rotazione qui a meno che non sia un reset completo.
        // Lascia che AlignOnLandingTowardsPlatform() faccia il lavoro nell'Update().
        // Se vuoi un reset immediato della rotazione prima dell'allineamento:
        // if (visualModel != null) visualModel.localRotation = Quaternion.identity;
    }


    private void AlignPreLanding()
    {
        if (visualModel == null || directionToNextPlatform == Vector3.zero) return;

        preLandingTargetRotation = Quaternion.LookRotation(directionToNextPlatform, Vector3.up);
        visualModel.rotation = preLandingTargetRotation;
    }

    void UpdateDirectionToNextPlatform()
    {
        if (targetPlatform == null)
        {
            directionToNextPlatform = Vector3.zero;
            return;
        }
        Vector3 direction = targetPlatform.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            directionToNextPlatform = direction.normalized;
        }
        else
        {
            directionToNextPlatform = Vector3.zero;
            /*if (directionToNextPlatform == Vector3.zero)
                directionToNextPlatform = Vector3.forward;
            */
        }
    }

    void RotateDuringFlight()
    {
        if (visualModel == null || directionToNextPlatform == Vector3.zero) return;

        Vector3 axis = Vector3.Cross(Vector3.up, directionToNextPlatform);
        if (axis.sqrMagnitude > 0.001f)
        {
            visualModel.Rotate(axis, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void AlignOnLandingTowardsPlatform()
    {
        if (visualModel == null || directionToNextPlatform == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(directionToNextPlatform, upDirection);
        visualModel.rotation = Quaternion.Slerp(visualModel.rotation, lookRotation, landingAlignmentSpeed * Time.deltaTime);
    }

    private IEnumerator StartAlignmentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isAligning)
        {
            isAligning = true;
            alignmentTimer = 0f;
            currentAlignmentDuration = alignmentDuration;
            if (visualModel != null) visualModel.localRotation = Quaternion.identity;
        }
        alignmentDelayCoroutine = null;
    }

    public void ResetRotation()
    {
        if (visualModel != null)
        {
            visualModel.rotation = Quaternion.identity;
        }
        //isRotating = false;
        isAligning = false;
        alignmentTimer = 0f;
        if (alignmentDelayCoroutine != null)
        {
            StopCoroutine(alignmentDelayCoroutine);
            alignmentDelayCoroutine = null;
        }
        hasAlignedPreLanding = false;
    }

    /*
    public void EnableRotation()
    {
        rotationEnabled = true;
    }
    */
}
