using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class FadeOutButton : MonoBehaviour
{
    [Tooltip("Il GameObject Canvas (o pannello) che verrà dissolto.")]
    public GameObject canvasToFadeOut;
    [Tooltip("Il GameObject Canvas (o pannello) che verrà attivato dopo la dissolvenza.")]
    public GameObject canvasToEnable;
    [Tooltip("Se true, ricarica la scena dopo aver attivato canvasToEnable.")]
    public bool reloadSceneAfterActivation = true;

    [Header("Audio Settings")]
    [Tooltip("AudioSource da cui riprodurre il suono (Lasciare nullo se non si vuole un suono o si gestisce globalmente).")]
    public AudioSource audioSource; // AudioSource da assegnare
    [Tooltip("Clip audio da riprodurre al click (Lasciare nulla se non si vuole un suono).")]
    public AudioClip clickSound; // AudioClip da assegnare
    [Range(0f, 1f)]
    [Tooltip("Volume del suono del click.")]
    public float clickSoundVolume = 1f;

    private Button button;
    private AutoFadeCanvas fadeControllerToFadeOut;
    private bool listenerAdded = false; // Flag per evitare di aggiungere il listener più volte

    void Awake()
    {
        // Ottieni il riferimento al componente Button
        button = GetComponent<Button>();
        if (button == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError($"[{gameObject.name}] FadeOutButton: Button component non trovato! Assicurati che il GameObject abbia un componente Button.");
#endif
            enabled = false; // Disabilita lo script se manca il Button
            return;
        }
    }

    void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        if (!listenerAdded && button != null)
        {
            button.onClick.AddListener(HandleFadeOutAndEnableCanvas);
            listenerAdded = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }

        // Ottieni il riferimento all'AutoFadeCanvas (se presente)
        if (canvasToFadeOut != null && fadeControllerToFadeOut == null)
            fadeControllerToFadeOut = canvasToFadeOut.GetComponent<AutoFadeCanvas>();
    }

    void OnDisable()
    {
        // Rimuovi il listener per evitare memory leak
        if (button != null && listenerAdded)
        {
            button.onClick.RemoveListener(HandleFadeOutAndEnableCanvas);
            listenerAdded = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }
    }

    void HandleFadeOutAndEnableCanvas()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        if (audioSource != null && clickSound != null && SoundManager.soundEnabled)
        {
            audioSource.PlayOneShot(clickSound, clickSoundVolume);
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }

        StartCoroutine(FadeOutAndEnable());
    }

    private IEnumerator FadeOutAndEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        if (canvasToFadeOut != null)
        {
            if (fadeControllerToFadeOut != null)
            {
                fadeControllerToFadeOut.FadeOutCanvas();
                yield return new WaitForSeconds(fadeControllerToFadeOut.fadeDuration);
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
                canvasToFadeOut.SetActive(false);
            }
            else
            {
                canvasToFadeOut.SetActive(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[{gameObject.name}] FadeOutButton: Nessun AutoFadeCanvas trovato su {canvasToFadeOut.name}, disattivato immediatamente.");
#endif
            }
        }

        if (canvasToEnable != null)
        {
            canvasToEnable.SetActive(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }

        if (reloadSceneAfterActivation)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }
}