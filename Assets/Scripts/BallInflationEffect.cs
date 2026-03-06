using UnityEngine;

[RequireComponent(typeof(BallController))]
public class BallInflationEffect : MonoBehaviour
{
    [Header("Inflation Effect Settings")]
    [Tooltip("Prefab dell'effetto visivo con Particle System")]
    public GameObject inflationVFXPrefab;
    [Tooltip("Offset di posizione rispetto al centro della palla")]
    public Vector3 effectOffset = new Vector3(0f, -0.2f, 0f);
    [Range(0.5f, 3f), Tooltip("Scala generale dell'effetto")]
    public float effectScale = 1f;

    private BallController ballController;
    private GameObject activeEffect;
    private ParticleSystem effectParticles;
    private bool effectActive = false;
    private Vector3 baseEffectScale; // Scala originale del prefab VFX

    void Start()
    {
        ballController = GetComponent<BallController>();

        if (inflationVFXPrefab != null)
        {
            InitializeEffect();
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("Nessun prefab per l'effetto di inflazione assegnato a BallInflationEffect.", this);
#endif
        }
    }

    // Istanzia e configura l'effetto all'inizio
    void InitializeEffect()
    {
        activeEffect = Instantiate(
            inflationVFXPrefab,
            transform.position + effectOffset, // Posizione iniziale
            Quaternion.identity,               // Rotazione iniziale
            transform                          // Rendi figlio della palla
        );

        activeEffect.SetActive(false); // Inizia disattivato
        effectParticles = activeEffect.GetComponent<ParticleSystem>();

        if (effectParticles == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("Il prefab inflationVFXPrefab assegnato non contiene un Particle System!", this);
#endif
            Destroy(activeEffect); // Distruggi l'istanza inutile se manca il componente chiave
            activeEffect = null;   // Azzera il riferimento
            return;
        }

        // Memorizza la scala originale del prefab per riferimento
        baseEffectScale = activeEffect.transform.localScale;
    }

    void Update()
    {
        // Controlli di sicurezza
        if (activeEffect == null || ballController == null) return;

        // Determina se l'effetto dovrebbe essere attivo
        bool shouldBeActive = ballController.IsGrounded() &&
                            Input.GetMouseButton(0) && // Il tasto è premuto
                            ballController.chargeTime > 0f; // C'è tempo di carica (la palla si sta "gonfiando")

        // Gestisce l'attivazione/disattivazione e Play/Stop delle particelle
        ManageEffectState(shouldBeActive);

        // Se l'effetto è attivo, aggiorna le sue proprietà (posizione/scala)
        if (effectActive)
        {
            UpdateEffectProperties();
        }
    }

    // Attiva o disattiva l'effetto particellare
    void ManageEffectState(bool activate)
    {
        // Cambia stato solo se necessario
        if (activate != effectActive)
        {
            effectActive = activate;
            activeEffect.SetActive(activate);

            if (activate)
            {
                effectParticles.Play();
                UpdateEffectScale(); // Applica la scala definita nell'inspector
            }
            else
            {
                // Ferma l'emissione ma lascia morire le particelle esistenti
                effectParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    // Aggiorna posizione e scala dell'effetto mentre è attivo
    void UpdateEffectProperties()
    {
        // Assicura che l'effetto segua la palla con l'offset definito
        activeEffect.transform.position = transform.position + effectOffset;

        // Assicura che la scala rimanga quella definita (utile se altre cose la modificano)
        UpdateEffectScale();
    }

    // Applica la scala desiderata all'effetto
    void UpdateEffectScale()
    {
        // Combina la scala base del prefab con il moltiplicatore dell'inspector
        activeEffect.transform.localScale = baseEffectScale * effectScale;
    }
}