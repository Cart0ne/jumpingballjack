using UnityEngine;
using System.Collections;

public class PlatformEntryAnimation : MonoBehaviour
{
    [Header("Parametri Rise")]
    public float animationDuration = 1.5f;
    public float verticalOffset = -10f;
    public AnimationCurve riseCurve;

    [Header("Parametri Oscillazione")]
    public float oscillationAmplitude = 2f;
    public float oscillationSpeed = 3f;
    public float restDuration = 0.5f;
    public float oscillationBlendDuration = 0.2f;
    public float straightenDuration = 0.3f; // Nuovo parametro per la durata del raddrizzamento

    private Vector3 targetPosition;
    private Vector3 startPosition;
    private Quaternion initialRotation;
    private DifficultyManager difficultyManager; // Riferimento non utilizzato nel codice fornito, ma mantenuto.
    private bool shouldOscillate = true;
    private float oscillationTime = 0f;
    private Coroutine oscillationCoroutine; // Riferimento alla coroutine di oscillazione per poterla fermare
    private Coroutine straightenCoroutine; // Riferimento alla coroutine di raddrizzamento

    public void Initialize(Vector3 finalPosition)
    {
        // Trova il DifficultyManager (mantenuto come nel codice originale)
        difficultyManager = FindFirstObjectByType<DifficultyManager>();

        targetPosition = finalPosition;
        startPosition = targetPosition + new Vector3(0, verticalOffset, 0);
        transform.position = startPosition;
        initialRotation = transform.rotation; // Salva la rotazione iniziale completa

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        StartCoroutine(AnimateSequence());
    }

    private IEnumerator AnimateSequence()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        // Fase di Rise
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            if (riseCurve != null)
                t = riseCurve.Evaluate(t);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        transform.position = targetPosition;
        // Reimposta la rotazione Z a 0 dopo il rise, mantenendo X e Y originali se necessario
        // (Questo assume che l'oscillationAngle venga applicato solo alla Z)
        transform.rotation = Quaternion.Euler(initialRotation.eulerAngles.x, initialRotation.eulerAngles.y, 0);

        // Fase di Riposo
        yield return new WaitForSeconds(restDuration);

        // Verifica se la piattaforma dovrà muoversi prima di creare il ventilatore
        var movementComponent = GetComponent<PlatformVerticalMovement>();
        if (movementComponent != null && movementComponent.WillMove)
        {
            movementComponent.InstantiateFan();
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            shouldOscillate = false; // Imposta shouldOscillate a false se la piattaforma si muoverà
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            // Se la piattaforma NON si muoverà verticalmente, allora dovrebbe oscillare (se shouldOscillate è ancora true)
            if (shouldOscillate)
            {
                // Avvia la coroutine di oscillazione e memorizza il riferimento
                oscillationCoroutine = StartCoroutine(OscillateCoroutine());
            }
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }

    private IEnumerator OscillateCoroutine()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        // Fase di Transizione all'Oscillazione
        float blendElapsed = 0f;
        Quaternion startRotation = transform.rotation; // Parte dalla rotazione attuale (Z=0 dopo il rise)
        while (blendElapsed < oscillationBlendDuration)
        {
            blendElapsed += Time.deltaTime;
            float blendT = Mathf.Clamp01(blendElapsed / oscillationBlendDuration);
            // Calcola l'angolo di oscillazione iniziale per iniziare fluidamente
            float currentOscTime = blendT * (2 * Mathf.PI / oscillationSpeed); // Usa il tempo per la transizione
            float oscAngle = Mathf.Sin(currentOscTime * oscillationSpeed) * oscillationAmplitude;
            Quaternion targetOscRotation = Quaternion.Euler(initialRotation.eulerAngles.x, initialRotation.eulerAngles.y, oscAngle);
            transform.rotation = Quaternion.Lerp(startRotation, targetOscRotation, blendT);
            yield return null;
        }

        // Fase di Oscillazione Continua
        oscillationTime = 0f; // Resetta il tempo per l'oscillazione continua
        while (shouldOscillate)
        {
            oscillationTime += Time.deltaTime;
            float oscAngle = Mathf.Sin(oscillationTime * oscillationSpeed) * oscillationAmplitude;
            transform.rotation = Quaternion.Euler(initialRotation.eulerAngles.x, initialRotation.eulerAngles.y, oscAngle);
            yield return null;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }

    private IEnumerator StraightenPlatformCoroutine(float duration)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(initialRotation.eulerAngles.x, initialRotation.eulerAngles.y, 0); // Target Z angle is 0

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t); // Usiamo Slerp per rotazioni più uniformi
            yield return null;
        }
        transform.rotation = targetRotation; // Assicura che sia esattamente a 0 alla fine
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Controlla se la piattaforma sta attualmente oscillando E non è già in fase di raddrizzamento
            if (shouldOscillate && straightenCoroutine == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
                shouldOscillate = false; // Ferma la coroutine di oscillazione

                // Avvia la coroutine di raddrizzamento e memorizza il riferimento
                straightenCoroutine = StartCoroutine(StraightenPlatformCoroutine(straightenDuration));

                // Opzionale: Ferma esplicitamente la coroutine di oscillazione
                if (oscillationCoroutine != null)
                {
                    StopCoroutine(oscillationCoroutine);
                }
            }
            else if (!shouldOscillate && straightenCoroutine == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
                // Se non stava oscillando ma non si stava nemmeno raddrizzando,
                // questo blocco potrebbe essere utile per gestire altri casi,
                // ma per la logica richiesta non è strettamente necessario agire qui.
            }
            // Se straightenCoroutine non è null, significa che si stava già raddrizzando
            // non facciamo nulla per non interrompere l'animazione corrente.
        }
    }
}