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

    [Header("Warmup Settings")]
    [Tooltip("Frame di warmup per prefab semplici (senza Animator/ParticleSystem)")]
    public int simpleWarmupFrames = 2;
    [Tooltip("Frame di warmup per prefab complessi (con Animator o ParticleSystem)")]
    public int complexWarmupFrames = 5;

    private IEnumerator WarmupPrefabs()
    {
        foreach (GameObject prefab in prefabsToWarmup)
        {
            if (prefab == null) continue;

            GameObject instance = Instantiate(prefab, warmupPosition, Quaternion.identity);
            tempInstances.Add(instance);

            DisableUnnecessaryComponents(instance);

            // Determina quanti frame servono in base alla complessita del prefab
            bool isComplex = instance.GetComponentInChildren<Animator>(true) != null
                          || instance.GetComponentInChildren<ParticleSystem>(true) != null;
            int framesToWait = isComplex ? complexWarmupFrames : simpleWarmupFrames;

            for (int i = 0; i < framesToWait; i++)
                yield return null;
        }

        yield return null;

        foreach (GameObject go in tempInstances)
        {
            if (go != null)
                Destroy(go);
        }

        tempInstances.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Warmup completato con " + prefabsToWarmup.Count + " prefab.");
#endif
    }

    private void DisableUnnecessaryComponents(GameObject instance)
    {
        // Disabilita audio per evitare suoni durante il warmup
        foreach (AudioSource audio in instance.GetComponentsInChildren<AudioSource>(true))
            audio.enabled = false;

        // Disabilita collider per evitare collisioni
        foreach (Collider col in instance.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        // Disabilita rigidbody per evitare fisica
        foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>(true))
            rb.isKinematic = true;

        // Disabilita solo script custom, lascia Animator e ParticleSystem attivi per il warmup
        foreach (MonoBehaviour script in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (script != null)
                script.enabled = false;
        }
    }
}
