using UnityEngine;

public class BallGravityController : MonoBehaviour
{
    [Tooltip("Fattore moltiplicativo della gravità quando la pallina non è in contatto con una superficie orizzontale (1 = gravità normale)")]
    public float offPlatformGravityScale = 1f;

    [Tooltip("Angolo massimo di inclinazione per considerare la superficie come orizzontale (in gradi)")]
    public float maxHorizontalAngle = 30f;

    private Rigidbody rb;
    private bool isOnPlatform = false;
    private DifficultyManager difficultyManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        difficultyManager = FindFirstObjectByType<DifficultyManager>();
    }

    void FixedUpdate()
    {
        float targetGravityScale;

        if (isOnPlatform)
        {
            if (difficultyManager != null)
            {
                float normalizedDifficulty = difficultyManager.GetNormalizedDifficulty();
                float initialGravity = difficultyManager.initialOnPlatformGravityScale;
                float gravityIncreaseRange = 1f - initialGravity;
                float scaledGravityIncrease = gravityIncreaseRange * difficultyManager.gravityIncreaseMultiplier;
                targetGravityScale = Mathf.Lerp(initialGravity, initialGravity + scaledGravityIncrease, normalizedDifficulty);
            }
            else
            {
                targetGravityScale = 0.1f;
            }
        }
        else
        {
            targetGravityScale = offPlatformGravityScale;
        }

        rb.AddForce(Physics.gravity * targetGravityScale, ForceMode.Acceleration);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("InitialPlatform") || collision.gameObject.CompareTag("Planet"))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                float angle = Vector3.Angle(contact.normal, Vector3.up);
                if (angle <= maxHorizontalAngle)
                {
                    isOnPlatform = true;
                    break;
                }
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("InitialPlatform") || collision.gameObject.CompareTag("Planet"))
        {
            isOnPlatform = false;
        }
    }
}