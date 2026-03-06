using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class PlatformAudioFader : MonoBehaviour
{
    public float fadeInDuration = 2f;
    public float fadeOutDuration = 2f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;
    private float originalVolume = 1f; // Volume massimo desiderato

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            originalVolume = audioSource.volume; // 🔥 Usa il volume impostato da Inspector
            audioSource.volume = 0f;
            audioSource.loop = true;
            audioSource.Play();
            fadeCoroutine = StartCoroutine(FadeAudio(0f, originalVolume, fadeInDuration));
        }
    }

    void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeAudio(audioSource.volume, 0f, fadeOutDuration, stopAfterFade: true));
        }
    }

    IEnumerator FadeAudio(float from, float to, float duration, bool stopAfterFade = false)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        audioSource.volume = to;

        if (stopAfterFade)
        {
            audioSource.Stop();
        }
    }

    public IEnumerator FadeOutAndDestroy(GameObject objectToDestroy)
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeAudio(audioSource.volume, 0f, fadeOutDuration, stopAfterFade: true));
            yield return new WaitForSeconds(fadeOutDuration);
        }

        if (objectToDestroy != null)
        {
            Destroy(objectToDestroy);
        }
    }
}
