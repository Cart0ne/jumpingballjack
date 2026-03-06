using UnityEngine;
using System;
using System.Collections;

public class FanController : MonoBehaviour
{
    public float rotationSpeed = 180f;
    public float duration = 1f;
    public float moveSpeed = 2f;
    public float verticalDistance = 20f;
    public float pauseAtTargetDuration = 1f;  // Nuovo: Durata della pausa in cima

    private Vector3 startMovePosition;
    private Vector3 endMovePosition;
    private Vector3 hiddenPosition;
    private bool movingUp = true;
    private float timer = 0f;
    private bool fadingOut = false;
    private float particlesAlpha = 1f;
    public float particlesFadeOutDuration = 0.5f;
    private bool pausedAtTarget = false;  // Nuovo: Flag per indicare se siamo in pausa
    private float pauseTimer = 0f;      // Nuovo: Timer per la pausa

    public event Action OnFanReachedTarget;

    [Header("Audio")]
    public AudioSource fanAudioSourcePrefab;
    private AudioSource currentAudioSource;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    [Header("Particles")]
    public ParticleSystem dustParticlesPrefab;
    private ParticleSystem currentDustParticles;
    public Vector3 dustParticlesOffset = Vector3.up * 0.5f;

    void Start() { }

    public void SetTargetPosition(Vector3 platformPosition, float offset)
    {
        startMovePosition = platformPosition - Vector3.up * verticalDistance;
        endMovePosition = platformPosition - Vector3.up * offset;
        hiddenPosition = startMovePosition;
        transform.position = startMovePosition;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        if (fanAudioSourcePrefab != null && SoundManager.soundEnabled)
        {
            currentAudioSource = Instantiate(fanAudioSourcePrefab, transform);
            currentAudioSource.volume = 0f;
            currentAudioSource.loop = true;
            currentAudioSource.Play();
            StartCoroutine(FadeInAudio());
        }

        if (dustParticlesPrefab != null)
        {
            Vector3 dustPosition = transform.position + dustParticlesOffset;
            currentDustParticles = Instantiate(dustParticlesPrefab, dustPosition, Quaternion.identity);
            currentDustParticles.Play();
        }
    }

    private IEnumerator FadeInAudio()
    {
        if (currentAudioSource == null) yield break;

        float timeElapsed = 0f;
        while (timeElapsed < fadeInDuration)
        {
            currentAudioSource.volume = Mathf.Lerp(0f, 1f, timeElapsed / fadeInDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        currentAudioSource.volume = 1f;
    }

    public void StartFanSequence()
    {
        movingUp = true;
        timer = 0f;
        fadingOut = false;
        particlesAlpha = 1f;
        pausedAtTarget = false;  // Assicurati che non sia in pausa quando inizia
        pauseTimer = 0f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        if (currentDustParticles != null)
        {
            currentDustParticles.transform.position = transform.position + dustParticlesOffset;

            if (fadingOut)
            {
                particlesAlpha -= Time.deltaTime / particlesFadeOutDuration;
                particlesAlpha = Mathf.Clamp01(particlesAlpha);

                var mainModule = currentDustParticles.main;
                Color startColor = mainModule.startColor.color;
                startColor.a = particlesAlpha;
                mainModule.startColor = startColor;
            }
        }

        if (pausedAtTarget)
        {
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= pauseAtTargetDuration)
            {
                pausedAtTarget = false;
                timer = 0f;  // Reset timer per la discesa
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            }
            return;  // Non muoverti mentre sei in pausa
        }

        if (movingUp)
        {
            transform.position = Vector3.MoveTowards(transform.position, endMovePosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, endMovePosition) < 0.01f)
            {
                movingUp = false;
                pausedAtTarget = true;  // Inizia la pausa
                pauseTimer = 0f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
                OnFanReachedTarget?.Invoke();
            }
        }
        else if (timer < duration)
        {
            timer += Time.deltaTime;
        }
        else if (!fadingOut)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
            StartCoroutine(FadeOutAndDestroyAudio());
            fadingOut = true;
        }
    }

    private IEnumerator FadeOutAndDestroyAudio()
    {
        if (currentAudioSource == null)
        {
            DestroyFanAndParticles();
            yield break;
        }

        fadingOut = true;
        currentAudioSource.loop = false;

        float startVolume = currentAudioSource.volume;
        float timeElapsed = 0f;

        while (timeElapsed < fadeOutDuration)
        {
            float t = timeElapsed / fadeOutDuration;
            float volume = startVolume * Mathf.Pow(1 - t, 6);
            currentAudioSource.volume = volume;

            transform.position = Vector3.MoveTowards(transform.position, hiddenPosition, moveSpeed * Time.deltaTime * (fadeOutDuration / (fadeOutDuration + 0.1f)));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        currentAudioSource.volume = 0f;
        currentAudioSource.Stop();
        Destroy(currentAudioSource.gameObject);
        currentAudioSource = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

        DestroyFanAndParticles();
    }

    private void DestroyFanAndParticles()
    {
        if (currentDustParticles != null)
        {
            currentDustParticles.Stop();
            Destroy(currentDustParticles.gameObject);
            currentDustParticles = null;
        }
        Destroy(gameObject);

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }

    void OnDestroy()
    {
        if (currentAudioSource != null)
        {
            if (currentAudioSource.isPlaying)
            {
                currentAudioSource.Stop();
            }
            Destroy(currentAudioSource.gameObject);
        }

        if (currentDustParticles != null)
        {
            currentDustParticles.Stop();
            Destroy(currentDustParticles.gameObject);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }
}