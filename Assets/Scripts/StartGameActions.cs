using UnityEngine;
using System.Collections;
using TMPro; // Assicurati che TextMeshPro sia importato nel progetto

public class StartGameActions : MonoBehaviour
{
    public static bool skipStartScreen = false;
    public GameObject startCanvas;
    public Camera mainCamera;
    public float cameraSizeTarget = 10f;
    public float cameraSizeSpeed = 0.5f; // Valore tipico per SmoothDamp, 2f è molto veloce
    public GameObject scoreText;
    public GameObject platformGenerator;
    public float fadeDuration = 1f;
    public TMP_Text startBestScore;

    public bool gameStarted = false;

    public bool IsGameStartSequenceInitiated()
    {
        return gameStarted;
    }

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip startButtonSound;

    private CameraController cameraController;
    private CanvasGroup startCanvasGroup;
    private CanvasGroup scoreTextGroup;

    void Start()
    {
        if (scoreText != null)
        {
            scoreTextGroup = scoreText.GetComponent<CanvasGroup>();
            if (scoreTextGroup == null)
            {
                scoreTextGroup = scoreText.AddComponent<CanvasGroup>();
            }
            scoreTextGroup.alpha = 0f;
            scoreText.SetActive(true);
        }

        if (startBestScore != null)
        {
            int bestScore = PlayerPrefs.GetInt("BestScore", 0);
            startBestScore.text = "Best Score: " + bestScore;
        }

        if (platformGenerator != null)
        {
            platformGenerator.SetActive(false);
        }

        if (mainCamera != null)
        {
            Vector3 pos = mainCamera.transform.position;
            pos.y = 6f;
            mainCamera.transform.position = pos;

            cameraController = mainCamera.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.enabled = false;
            }
        }

        if (startCanvas != null)
        {
            startCanvasGroup = startCanvas.GetComponent<CanvasGroup>();
            if (startCanvasGroup == null)
            {
                startCanvasGroup = startCanvas.AddComponent<CanvasGroup>();
            }
            startCanvasGroup.alpha = 1f;
            startCanvas.SetActive(true);
        }

        if (skipStartScreen)
        {
            skipStartScreen = false;
            if (startCanvasGroup != null)
            {
                startCanvasGroup.alpha = 0f;
                if (startCanvas != null) startCanvas.SetActive(false);
            }
            if (scoreTextGroup != null)
            {
                scoreTextGroup.alpha = 1f;
            }
            StartImmediateGame();
            return;
        }
    }

    private void StartImmediateGame()
    {
        gameStarted = true;
        BallController ball = Object.FindFirstObjectByType<BallController>();
        if (ball != null)
        {
            ball.ResetBounceState();
            ball.ActivateBall(); // se stai ancora usando questa parte
        }


        if (platformGenerator != null)
        {
            platformGenerator.SetActive(true);
            StartCoroutine(InitializePlatformSpawnerCoroutine());
        }

        PreGameBouncer preGameBouncer = Object.FindFirstObjectByType<PreGameBouncer>();
        if (preGameBouncer != null)
        {
            preGameBouncer.DisableBouncing();
        }

        BallController ballController = Object.FindFirstObjectByType<BallController>();
        if (ballController != null)
        {
            ballController.gameStarted = true;
        }

        if (mainCamera != null)
        {
            // Per un avvio immediato, potresti voler impostare la dimensione direttamente:
            // mainCamera.orthographicSize = cameraSizeTarget;
            // O mantenere una transizione veloce:
            StartCoroutine(ChangeCameraSizeCoroutine(mainCamera, cameraSizeTarget, cameraSizeSpeed));
        }

        if (cameraController != null)
        {
            cameraController.enabled = true;
        }
    }

    void Update()
    {
        if (!gameStarted && mainCamera != null)
        {
            Vector3 pos = mainCamera.transform.position;
            if (!Mathf.Approximately(pos.y, 6f))
            {
                pos.y = 6f;
                mainCamera.transform.position = pos;
            }
        }
    }

    public void StartGame()
    {
        if (gameStarted) return; // Prevenzione avvii multipli

        if (audioSource != null && startButtonSound != null && SoundManager.soundEnabled)
        {
            audioSource.PlayOneShot(startButtonSound);
        }

        gameStarted = true;

        BallController ball = Object.FindFirstObjectByType<BallController>();
        if (ball != null)
        {
            ball.ResetBounceState();
            ball.ActivateBall(); // se stai ancora usando questa parte
        }

        BallController ballController = Object.FindFirstObjectByType<BallController>();
        if (ballController != null)
        {
            ballController.gameStarted = true;
        }

        if (startCanvasGroup != null && scoreTextGroup != null)
        {
            StartCoroutine(FadeStartThenShowScoreCoroutine());
        }
        else if (scoreTextGroup != null)
        {
            if (!scoreTextGroup.gameObject.activeSelf) scoreTextGroup.gameObject.SetActive(true);
            StartCoroutine(FadeCanvasCoroutine(scoreTextGroup, scoreTextGroup.alpha, 1f, fadeDuration));
        }


        if (platformGenerator != null)
        {
            platformGenerator.SetActive(true);
            StartCoroutine(InitializePlatformSpawnerCoroutine());
        }

        PreGameBouncer preGameBouncer = Object.FindFirstObjectByType<PreGameBouncer>();
        if (preGameBouncer != null)
        {
            preGameBouncer.DisableBouncing();
        }

        if (mainCamera != null)
        {
            StartCoroutine(ChangeCameraSizeCoroutine(mainCamera, cameraSizeTarget, cameraSizeSpeed));
        }

        if (cameraController != null)
        {
            cameraController.enabled = true;
        }
    }

    private IEnumerator FadeCanvasCoroutine(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float time = 0f;
        if (duration <= 0f)
        {
            canvasGroup.alpha = endAlpha;
        }
        else
        {
            // Cache initial alpha for lerp if not provided as startAlpha argument (though current usage does provide it)
            // float initialAlpha = canvasGroup.alpha; // Use this if startAlpha is not passed
            while (time < duration)
            {
                time += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
                yield return null;
            }
            canvasGroup.alpha = endAlpha;
        }

        if (endAlpha == 0f && canvasGroup.gameObject != null)
        {
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private IEnumerator InitializePlatformSpawnerCoroutine()
    {
        yield return new WaitForSeconds(0.1f);

        yield return null;

        PlatformSpawner spawner = Object.FindFirstObjectByType<PlatformSpawner>();
        if (spawner != null)
        {
            GameObject initPlat = GameObject.FindGameObjectWithTag("InitialPlatform");
            if (initPlat != null)
            {
                spawner.SetInitialPlatform(initPlat);
            }

            BallController ballController = Object.FindFirstObjectByType<BallController>();
            if (ballController != null)
            {
                ballController.SetPlatformSpawner(spawner);
                ballController.gameStarted = true;
            }

            // 🔄 SPOSTATO QUI: stop del bouncing solo quando tutto è pronto
            PreGameBouncer preGameBouncer = Object.FindFirstObjectByType<PreGameBouncer>();
            if (preGameBouncer != null)
            {
                preGameBouncer.DisableBouncing();
            }
        }
    }

    private IEnumerator ChangeCameraSizeCoroutine(Camera camera, float targetSize, float cameraSizeSpeedValue)
    {
        if (camera == null) yield break;

        if (cameraSizeSpeedValue <= 0.001f)
        {
            camera.orthographicSize = targetSize;
            yield break;
        }

        float velocity = 0f;
        while (Mathf.Abs(camera.orthographicSize - targetSize) > 0.01f)
        {
            camera.orthographicSize = Mathf.SmoothDamp(camera.orthographicSize, targetSize, ref velocity, cameraSizeSpeedValue);
            yield return null;
        }
        camera.orthographicSize = targetSize;
    }

    private IEnumerator FadeStartThenShowScoreCoroutine()
    {
        if (startCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasCoroutine(startCanvasGroup, startCanvasGroup.alpha, 0f, fadeDuration));
        }

        if (scoreTextGroup != null)
        {
            if (!scoreTextGroup.gameObject.activeSelf && scoreTextGroup.alpha < 1f)
            {
                scoreTextGroup.gameObject.SetActive(true);
            }
            yield return StartCoroutine(FadeCanvasCoroutine(scoreTextGroup, scoreTextGroup.alpha, 1f, fadeDuration));
        }
    }
}