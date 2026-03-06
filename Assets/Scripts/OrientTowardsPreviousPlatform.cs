using UnityEngine;

public class OrientTowardsPreviousPlatform : MonoBehaviour
{
    [Header("Orientamento Automatico")]
    [Tooltip("Se true, la piattaforma si orienterà verso quella precedente all'attivazione.")]
    public bool orientTowardsPrevious = true;
    [Tooltip("Se true, l'orientamento verso la piattaforma precedente avverrà solo sull'asse Y (orizzontale).")]
    public bool orientOnYAxisOnly = true;

    [Header("Rotazione Aggiuntiva Locale")]
    [Tooltip("Rotazione aggiuntiva da applicare all'oggetto sui suoi assi locali (X, Y, Z in gradi) DOPO l'orientamento automatico.")]
    public Vector3 additionalLocalRotationDegrees = Vector3.zero;

    private Transform previousPlatformTransform;
    //private bool hasBeenOriented = false;

    // Questo metodo verrà chiamato da PlatformSpawner
    // subito dopo che questa piattaforma è stata istanziata.
    public void InitializeAndOrient(Transform platformThatSpawnedThis)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        if (platformThatSpawnedThis == null && orientTowardsPrevious)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"OrientTowardsPreviousPlatform: Previous platform transform not provided for {gameObject.name}, but orientation is required.");
#endif
            // Non si orienta se manca il riferimento e l'orientamento è richiesto.
            // Applica solo la rotazione aggiuntiva se specificata
            if (additionalLocalRotationDegrees != Vector3.zero)
            {
                transform.Rotate(additionalLocalRotationDegrees, Space.Self);
            }
            //hasBeenOriented = true; // Consideralo "orientato" per evitare chiamate multiple da Start
            return;
        }

        this.previousPlatformTransform = platformThatSpawnedThis;

        if (orientTowardsPrevious && previousPlatformTransform != null)
        {
            Vector3 dir = previousPlatformTransform.position - transform.position;
            if (orientOnYAxisOnly) dir.y = 0;

            if (dir.sqrMagnitude > 0.001f)
            {
                OrientSelf();
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"OrientTowardsPreviousPlatform: {gameObject.name} skipped orientation (zero direction vector).");
#endif
            }
        }

        // Applica la rotazione aggiuntiva specificata nell'Inspector
        // Questa avviene DOPO l'eventuale orientamento automatico
        if (additionalLocalRotationDegrees != Vector3.zero)
        {
            // Space.Self assicura che la rotazione sia relativa agli assi attuali dell'oggetto
            transform.Rotate(additionalLocalRotationDegrees, Space.Self);
        }
        //hasBeenOriented = true;
    }

    // Potresti voler chiamare questo in Start se l'inizializzazione potesse avvenire in ritardo
    // o se lo script fosse aggiunto a oggetti già in scena senza passare per lo Spawner.
    // void Start()
    // {
    //     if (!hasBeenOriented && orientTowardsPrevious && previousPlatformTransform != null)
    //     {
    //         OrientSelf();
    //         if (additionalLocalRotationDegrees != Vector3.zero)
    //         {
    //             transform.Rotate(additionalLocalRotationDegrees, Space.Self);
    //         }
    //         hasBeenOriented = true;
    //     }
    //     else if (!hasBeenOriented && additionalLocalRotationDegrees != Vector3.zero)
    //     {
    //         // Se non c'è orientamento automatico o manca previousPlatformTransform,
    //         // applica solo la rotazione aggiuntiva
    //         transform.Rotate(additionalLocalRotationDegrees, Space.Self);
    //         hasBeenOriented = true;
    //     }
    // }

    void OrientSelf()
    {
        if (previousPlatformTransform == null) return;

        // Calcola la direzione verso la piattaforma precedente
        Vector3 directionToPrevious = previousPlatformTransform.position - transform.position;

        // Se specificato, ignora la differenza di altezza per l'orientamento
        // e fa ruotare la piattaforma solo sull'asse Y (orizzontale).
        if (orientOnYAxisOnly)
        {
            directionToPrevious.y = 0;
        }

        // Assicurati che la direzione non sia un vettore nullo
        if (directionToPrevious.sqrMagnitude > 0.001f)
        {
            // Crea la rotazione che fa puntare l'asse Z locale (forward) verso la direzione calcolata.
            // Il secondo argomento (Vector3.up) definisce qual è l' "alto" del mondo.
            Quaternion targetRotation = Quaternion.LookRotation(directionToPrevious.normalized); // Normalizza per sicurezza
            transform.rotation = targetRotation;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"OrientTowardsPreviousPlatform: {gameObject.name} could not orient due to zero direction vector.");
#endif
        }
    }
}