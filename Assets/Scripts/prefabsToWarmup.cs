using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderPreloader : MonoBehaviour
{
    [Header("Prefabs da Precaricare")]
    public List<GameObject> prefabsToWarmup;

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

            GameObject instance = Instantiate(prefab);
            instance.SetActive(false); // invisibile in scena
            tempInstances.Add(instance);

            yield return null; // aspetto un frame per permettere la compilazione shader
        }

        yield return new WaitForSeconds(0.5f); // tempo extra di sicurezza

        foreach (GameObject go in tempInstances)
        {
            if (go != null)
                Destroy(go);
        }

        tempInstances.Clear();

        Debug.Log("✅ Shader preload completato con " + prefabsToWarmup.Count + " prefab scansionati.");
    }
}
