using UnityEngine;
using System.Collections;

public class StartScreenFadeIn : MonoBehaviour
{
    private CanvasGroup startCanvasGroup;
    public float fadeInDuration = 2f; // Durata del fade-in in secondi

    void OnEnable()
    {
        // Ottieni il CanvasGroup del Canvas di Start
        startCanvasGroup = GetComponent<CanvasGroup>();
        if (startCanvasGroup == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("CanvasGroup non trovato sul GameObject a cui è attaccato StartScreenFadeIn! Impossibile eseguire il fade-in.");
#endif
            enabled = false; // Disabilita lo script se il componente essenziale manca
            return;
        }

        // Avvia il fade-in
        StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        // Assicurati che l'Alpha iniziale sia 0
        startCanvasGroup.alpha = 0f;

        // Assicurati che il GameObject del CanvasGroup sia attivo.
        // Questo è utile se il GameObject potrebbe iniziare come disattivato.
        if (!startCanvasGroup.gameObject.activeSelf)
        {
            startCanvasGroup.gameObject.SetActive(true);
        }

        // Abilita l'interattività e il blocco dei raycast durante il fade-in se necessario
        // startCanvasGroup.interactable = true; // Decommenta se necessario
        // startCanvasGroup.blocksRaycasts = true; // Decommenta se necessario

        float time = 0f;
        while (time < fadeInDuration)
        {
            time += Time.deltaTime;
            startCanvasGroup.alpha = Mathf.Lerp(0f, 1f, time / fadeInDuration);
            yield return null;
        }
        startCanvasGroup.alpha = 1f; // Assicurati che l'Alpha finale sia 1
    }
}