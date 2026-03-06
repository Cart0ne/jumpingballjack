using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderPreloader : MonoBehaviour
{
    [Header("Prefabs da Precaricare")]
    public List<GameObject> prefabsToWarmup;

    [Header("Posizione fuori campo per il warmup")]
    public Vector3 warmupPosition = new Vector3(0f, -500f, 0f);

    private List<GameObject> tempInstances = new List<GameObject>();

    void Start()
    {
        StartCoroutine(WarmupPrefabs());
    }

    private IEnumerator WarmupPrefabs()
    {
        foreach (GameObject prefab in prefabsToWarmup)
        {
            if (prefab == null) continue;

            // Istanzia ATTIVO ma fuori campo cosi la GPU compila gli shader
            GameObject instance = Instantiate(prefab, warmupPosition, Quaternion.identity);
            tempInstances.Add(instance);

            // Disabilita componenti che non servono durante il warmup
            DisableUnnecessaryComponents(instance);

            // Aspetta 1 frame: il renderer e attivo, la GPU compila le shader variant
            yield return null;
        }

        // Frame extra per i Particle System che potrebbero avere emissione ritardata
        yield return null;
        yield return null;

        foreach (GameObject go in tempInstances)
        {
            if (go != null)
                Destroy(go);
        }

        tempInstances.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Shader preload completato con " + prefabsToWarmup.Count + " prefab.");
#endif
    }

    private void DisableUnnecessaryComponents(GameObject instance)
    {
        // Disabilita audio per evitare suoni durante il warmup
        foreach (AudioSource audio in instance.GetComponentsInChildren<AudioSource>(true))
        {
            audio.enabled = false;
        }

        // Disabilita collider per evitare collisioni
        foreach (Collider col in instance.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        // Disabilita rigidbody per evitare fisica
        foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
        }

        // Disabilita script custom per evitare logica di gioco
        foreach (MonoBehaviour script in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (script != null && !(script is ParticleSystem))
                script.enabled = false;
        }
    }
}
