using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class AutoFadeCanvas : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public float fadeDuration = 0.5f; // Durata predefinita del fade in/out in secondi

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("CanvasGroup non trovato su questo GameObject: " + gameObject.name + ". Lo script AutoFadeCanvas richiede un CanvasGroup.");
#endif
            enabled = false;
        }

        canvasGroup.alpha = 0f; // Inizia con l'alpha a 0
    }

    void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        FadeInCanvas(); // Avvia il fade-in quando il Canvas è abilitato
    }

    /// <summary>
    /// Avvia il fade-in del Canvas.
    /// </summary>
    public void FadeInCanvas()
    {
        StartCoroutine(FadeCanvas(0f, 1f));
    }

    /// <summary>
    /// Avvia manualmente il fade-out del Canvas.
    /// </summary>
    public void FadeOutCanvas()
    {
        StartCoroutine(FadeCanvas(1f, 0f));
    }

    private IEnumerator FadeCanvas(float startAlpha, float endAlpha)
    {
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }
}