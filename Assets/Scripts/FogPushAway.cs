using UnityEngine;

/// Mantiene la nebbia aperta finché la palla è dentro O finché
/// non è passata exitGraceTime dall’ultimo OnTriggerExit.
[RequireComponent(typeof(ParticleSystem), typeof(SphereCollider))]
public class FogPushAway : MonoBehaviour
{
    [Header("Forza che spinge le particelle")]
    [SerializeField] private float pushStrength   = 1.2f;

    [Header("Quando la palla esce, la spinta cala in ... secondi")]
    [SerializeField] private float returnSpeed    = 1f;

    [Header("Tempo di tolleranza dopo l’uscita (per il saltino)")]
    [SerializeField] private float exitGraceTime  = 0.4f;   // deve coprire bounceDelay + mezzo rimbalzo

    private ParticleSystem ps;
    private ParticleSystem.VelocityOverLifetimeModule vel;
    private float currentStrength;
    private float graceTimer;             // >0 ⇒ siamo nel periodo di tolleranza

    void Awake()
    {
        ps  = GetComponent<ParticleSystem>();
        vel = ps.velocityOverLifetime;
        vel.enabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            graceTimer      = exitGraceTime;   // resetta il timer
            currentStrength = pushStrength;    // subito alla massima spinta
            vel.radial      = new ParticleSystem.MinMaxCurve(currentStrength);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            graceTimer = exitGraceTime;        // parte il conto alla rovescia
        }
    }

    void LateUpdate()
    {
        // 1) Decresce il grace timer
        if (graceTimer > 0f)
            graceTimer -= Time.deltaTime;

        // 2) Determina il “target” (pushStrength se siamo ancora in grace, altrimenti 0)
        float target = graceTimer > 0f ? pushStrength : 0f;

        // 3) Avvicina morbidamente currentStrength al target
        currentStrength = Mathf.MoveTowards(currentStrength, target, returnSpeed * Time.deltaTime);

        // 4) Aggiorna il modulo
        vel.radial = new ParticleSystem.MinMaxCurve(currentStrength);
    }
}
