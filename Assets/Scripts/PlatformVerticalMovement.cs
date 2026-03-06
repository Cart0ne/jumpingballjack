using UnityEngine;
using System.Collections;

public class PlatformVerticalMovement : MonoBehaviour
{
    private float moveDistance = 2f;
    private float moveSpeed = 2f;

    private Vector3 startVerticalPosition;
    private Vector3 endPositionUp;
    private Vector3 endPositionDown;
    private bool movingUp = true;
    private bool canMove = false;
    private PlatformEntryAnimation entryAnimation;
    private DifficultyManager difficultyManager;

    public bool WillMove { get; private set; } = false;

    [Header("Visual Effects")]
    public GameObject fanPrefab;
    public float fanVerticalOffset = 1.5f; // Distanza verticale del target del ventilatore sotto la piattaforma
    private FanController fanController;

    void Start()
    {
        entryAnimation = GetComponent<PlatformEntryAnimation>();
        difficultyManager = FindFirstObjectByType<DifficultyManager>();

        if (difficultyManager != null && Random.value < difficultyManager.verticalMovementProbability)
        {
            moveSpeed = Random.Range(difficultyManager.minVerticalMoveSpeed, difficultyManager.maxVerticalMoveSpeed);
            moveDistance = Random.Range(difficultyManager.minVerticalMoveDistance, difficultyManager.maxVerticalMoveDistance);
            WillMove = true;
            Invoke("StartPlatformMovement", (entryAnimation != null ? entryAnimation.animationDuration : 0f));
        }
        else
        {
            enabled = false;
        }
    }

    void StartPlatformMovement()
    {
        startVerticalPosition = transform.position;
        endPositionUp = startVerticalPosition + transform.up * moveDistance;
        endPositionDown = startVerticalPosition - transform.up * moveDistance;

        if (fanController == null)
            canMove = true;
    }

    void Update()
    {
        if (canMove)
        {
            Vector3 targetPosition = movingUp ? endPositionUp : endPositionDown;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                movingUp = !movingUp;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            canMove = false;
        }
    }

    public void InstantiateFan()
    {
        Vector3 fanPosition = transform.position - transform.up * fanVerticalOffset;
        GameObject fanInstance = Instantiate(fanPrefab, fanPosition, Quaternion.identity);

        fanController = fanInstance.GetComponent<FanController>();
        if (fanController != null)
        {
            fanController.OnFanReachedTarget += EnableMovement;
            fanController.SetTargetPosition(transform.position, fanVerticalOffset); // Passa la posizione della piattaforma E l'offset
            fanController.StartFanSequence();
        }
    }

    void EnableMovement()
    {
        canMove = true;
    }
}