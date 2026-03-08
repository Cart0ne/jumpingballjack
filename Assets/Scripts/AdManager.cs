using System;
using Unity.Services.LevelPlay;
using UnityEngine;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("Ads Configuration")]
    [Tooltip("Attiva/disattiva gli ads. OFF durante sviluppo, ON per release.")]
    public bool enableAds = false;

    [Tooltip("App Key fornito da LevelPlay dashboard")]
    public string appKey = "257a0d61d";

    [Tooltip("Ad Unit ID interstitial fornito da LevelPlay dashboard")]
    public string interstitialAdUnitId = "k6aj9aj3t6ergu19";

    [Tooltip("Tempo minimo in secondi tra un interstitial e l'altro")]
    public float minTimeBetweenAds = 30f;

    private LevelPlayInterstitialAd interstitialAd;
    private bool sdkInitialized;
    private float lastAdTime = -999f;
    private Action pendingAction;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!enableAds)
            return;

        InitializeAds();
    }

    private void InitializeAds()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

#if UNITY_IOS
        // ATT consent - gli ads funzionano anche senza, ma pagano meno
        LevelPlay.SetConsent(true);
#endif

        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        sdkInitialized = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("AdManager: LevelPlay SDK inizializzato con successo");
#endif
        CreateInterstitialAd();
        LoadInterstitial();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"AdManager: Inizializzazione SDK fallita - {error.ErrorMessage}");
#endif
    }

    private void CreateInterstitialAd()
    {
        interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitId);
        interstitialAd.OnAdLoaded += OnAdLoaded;
        interstitialAd.OnAdLoadFailed += OnAdLoadFailed;
        interstitialAd.OnAdDisplayed += OnAdDisplayed;
        interstitialAd.OnAdDisplayFailed += OnAdDisplayFailed;
        interstitialAd.OnAdClosed += OnAdClosed;
    }

    private void LoadInterstitial()
    {
        if (!sdkInitialized || interstitialAd == null)
            return;

        interstitialAd.LoadAd();
    }

    /// <summary>
    /// Mostra un interstitial se possibile, poi esegue l'azione.
    /// Se ads disabilitati, tempo insufficiente, o ad non pronto: esegue l'azione direttamente.
    /// </summary>
    public void ShowInterstitialThenExecute(Action action)
    {
        if (!enableAds || !sdkInitialized)
        {
            action?.Invoke();
            return;
        }

        if (Time.unscaledTime - lastAdTime < minTimeBetweenAds)
        {
            action?.Invoke();
            return;
        }

        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            pendingAction = action;
            interstitialAd.ShowAd();
        }
        else
        {
            action?.Invoke();
            // Prova a caricare per la prossima volta
            LoadInterstitial();
        }
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("AdManager: Interstitial caricato");
#endif
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"AdManager: Caricamento interstitial fallito - {error.ErrorMessage}");
#endif
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("AdManager: Interstitial mostrato");
#endif
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"AdManager: Visualizzazione interstitial fallita - {error.ErrorMessage}");
#endif
        // Se il display fallisce, esegui comunque l'azione
        pendingAction?.Invoke();
        pendingAction = null;
        LoadInterstitial();
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("AdManager: Interstitial chiuso");
#endif
        lastAdTime = Time.unscaledTime;
        pendingAction?.Invoke();
        pendingAction = null;
        // Pre-carica il prossimo interstitial
        LoadInterstitial();
    }

    void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
        interstitialAd?.DestroyAd();
    }
}
