using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameOverPanel;
    public CanvasGroup gameOverCanvasGroup;
    public TMP_Text gameOverText;
    public TMP_Text textActualScore;
    public TMP_Text textBestScore;
    public TMP_Text scoreText;

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float cameraMoveSpeed = 2f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip gameOverSound;
    public float gameOverSoundVolume = 1f;
    public AudioClip playAgainSound;

    private ScoreManager scoreManager;
    private DifficultyManager difficultyManager;
    private Vector3 targetPosition = new Vector3(-5f, 6f, -5f);
    private Quaternion targetRotation = Quaternion.Euler(26f, 41f, 0f);
    private float targetOrthographicSize = 6.5f;
    private float exitAnimTotalDuration = 1f;

    private AutoFadeCanvas gameOverFadeController;
    public float gameOverFadeDuration = 1f;

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("GameOverManager: GameOverPanel non è assegnato!");
#endif
        }

        if (gameOverCanvasGroup != null)
            gameOverCanvasGroup.alpha = 1f;
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("GameOverManager: gameOverCanvasGroup non è assegnato!");
#endif
        }

        scoreManager = Object.FindFirstObjectByType<ScoreManager>();
        difficultyManager = Object.FindFirstObjectByType<DifficultyManager>();
        if (difficultyManager == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("GameOverManager: DifficultyManager non trovato nella scena!");
#endif
        }
    }

    public void TriggerGameOver(string message = "GAME OVER")
    {
        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverText != null)
                gameOverText.text = message;

            if (scoreManager != null)
            {
                if (textActualScore != null)
                    textActualScore.text = "Your Score: " + scoreManager.GetCurrentScore();
                if (textBestScore != null)
                    textBestScore.text = "Best Score: " + scoreManager.GetBestScore();
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("GameOverManager: ScoreManager non trovato.");
#endif
            }

            CameraController cc = mainCamera.GetComponent<CameraController>();
            if (cc != null)
                cc.enabled = false;

            if (audioSource != null && gameOverSound != null && SoundManager.soundEnabled)
            {
                audioSource.PlayOneShot(gameOverSound, gameOverSoundVolume);
            }
            else if (audioSource == null || gameOverSound == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("GameOverManager: AudioSource o GameOverSound non assegnati!");
#endif
            }

            StartCoroutine(HandleGameOverSequence());
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("GameOverManager: GameOverPanel non è assegnato!");
#endif
        }
    }

    private IEnumerator HandleGameOverSequence()
    {
        List<GameObject> objectsToDestroyAfterDelay = new List<GameObject>();

        GameObject[] platforms = GameObject.FindGameObjectsWithTag("Platform");
        foreach (GameObject plat in platforms)
        {
            if (!plat.CompareTag("InitialPlatform"))
            {
                PlatformExitAnimation exitAnim = plat.GetComponent<PlatformExitAnimation>();
                if (exitAnim != null)
                {
                    exitAnim.AnimateExit();
                }
                else
                {
                    objectsToDestroyAfterDelay.Add(plat);
                }
            }
        }

        GameObject[] planets = GameObject.FindGameObjectsWithTag("Planet");
        foreach (GameObject planet in planets)
        {
            PlatformExitAnimation exitAnim = planet.GetComponent<PlatformExitAnimation>();
            if (exitAnim != null)
            {
                exitAnim.AnimateExit();
            }
            else
            {
                objectsToDestroyAfterDelay.Add(planet);
            }
        }

        yield return new WaitForSeconds(exitAnimTotalDuration);

        foreach (GameObject obj in objectsToDestroyAfterDelay)
        {
            if (obj != null)
                Destroy(obj);
        }

        yield return StartCoroutine(MoveCameraToStartView());
    }

    private IEnumerator MoveCameraToStartView()
    {
        if (mainCamera == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("GameOverManager: MainCamera non è assegnata!");
#endif
            yield break;
        }

        if (SkyboxController.Instance != null)
        {
            SkyboxController.Instance.TransitionToInitialSkybox();
        }

        float moveDuration = cameraMoveSpeed;
        float elapsedTime = 0f;
        Vector3 initialCamPos = mainCamera.transform.position;
        Quaternion initialCamRot = mainCamera.transform.rotation;
        float initialCamSize = mainCamera.orthographic ? mainCamera.orthographicSize : 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            float easeVal = 1f - Mathf.Pow(1f - t, 2);

            mainCamera.transform.position = Vector3.Lerp(initialCamPos, targetPosition, easeVal);
            mainCamera.transform.rotation = Quaternion.Slerp(initialCamRot, targetRotation, easeVal);
            if (mainCamera.orthographic)
                mainCamera.orthographicSize = Mathf.Lerp(initialCamSize, targetOrthographicSize, easeVal);

            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
        if (mainCamera.orthographic)
            mainCamera.orthographicSize = targetOrthographicSize;

        StartCoroutine(FreezeCamera());
    }

    private IEnumerator FreezeCamera()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            if (mainCamera != null)
            {
                mainCamera.transform.position = targetPosition;
                mainCamera.transform.rotation = targetRotation;
                if (mainCamera.orthographic)
                    mainCamera.orthographicSize = targetOrthographicSize;
            }
        }
    }

    public void RestartGame()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        if (audioSource != null && playAgainSound != null && SoundManager.soundEnabled)
        {
            audioSource.PlayOneShot(playAgainSound);
        }
        else if (audioSource == null || playAgainSound == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("GameOverManager: AudioSource o PlayAgainSound non assegnati!");
#endif
        }

        if (difficultyManager != null)
        {
            difficultyManager.ResetDifficulty();
#if UNITY_EDITOR || DEVELOPMENT_BUILD


#endif
        }

        StartGameActions.skipStartScreen = true;

        if (SkyboxController.Instance != null)
        {
            SkyboxController.resetSkyboxOnLoad = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }

        StartCoroutine(FadeGameOverAndRestart());
    }

    public void GoBackToStart()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        StartGameActions.skipStartScreen = false;

        if (SkyboxController.Instance != null)
        {
            SkyboxController.resetSkyboxOnLoad = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (difficultyManager != null)
        {
            difficultyManager.ResetDifficulty();
#if UNITY_EDITOR || DEVELOPMENT_BUILD


#endif
        }
    }

    private IEnumerator FadeGameOverAndRestart()
    {
        if (gameOverCanvasGroup == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("GameOverManager: GameOverCanvasGroup non è assegnato!");
#endif
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < gameOverFadeDuration)
        {
            elapsed += Time.deltaTime;
            gameOverCanvasGroup.alpha = 1f - (elapsed / gameOverFadeDuration);
            yield return null;
        }
        gameOverCanvasGroup.alpha = 0f;

        if (SkyboxController.Instance != null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            Destroy(SkyboxController.Instance.gameObject);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}