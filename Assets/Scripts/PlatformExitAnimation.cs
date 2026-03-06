using System.Collections;
using UnityEngine;

public class PlatformExitAnimation : MonoBehaviour
{
        [Header("Animation Settings")]
        [Tooltip("Durata della fase di carica (in secondi)")]
        public float chargeDuration = 0.5f;
        [Tooltip("Durata della fase razzo (in secondi)")]
        public float rocketDuration = 0.5f;
        [Tooltip("Distanza verso il basso che la piattaforma raggiunge durante l'affondamento")]
        public float sinkDistance = 20f;
        [Tooltip("Moltiplicatore di scala massimo durante la carica (es. 1.2 significa un aumento del 20%)")]
        public float chargeScaleMultiplier = 1.2f;

        /// <summary>
        /// Avvia l'animazione di uscita con affondamento e, al termine, distrugge il GameObject.
        /// </summary>
        public void AnimateExit()
        {
                StartCoroutine(AnimateSinkExit());
        }

        private IEnumerator AnimateSinkExit()
        {
                // Fase 1: Carica
                Vector3 initialScale = transform.localScale;
                float elapsed = 0f;
                float halfDuration = chargeDuration * 0.5f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

                while (elapsed < chargeDuration)
                {
                        elapsed += Time.deltaTime;
                        if (elapsed <= halfDuration)
                        {
                                // Crescita
                                float t = elapsed / halfDuration;
                                transform.localScale = Vector3.Lerp(initialScale, initialScale * chargeScaleMultiplier, t);
                        }
                        else
                        {
                                // Ritorno alla scala iniziale
                                float t = (elapsed - halfDuration) / halfDuration;
                                transform.localScale = Vector3.Lerp(initialScale * chargeScaleMultiplier, initialScale, t);
                        }
                        yield return null;
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

                // Fase 2: Affondamento
                elapsed = 0f;
                Vector3 initialPosition = transform.position;
                Vector3 targetPosition = initialPosition + Vector3.down * sinkDistance; // Cambio direzione

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

                while (elapsed < rocketDuration)
                {
                        elapsed += Time.deltaTime;
                        float t = Mathf.SmoothStep(0f, 1f, elapsed / rocketDuration);
                        transform.position = Vector3.Lerp(initialPosition, targetPosition, t);
                        transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
                        yield return null;
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif

                transform.localScale = Vector3.zero;
                //Destroy(gameObject);
                PlatformAudioFader audioFader = GetComponent<PlatformAudioFader>();
                if (audioFader != null)
                {
                        StartCoroutine(audioFader.FadeOutAndDestroy(gameObject));
                }
                else
                {
                        Destroy(gameObject); // fallback se non troviamo lo script audio
                }
        }
}
