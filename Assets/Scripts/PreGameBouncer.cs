using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PreGameBouncer : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("Forza minima del salto in pre-game (InitialPlatform)")]
    public float minBounceForce = 1f;
    [Tooltip("Forza massima del salto in pre-game (InitialPlatform)")]
    public float maxBounceForce = 5f;

    [Header("Visual Settings")]
    [Tooltip("Il figlio BallVisual da ruotare")]
    public Transform visualModel;
    [Tooltip("Velocità di rotazione in gradi al secondo durante il volo")]
    public float rotationSpeed = 360f;

    [Header("Animation Settings")]
    [Tooltip("L'Animator su BallVisual che contiene la clip di squash/stretch")]
    public Animator squashAnimator;
    [Tooltip("Nome del trigger nell'Animator per far partire lo squash")]
    public string squashTriggerName = "Squash";
    [Tooltip("Nome dello stato Squash nell'Animator")]
    public string squashStateName = "Squash";
    [Tooltip("Nome dello stato Idle nell'Animator")]
    public string idleStateName = "Idle";
    [Tooltip("Ritardo (in secondi) dopo l'inizio dell'animazione prima di lanciare il salto")]
    public float jumpDelay = 0.3f;
    [Tooltip("Tempo minimo (in secondi) tra un salto e l'altro")]
    public float minTimeBetweenJumps = 0.5f;

    [Header("Alignment Settings")]
    [Tooltip("Durata dell'allineamento (in secondi) dopo l'apice")]
    public float alignmentDuration = 0.5f;

    [Tooltip("Abilita il bouncing pre-game")]
    public bool preGameBouncing = true;

    private Rigidbody rb;
    private Quaternion initialVisualLocalRotation;

    // Stati
    private bool isGrounded = false;
    private bool jumpScheduled = false;
    private bool isInFlight = false;
    private bool isAligning = false;
    private float lastJumpTime = -10f;

    // Rotazione / allineamento
    private float currentAngle = 0f;
    private float startAngle = 0f;
    private float targetAngle = 0f;
    private float alignTimer = 0f;
    private float prevYVelocity = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (visualModel == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("PreGameBouncer: visualModel non assegnato!");
#endif
        }
        if (squashAnimator == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("PreGameBouncer: squashAnimator non assegnato!");
#endif
        }

        initialVisualLocalRotation = visualModel != null ? visualModel.localRotation : Quaternion.identity;
        if (squashAnimator != null)
        {
            squashAnimator.speed = 1f;
            squashAnimator.ResetTrigger(squashTriggerName);
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!preGameBouncing || rb == null) return;

        if (collision.gameObject.CompareTag("InitialPlatform"))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            isGrounded = true;
            TryScheduleJump();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!preGameBouncing || rb == null) return;

        if (collision.gameObject.CompareTag("InitialPlatform"))
        {
            isGrounded = true;
            TryScheduleJump();
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("InitialPlatform"))
        {
            isGrounded = false;
        }
    }

    private void TryScheduleJump()
    {
        if (!isInFlight && !jumpScheduled && !isAligning && Time.time > lastJumpTime + minTimeBetweenJumps && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
        {
            ResetFlightState();
            StartCoroutine(TriggerAfterStabilize());
        }
    }

    IEnumerator TriggerAfterStabilize()
    {
        yield return new WaitForSeconds(0.05f);
        if (isGrounded && !isInFlight && !jumpScheduled && !isAligning)
        {
            TriggerSquashAndSchedule();
        }
    }

    void ResetFlightState()
    {
        isInFlight = false;
        isAligning = false;
        currentAngle = startAngle = targetAngle = alignTimer = prevYVelocity = 0f;
    }

    void TriggerSquashAndSchedule()
    {
        if (jumpScheduled)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("PreGameBouncer: Salto già pianificato");
#endif
            return;
        }
        if (!isGrounded || isInFlight)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("PreGameBouncer: Impossibile pianificare il salto ora");
#endif
            return;
        }

        jumpScheduled = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        if (squashAnimator != null)
        {
            squashAnimator.SetTrigger(squashTriggerName);
        }
        StartCoroutine(DelayedJump());
    }

    IEnumerator DelayedJump()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        yield return new WaitForSeconds(jumpDelay);

        if (squashAnimator != null)
        {
            squashAnimator.ResetTrigger(squashTriggerName);
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }

        if (!isGrounded || !preGameBouncing)
        {
            jumpScheduled = false;
            yield break;
        }

        float jf = Random.Range(minBounceForce, maxBounceForce);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jf, ForceMode.Impulse);
        isInFlight = true;
        jumpScheduled = false;
        lastJumpTime = Time.time;
        prevYVelocity = rb.linearVelocity.y;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        StartCoroutine(PreventNewAnimationWhileInFlight());
    }

    IEnumerator PreventNewAnimationWhileInFlight()
    {
        while (!isGrounded || isInFlight)
            yield return null;
        yield return new WaitForSeconds(0.1f);
        if (squashAnimator != null)
            squashAnimator.ResetTrigger(squashTriggerName);
    }

    void Update()
    {
        if (!preGameBouncing || visualModel == null || rb == null)
            return;

        float yVel = rb.linearVelocity.y;
        CheckAndFixAnimatorState();

        if (isInFlight)
        {
            currentAngle += rotationSpeed * Time.deltaTime;
            if (prevYVelocity > 0f && yVel <= 0f)
            {
                isInFlight = false;
                isAligning = true;
                alignTimer = 0f;
                startAngle = currentAngle;
                float rem = startAngle % 360f;
                targetAngle = startAngle + (360f - rem);
            }
        }
        else if (isAligning)
        {
            alignTimer += Time.deltaTime;
            float t = Mathf.Clamp01(alignTimer / alignmentDuration);
            currentAngle = Mathf.Lerp(startAngle, targetAngle, t);
            if (t >= 1f)
            {
                isAligning = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            }
        }

        visualModel.localRotation = initialVisualLocalRotation * Quaternion.Euler(currentAngle, 0f, 0f);
        prevYVelocity = yVel;
        if (isInFlight || isAligning || jumpScheduled)
{
    Vector3 pos = transform.position;
    pos.x = 0f;
    pos.z = 0f;
    transform.position = pos;

    Vector3 vel = rb.linearVelocity;
    vel.x = 0f;
    vel.z = 0f;
    rb.linearVelocity = vel;
}
    }

    void CheckAndFixAnimatorState()
    {
        if (squashAnimator == null) return;
        AnimatorStateInfo stateInfo = squashAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(squashStateName) && stateInfo.normalizedTime > 1.5f && isGrounded && !isInFlight && !jumpScheduled)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"PreGameBouncer: Animator bloccato in {squashStateName}, forzando {idleStateName}");
#endif
            squashAnimator.Play(idleStateName, 0, 0f);
            squashAnimator.ResetTrigger(squashTriggerName);
        }
    }

    public void DisableBouncing()
    {
        preGameBouncing = false;
        jumpScheduled = isInFlight = isAligning = false;
        visualModel.localRotation = initialVisualLocalRotation;
        if (squashAnimator != null)
        {
            squashAnimator.Play(idleStateName, 0, 0f);
            squashAnimator.ResetTrigger(squashTriggerName);
        }
    }

    public void ResetAnimationState()
    {
        if (squashAnimator != null)
        {
            squashAnimator.Play(idleStateName, 0, 0f);
            squashAnimator.ResetTrigger(squashTriggerName);
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }
        jumpScheduled = false;
    }
}
