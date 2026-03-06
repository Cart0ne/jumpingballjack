using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SkyboxController : MonoBehaviour
{
    public static SkyboxController Instance { get; private set; }

    [Header("Skybox Settings")]
    public Material[] skyboxes;
    public Material initialSkybox;
    public int platformsPerChange = 5;
    public float transitionDuration = 1f;

    [Header("UI Fade Settings")]
    public Image fadeOverlay;
    public float inGameTransitionDuration = 2f;
    public float gameOverTransitionDuration = 0.5f;

    private int currentSkyboxIndex = 0;
    private int platformCount = 0;
    private Color originalOverlayColor;
    
    // Flag pubblico per resettare lo skybox system quando si preme Play Again
    public static bool resetSkyboxOnLoad = false;
    
    private bool isFirstLoad
    {
        get { return PlayerPrefs.GetInt("IsFirstLoad", 1) == 1; }
        set { PlayerPrefs.SetInt("IsFirstLoad", value ? 1 : 0); }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(transform.root.gameObject);
        }
        else if (Instance != this)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("SkyboxController: Un'altra istanza di SkyboxController è stata trovata e distrutta.");
#endif
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Se il fadeOverlay non esiste, lo cerca nel nuovo contesto
        if (fadeOverlay == null)
        {
            GameObject fadeObj = GameObject.FindWithTag("FadeOverlay");
            if (fadeObj != null)
            {
                fadeOverlay = fadeObj.GetComponent<Image>();
                if (fadeOverlay != null)
                {
                    originalOverlayColor = fadeOverlay.color;
                }
            }
        }
        else
        {
            // Aggiorna il colore originale
            originalOverlayColor = fadeOverlay.color;
        }

        // Resetta lo stato dello sfondo al primo caricamento della scena o quando resetSkyboxOnLoad è true
        if (isFirstLoad || resetSkyboxOnLoad)
        {
            // Reimposta i contatori e gli indici
            platformCount = 0;
            currentSkyboxIndex = 0;

            if (initialSkybox == null && skyboxes.Length > 0)
            {
                initialSkybox = skyboxes[0];
            }

            RenderSettings.skybox = initialSkybox;
            DynamicGI.UpdateEnvironment();

            isFirstLoad = false; // Imposta il flag a false dopo il primo caricamento
            resetSkyboxOnLoad = false; // Reimposta il flag di reset
            PlayerPrefs.Save(); // Salva immediatamente il valore
        }
        else
        {
            // Se non è il primo caricamento, mantieni lo skybox attuale
            DynamicGI.UpdateEnvironment(); // Assicurati che l'illuminazione ambientale sia aggiornata
        }
    }

    /// <summary>
    /// Chiamare questo metodo ogni volta che viene generata una piattaforma.
    /// </summary>
    public void AddPlatform()
    {
        platformCount++;
        if (platformCount >= platformsPerChange)
        {
            platformCount = 0;
            ChangeSkybox();
        }
    }

    /// <summary>
    /// Cambia lo skybox (cambio in-game).
    /// </summary>
    public void ChangeSkybox()
    {
        currentSkyboxIndex = (currentSkyboxIndex + 1) % skyboxes.Length;
        StartCoroutine(SkyboxTransition());
    }

    private IEnumerator SkyboxTransition()
    {
        if (fadeOverlay == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("SkyboxController: fadeOverlay non assegnato. Transizione interrotta.");
#endif
            yield break;
        }

        float t = 0f;
        while (t < inGameTransitionDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / inGameTransitionDuration);
            fadeOverlay.color = new Color(originalOverlayColor.r, originalOverlayColor.g, originalOverlayColor.b, alpha);
            yield return null;
        }

        RenderSettings.skybox = skyboxes[currentSkyboxIndex];
        DynamicGI.UpdateEnvironment();

        t = 0f;
        while (t < inGameTransitionDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / inGameTransitionDuration);
            fadeOverlay.color = new Color(originalOverlayColor.r, originalOverlayColor.g, originalOverlayColor.b, alpha);
            yield return null;
        }
    }

    /// <summary>
    /// Esegue il crossfade finale (ad es. durante il Game Over) che passa dallo skybox corrente allo skybox iniziale.
    /// Se lo skybox corrente è già l'initialSkybox, la transizione non viene eseguita.
    /// </summary>
    public IEnumerator CrossfadeToInitialSkybox()
    {
        if (fadeOverlay == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("SkyboxController: fadeOverlay non assegnato. Crossfade interrotto.");
#endif
            yield break;
        }

        if (RenderSettings.skybox == initialSkybox)
            yield break;

        float t = 0f;
        while (t < gameOverTransitionDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / gameOverTransitionDuration);
            fadeOverlay.color = new Color(originalOverlayColor.r, originalOverlayColor.g, originalOverlayColor.b, alpha);
            yield return null;
        }
        fadeOverlay.color = new Color(originalOverlayColor.r, originalOverlayColor.g, originalOverlayColor.b, 1f);

        RenderSettings.skybox = initialSkybox;
        DynamicGI.UpdateEnvironment();

        t = 0f;
        while (t < gameOverTransitionDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / gameOverTransitionDuration);
            fadeOverlay.color = new Color(originalOverlayColor.r, originalOverlayColor.g, originalOverlayColor.b, alpha);
            yield return null;
        }
        fadeOverlay.color = new Color(originalOverlayColor.r, originalOverlayColor.g, originalOverlayColor.b, 0f);
    }

    /// <summary>
    /// Avvia il crossfade finale verso lo skybox iniziale.
    /// </summary>
    public void TransitionToInitialSkybox()
    {
        StartCoroutine(CrossfadeToInitialSkybox());
    }
}