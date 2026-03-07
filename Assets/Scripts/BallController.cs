using UnityEngine;
using System.Collections;

public class BallController : MonoBehaviour
{
    public bool gameStarted = false; // Impostato tramite StartGameActions

    [Header("Jump Settings")]
    public float minJumpDistance = 1f;
    public float maxJumpDistance = 10f;
    public float minChargeTime = 0.1f;
    public float maxChargeTime = 2f;

    [Header("Vertical Jump Control")]
    [Tooltip("Altezza massima raggiungibile dal salto")]
    public float maxJumpHeight = 3f; // Imposta un valore predefinito a tuo piacimento

    [Header("Flight Speed")]
    [Tooltip("Moltiplicatore velocita in volo (aggiornato dal DifficultyManager se presente). >1 = piu veloce, stessa traiettoria.")]
    public float flightSpeedMultiplier = 1f;
    private bool flightSpeedFromDifficulty = false;

    [Header("Platform Target")]
    public Transform targetPlatform;
    private PlatformSpawner spawner; // Riferimento al PlatformSpawner

    [Header("Physics")]
    public Rigidbody rb;

    [Header("Bounce Settings")]
    public float bounceForce = 3f;
    public float bounceDelay = 0.2f;
    private bool hasBounced = false;
    public bool isBouncing = false;

    [Header("Animation Settings")]
    private CollisionAnimationPlayer animationPlayer;
    //private bool isCharging = false;
    //private float chargeTimer = 0f;
    private const float pauseTime = 0.1f;
    private Animator ballAnimator;
    private float currentChargeTime;
#pragma warning disable CS0414
    private float animationTimer;
#pragma warning disable CS0414
    private bool isChargingAnimation;
    private bool hasPausedAnimation;
    private bool canPlayLandAnimation = true; // Flag per controllare se l'animazione può partire
    private float landAnimationCooldown = 1f; // Durata minima (secondi) tra due animazioni 'land'

    //[Header("Inflation/Deflation Settings")]
    public Transform ballVisual;

    [Header("Explosion & Game Over Settings")]
    public GameObject explosionPrefab;
    public float explosionHeight = -5f;
    public float gameOverDelay = 2f;
    private bool hasExploded = false;

    [Header("Score Settings")]
    public ScoreManager scoreManager;

    [Header("Game Over Manager")]
    public GameOverManager gameOverManager;
    private DifficultyManager difficultyManager; // Riferimento al DifficultyManager
    public float chargeTime = 0f;
    private bool isGrounded = true;
    public bool gameEnded = false;
    private float timeSinceJumpStart = 0f;
    private float estimatedFlightTime = 1f;
    private float jumpStartScale = 1f;
    // Flag per evitare di generare ripetutamente la nuova piattaforma nella stessa collisione.
    private bool hasGeneratedNextPlatform = false;

    [Header("Audio Settings")]
    public AudioSource audioSource; // Riferimento all'AudioSource
    public float bounceSoundVolume = 1f; // Volume per il suono normale
    public AudioClip bounceSound; // Suono del rimbalzo (aterraggio iniziale)
    public AudioSource bounceEndAudioSource; // Riferimento per l'AudioSource del suono di fine bounce
    public AudioClip bounceEndSound; // Suono da riprodurre quando la palla atterra dopo il bounce
    public float bounceEndSoundVolume = 1f; // Volume per il suono di fine bounce

    [Header("Explosion Audio Settings")]
    public AudioClip explosionSound; // Suono da riprodurre quando la palla esplode
    public float explosionSoundVolume = 1f; // Volume del suono dell'esplosione

    [Header("Inflation Audio Settings")]
    public AudioClip inflationSound; // Suono da riprodurre durante l'inflation
    public float inflationSoundVolume = 1f; // Volume del suono di inflation
    // Flag per distinguere l'atterraggio successivo a un bounce.
    private bool isBounceLanding = false;
    // Flag per controllare se il suono di inflation è già in riproduzione
    private bool inflationSoundPlaying = false;
    public AudioSource inflationAudioSource;
    [Header("Auto Jump Settings")]
    public float audioInflationDuration = 6f; // Durata del file audio di inflation (6 secondi)
    private float inflationStartTime = 0f; // Momento in cui inizia l'inflation
    public BallRotationController ballRotationController;
    private bool firstCollisionDetected = false;

    public bool IsGrounded()
    {
        return isGrounded;
    }

    private bool ballIsActive = false;

    public void ActivateBall()
    {
        ballIsActive = true;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Recupera l'oggetto iniziale con tag "InitialPlatform" e imposta il target.
        GameObject initPlat = GameObject.FindGameObjectWithTag("InitialPlatform");
        if (initPlat)
        {
            targetPlatform = initPlat.transform;
        }
        // Se necessario, qui si potrebbe gestire la mancanza dell'oggetto iniziale.

        // Ottieni il riferimento al DifficultyManager
        difficultyManager = Object.FindFirstObjectByType<DifficultyManager>();

        ballAnimator = ballVisual.GetComponentInChildren<Animator>();

        animationPlayer = GetComponentInChildren<CollisionAnimationPlayer>();
        if (animationPlayer == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogError("BallController: non trovo CollisionAnimationPlayer nei figli!");
#endif
        }
        else
        {
            // Reset dei flag importanti all'avvio
            hasExploded = false;
            hasBounced = false;
            isBounceLanding = false;
            gameEnded = false;
            inflationSoundPlaying = false;
        }
    }

    // Metodo per assegnare il PlatformSpawner, chiamato da StartGameActions dopo l'attivazione del Platform Generator.
    public void SetPlatformSpawner(PlatformSpawner newSpawner)
    {
        if (newSpawner != null)
        {
            spawner = newSpawner;
        }
        // Eventuale gestione di errore in caso di newSpawner nullo.
    }

    void Update()
    {
        HandleJumpInput();
        CheckForExplosion();

        if (!isGrounded)
            timeSinceJumpStart += Time.deltaTime;

        HandlePrejumpAnimation();
        if (gameStarted && !firstCollisionDetected)
        {
            firstCollisionDetected = true;

            /*if (ballRotationController != null)
            {
                // Attiva lo script BallRotationController
                ballRotationController.enabled = true;

                // Se vuoi, chiama anche un metodo per iniziare la rotazione
                ballRotationController.EnableRotation();
            }*/
        }

    }

    void CheckForExplosion()

    {
        if (gameEnded || hasExploded) return;
        if (transform.position.y <= explosionHeight && !hasExploded)
            TriggerExplosion();
    }

    void TriggerExplosion()
    {
        // Interrompi il suono di inflation se sta ancora suonando
        StopInflationSound();

        // Riproduce il suono dell'esplosione con il volume specificato
        if (audioSource != null && explosionSound != null && SoundManager.soundEnabled)
        {
            audioSource.PlayOneShot(explosionSound, explosionSoundVolume);
        }

        // Istanzia l'effetto esplosivo, se assegnato
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // Disabilita i renderer e i collider dei figli per "nascondere" la palla
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Ferma il movimento della palla
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Attiva la schermata di Game Over dopo un breve ritardo
        StartCoroutine(ActivateGameOverAfterDelay());

        // Segnala che l'esplosione è avvenuta e il gioco è terminato
        hasExploded = true;
        gameEnded = true; // Blocca ulteriori input
    }

    IEnumerator ActivateGameOverAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);
        if (gameOverManager != null)
            gameOverManager.TriggerGameOver("GAME OVER");
    }

    // Gestione dell'input: qui si parte il suono di inflation quando l'utente preme e si ferma quando rilascia
    private void HandleJumpInput()
    {
        // Evita il salto se il gioco non è iniziato
        if (!gameStarted) return;
        if (gameEnded) return;

        // Altrimenti esegui la logica del salto
        if (Input.GetMouseButtonDown(0))
            if (gameEnded || isBouncing) return;

        if (isGrounded && !isBouncing)
        {
            if (Input.GetMouseButton(0))
            {
                // Se è il primo frame in cui premiamo, registra il tempo di inizio
                if (chargeTime == 0f)
                {
                    inflationStartTime = Time.time;
                }

                // Blocca la rotazione sui 3 assi quando inizia la carica
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeRotation; // Blocca tutta la rotazione

                // Aggiorna il tempo di carica, ma non oltre il maxChargeTime
                chargeTime = Mathf.Clamp(chargeTime + Time.deltaTime, minChargeTime, maxChargeTime);

                // Avvia il suono se non è già in riproduzione
                if (!inflationSoundPlaying)
                {
                    StartInflationSound();
                }

                // Controlla se il tempo di inflation ha raggiunto la durata dell'audio
                if (Time.time - inflationStartTime >= audioInflationDuration)
                {
                    // Imposta il tempo di carica al massimo per un salto completo
                    chargeTime = maxChargeTime;

                    // Simula il rilascio del pulsante per eseguire il salto
                    StopInflationSound();
                    hasExploded = false;
                    hasBounced = false;
                    isBounceLanding = false;
                    PerformJump();
                    chargeTime = 0f;
                    isGrounded = false;

                    // Ripristina i vincoli del rigidbody
                    rb.constraints = RigidbodyConstraints.None;
                }
            }
            // Questa condizione viene eseguita solo quando il pulsante viene rilasciato
            else if (Input.GetMouseButtonUp(0))
            {
                // Ripristina i vincoli del rigidbody al rilascio
                rb.constraints = RigidbodyConstraints.None;

                StopInflationSound(); // Assicurati che il suono si fermi al rilascio
                hasExploded = false;
                hasBounced = false;
                isBounceLanding = false;
                PerformJump();
                chargeTime = 0f;
                isGrounded = false;
            }
            // Assicurati che il suono si fermi anche se si rilascia il touch prima del tempo massimo
            else if (!Input.GetMouseButton(0) && inflationSoundPlaying)
            {
                // Assicurati che i vincoli vengano ripristinati anche se il suono si ferma
                rb.constraints = RigidbodyConstraints.None;

                StopInflationSound();
            }
        }
        // Se non è a terra e il suono è ancora attivo (dovrebbe essere fermato al rilascio, ma per sicurezza)
        else if (!isGrounded && inflationSoundPlaying)
        {
            StopInflationSound();
        }
    }

    // Aggiorna la scala visiva della palla in base al caricamento del salto

    // Funzioni per gestire il suono dell'inflation
    private void StartInflationSound()
    {
        if (!inflationSoundPlaying && inflationAudioSource != null && inflationSound != null && SoundManager.soundEnabled)
        {
            inflationSoundPlaying = true;
            inflationAudioSource.loop = true;
            inflationAudioSource.clip = inflationSound;
            inflationAudioSource.volume = inflationSoundVolume;
            inflationAudioSource.Play();
        }
    }

    private void StopInflationSound()
    {
        if (inflationSoundPlaying && inflationAudioSource != null)
        {
            inflationSoundPlaying = false;
            inflationAudioSource.Stop();
            inflationAudioSource.loop = false;
        }
    }

    private void PerformJump()
    {
        if (gameEnded) return;
        // Verifica che targetPlatform non sia null prima di accedervi
        if (targetPlatform == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("PerformJump: targetPlatform è null, cerco una nuova piattaforma iniziale");
#endif
            // Cerca di trovare una piattaforma come fallback
            GameObject initPlat = GameObject.FindGameObjectWithTag("InitialPlatform");
            if (initPlat)
            {
                targetPlatform = initPlat.transform;
            }
            else
            {
                // Se non troviamo neanche una piattaforma iniziale, usiamo un salto generico
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError("PerformJump: Nessuna piattaforma trovata, eseguo un salto generico");
#endif
                // Applica un salto generico in avanti
                float chargeTimeNormalized = Mathf.Pow(Mathf.InverseLerp(minChargeTime, maxChargeTime, chargeTime), 1.5f);
                float verticalVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * maxJumpHeight);
                verticalVelocity = Mathf.Lerp(verticalVelocity * 0.7f, verticalVelocity, chargeTimeNormalized);

                // Salta in una direzione fissa (ad esempio avanti)
                rb.linearVelocity = new Vector3(0f, verticalVelocity, Mathf.Lerp(minJumpDistance, maxJumpDistance, chargeTimeNormalized));

                // Imposta i valori necessari per la funzione
                estimatedFlightTime = (2 * verticalVelocity) / Mathf.Abs(Physics.gravity.y);
                estimatedFlightTime = Mathf.Max(estimatedFlightTime * 0.8f, 0.5f);
                timeSinceJumpStart = 0f;
                //jumpStartScale = ballVisual.localScale.x;

                return;
            }
        }

        try
        {
            // Aggiorna flightSpeedMultiplier dal DifficultyManager
            if (difficultyManager != null)
            {
                float t = difficultyManager.GetNormalizedDifficulty();
                flightSpeedMultiplier = Mathf.Lerp(
                    difficultyManager.initialFlightSpeedMultiplier,
                    difficultyManager.ultimateFlightSpeedMultiplier,
                    t);
            }

            // Normalizza il tempo di carica
            float chargeTimeNormalized = Mathf.Pow(Mathf.InverseLerp(minChargeTime, maxChargeTime, chargeTime), 1.5f);

            // Calcolo della distanza orizzontale verso la piattaforma
            float horizontalDistance = Mathf.Lerp(minJumpDistance, maxJumpDistance, chargeTimeNormalized);

            // Direzione verso la piattaforma
            Vector3 directionHorizontal = (targetPlatform.position - transform.position).normalized;
            directionHorizontal.y = 0; // Movimento orizzontale

            // Incrementa la velocità orizzontale
            float horizontalSpeed = horizontalDistance / 1.2f; // Riduci il divisore per maggiore velocità
            Vector3 horizontalVelocity = directionHorizontal * horizontalSpeed;

            // Incrementa la velocità verticale
            float maxVerticalVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * maxJumpHeight);
            float verticalVelocity = Mathf.Lerp(maxVerticalVelocity * 0.7f, maxVerticalVelocity, chargeTimeNormalized);

            // Applica il moltiplicatore di velocita (flightSpeedMultiplier scala la velocita,
            // BallGravityController scala la gravita di flightSpeedMultiplier^2 per mantenere la stessa traiettoria)
            float totalMultiplier = 1.2f * flightSpeedMultiplier;
            rb.linearVelocity = new Vector3(horizontalVelocity.x * totalMultiplier, verticalVelocity * totalMultiplier, horizontalVelocity.z * totalMultiplier);

            // Tempo di volo stimato (ridotto proporzionalmente alla velocita)
            estimatedFlightTime = (2 * verticalVelocity) / Mathf.Abs(Physics.gravity.y);
            estimatedFlightTime = Mathf.Max((estimatedFlightTime * 0.8f) / flightSpeedMultiplier, 0.5f);

            timeSinceJumpStart = 0f;
            jumpStartScale = ballVisual.localScale.x;

            // Magnetismo verso la piattaforma - verifico che il componente e la piattaforma esistano
            BallMagnetism ballMagnetism = GetComponent<BallMagnetism>();
            if (ballMagnetism != null && targetPlatform != null)
            {
                ballMagnetism.ActivateMagnetism(targetPlatform);
            }
        }
        catch (System.Exception e)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("Errore durante l'esecuzione di PerformJump: " + e.Message);
#endif

            // Implementa un fallback per evitare che il gioco si blocchi
            float chargeTimeNormalized = Mathf.Pow(Mathf.InverseLerp(minChargeTime, maxChargeTime, chargeTime), 1.5f);
            float verticalVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * maxJumpHeight);
            verticalVelocity = Mathf.Lerp(verticalVelocity * 0.7f, verticalVelocity, chargeTimeNormalized);

            // Applica un salto generico in avanti
            rb.linearVelocity = new Vector3(0f, verticalVelocity, Mathf.Lerp(minJumpDistance, maxJumpDistance, chargeTimeNormalized));

            // Imposta i valori necessari per la funzione
            estimatedFlightTime = (2 * verticalVelocity) / Mathf.Abs(Physics.gravity.y);
            estimatedFlightTime = Mathf.Max(estimatedFlightTime * 0.8f, 0.5f);
            timeSinceJumpStart = 0f;
            //jumpStartScale = ballVisual.localScale.x;
        }
        /*BallRotationController rotController = GetComponent<BallRotationController>();
        if (rotController != null)
        {
            rotController.EnableRotation();
        }*/
    }

    void OnCollisionEnter(Collision collision)
    {


        if (collision.gameObject.CompareTag("InitialPlatform") || collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("Planet"))
        {
            // Controlla se l'atterraggio e su una superficie orizzontale
            bool isHorizontalLanding = false;
            foreach (ContactPoint contact in collision.contacts)
            {
                float angle = Vector3.Angle(contact.normal, Vector3.up);
                if (angle <= 45f)
                {
                    isHorizontalLanding = true;
                    break;
                }
            }

            // Se non e un atterraggio orizzontale (spigolo/lato), lascia che la fisica gestisca
            if (!isHorizontalLanding) return;

            if (animationPlayer != null)
                animationPlayer.PlayLandAnimation();
            else
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("[BallController] animationPlayer è NULL, non posso riprodurre animazione!");
#endif
                StartCoroutine(LandAnimationCooldown());

            rb.linearVelocity = Vector3.zero;

            // Se siamo in atterraggio post-bounce, riproduci il suono modificato
            if (isBounceLanding && bounceEndAudioSource != null && bounceEndSound != null && SoundManager.soundEnabled)
            {
                bounceEndAudioSource.PlayOneShot(bounceEndSound, bounceEndSoundVolume);
                isBounceLanding = false;
            }
            else if (!isBounceLanding && audioSource != null && bounceSound != null && SoundManager.soundEnabled)
            {
                audioSource.PlayOneShot(bounceSound, bounceSoundVolume);
            }

            // Bounce solo su atterraggio orizzontale
            if (!hasBounced && ballIsActive)
            {
                // Blocca temporaneamente il movimento su X e Z
                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

                StartCoroutine(DelayedBounce());
                isBouncing = true;
                hasBounced = true;
                isBounceLanding = true;
                StartCoroutine(EndBounce());
            }

            if (targetPlatform != null && collision.gameObject == targetPlatform.gameObject)
            {
                if (!hasGeneratedNextPlatform)
                {
                    if (spawner == null)
                        return;

                    hasGeneratedNextPlatform = true;

                    if (spawner != null)
                    {
                        spawner.SpawnNextPlatform();
                        GameObject nextPlatform = spawner.GetNextPlatform();
                        if (nextPlatform != null)
                            targetPlatform = nextPlatform.transform;
                    }
                    if (ballRotationController != null)
                        ballRotationController.ForceAlignment(ballRotationController.alignmentDuration);
                    else
                        Debug.LogWarning("[BallController] ballRotationController è NULL in OnCollisionEnter");
                }

                if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("Planet") && !collision.gameObject.CompareTag("InitialPlatform"))
                {
                    if (difficultyManager != null && !difficultyManager.firstJumpOccurred)
                    {
                        difficultyManager.firstJumpOccurred = true;

                    }
                }
            }

        }
    }

    private IEnumerator LandAnimationCooldown()
    {
        // Aspetta per la durata del cooldown
        yield return new WaitForSeconds(landAnimationCooldown);
        // Riabilita l'esecuzione dell'animazione
        canPlayLandAnimation = true;
    }
    // Coroutine che applica la forza di rimbalzo con un ritardo
    private IEnumerator DelayedBounce()
    {
        yield return new WaitForSeconds(bounceDelay);
        if (!gameEnded)
        {
            rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);

        }
    }

    // Resetta il flag di generazione quando il contatto termina
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("InitialPlatform") || collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("Planet"))
        {
            hasGeneratedNextPlatform = false;

        }
    }

    IEnumerator EndBounce()
    {
        yield return new WaitForSeconds(1f);
        // Se la palla non ha atterrato entro questo tempo, resettiamo comunque il flag.
        isBouncing = false;
        // Resettiamo il tempo di carica alla fine del bounce

        /* 
              if (isGrounded) // Assicuriamoci che sia ancora a terra dopo il (potenziale) ritardo
               {
                   chargeTime = 0f;
                   //ballVisual.localScale = Vector3.one; // Resettiamo anche la scala visiva per sicurezza
               }
       */
        if (!gameEnded)
        {
            isBouncing = false;
            isGrounded = true;
            chargeTime = 0f;
            //ballVisual.localScale = Vector3.one;
            // Sblocca i vincoli sulla posizione
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    void HandlePrejumpAnimation()
    {
        if (!gameStarted) return;

        // Caso normale: il player ha appena premuto il touch
        if (isGrounded && !isBouncing && Input.GetMouseButtonDown(0))
        {
            ballAnimator.Play("Ball_prejump", 0, 0f);
            currentChargeTime = 0f;
            animationTimer = 0f;
            isChargingAnimation = true;
            hasPausedAnimation = false;

            float targetAnimationTime = 0.25f;
            float halfChargeTime = maxChargeTime / 2f;
            ballAnimator.speed = targetAnimationTime / halfChargeTime;
        }

        // 👇 NUOVO caso: il player sta già tenendo premuto mentre la palla torna a terra
        else if (isGrounded && !isBouncing && Input.GetMouseButton(0) && !isChargingAnimation)
        {
            ballAnimator.Play("Ball_prejump", 0, 0f);
            currentChargeTime = 0f;
            animationTimer = 0f;
            isChargingAnimation = true;
            hasPausedAnimation = false;

            float targetAnimationTime = 0.25f;
            float halfChargeTime = maxChargeTime / 2f;
            ballAnimator.speed = targetAnimationTime / halfChargeTime;
        }

        // Progresso animazione durante la carica
        if (isChargingAnimation && Input.GetMouseButton(0))
        {
            currentChargeTime += Time.deltaTime;

            if (currentChargeTime >= maxChargeTime / 2f && !hasPausedAnimation)
            {
                ballAnimator.speed = 0f;
                hasPausedAnimation = true;
            }
        }

        // Rilascio del tocco → riprende animazione
        if (Input.GetMouseButtonUp(0) && isChargingAnimation)
        {
            ballAnimator.speed = 1f;
            isChargingAnimation = false;
        }
    }

    // Metodo per resettare lo stato del controller, da chiamare quando si riavvia il gioco
    public void ResetController()
    {
        // Resetta tutti i flag e le variabili di stato
        hasExploded = false;
        hasBounced = false;
        isBounceLanding = false;
        gameEnded = false;
        isBouncing = false;
        isGrounded = true;
        inflationSoundPlaying = false;
        chargeTime = 0f;

        // Trova una nuova piattaforma iniziale
        GameObject initPlat = GameObject.FindGameObjectWithTag("InitialPlatform");
        if (initPlat)
        {
            targetPlatform = initPlat.transform;
        }

        // Resetta la scala visiva
        //if (ballVisual != null)
        //{
        //    ballVisual.localScale = Vector3.one;
        //}

        // Assicurati che tutti i renderer e collider siano attivi
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = true;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = true;

        // Resetta la fisica
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
        }

        // Ferma eventuali suoni in riproduzione
        StopInflationSound();

    }
    public void ResetBounceState()
{
    hasBounced = false;
    isBouncing = false;
    isBounceLanding = false;
}
}