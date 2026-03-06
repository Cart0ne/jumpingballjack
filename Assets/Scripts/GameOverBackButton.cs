using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class GameOverBackButton : MonoBehaviour
{
    [Tooltip("Il GameObject Canvas (o pannello) che verrà dissolto (es. il GameOverPanel).")]
    public GameObject canvasToFadeOut;

    [Header("Audio Settings")]
    [Tooltip("AudioSource da cui riprodurre il suono (Lasciare nullo se non si vuole un suono o si gestisce globalmente).")]
    public AudioSource audioSource;
    [Tooltip("Clip audio da riprodurre al click (Lasciare nulla se non si vuole un suono).")]
    public AudioClip clickSound;
    [Range(0f, 1f)]
    [Tooltip("Volume del suono del click.")]
    public float clickSoundVolume = 1f;

    private Button button;
    private AutoFadeCanvas fadeControllerToFadeOut;
    private bool listenerAdded = false;
    private bool isActionInProgress = false;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError($"[{gameObject.name}] Button component non trovato! Assicurati che il GameObject abbia un componente Button.");
#endif
            enabled = false;
            return;
        }

        // L'AudioSource deve essere assegnato nell'Inspector se non è sullo stesso GameObject
    }

    void OnEnable()
    {
        if (!listenerAdded && button != null)
        {
            button.onClick.AddListener(HandleClickAction);
            listenerAdded = true;
        }

        if (canvasToFadeOut != null && fadeControllerToFadeOut == null)
            fadeControllerToFadeOut = canvasToFadeOut.GetComponent<AutoFadeCanvas>();

        isActionInProgress = false;
    }

    void OnDisable()
    {
        if (button != null && listenerAdded)
        {
            button.onClick.RemoveListener(HandleClickAction);
            listenerAdded = false;
        }
    }

    void HandleClickAction()
    {
        if (isActionInProgress) return;
        isActionInProgress = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        // Riproduci il suono del click (se assegnato)
        // Assumiamo SoundManager.soundEnabled esista, altrimenti rimuovi quel controllo
        if (audioSource != null && clickSound != null && SoundManager.soundEnabled)
        {
            audioSource.PlayOneShot(clickSound, clickSoundVolume);
        }

        StartCoroutine(ProcessButtonAction());
    }

    private IEnumerator ProcessButtonAction()
    {
        // Disabilita l'interazione del bottone mentre l'azione è in corso
        if (button != null) button.interactable = false;

        // Fase 1: Dissolvenza del canvasToFadeOut (es. GameOverPanel)
        if (canvasToFadeOut != null)
        {
            if (fadeControllerToFadeOut != null)
            {
                fadeControllerToFadeOut.FadeOutCanvas();
                yield return new WaitForSeconds(fadeControllerToFadeOut.fadeDuration);
                // canvasToFadeOut.SetActive(false); // Questa riga potrebbe essere gestita da AutoFadeCanvas
            }
            else
            {
                canvasToFadeOut.SetActive(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[{gameObject.name}] Nessun AutoFadeCanvas trovato su {canvasToFadeOut.name}, disattivato immediatamente.");
#endif
            }
        }

        // Fase 2: Chiama GameOverManager.GoBackToStart()
        GameOverManager gameOverManagerInstance = FindFirstObjectByType<GameOverManager>();
        if (gameOverManagerInstance != null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            gameOverManagerInstance.GoBackToStart(); // Delega tutta la logica (che ricarica la scena)
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError($"[{gameObject.name}] GameOverManager non trovato! Impossibile chiamare GoBackToStart(). La scena NON verrà ricaricata da questo script.");
#endif
             // Se non ricarica la scena, il flag isActionInProgress dovrebbe essere resettato
             isActionInProgress = false;
        }

        // Questa coroutine terminerà qui perché GameOverManager.GoBackToStart() ricarica la scena.
    }
}