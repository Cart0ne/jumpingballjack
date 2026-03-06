using UnityEngine;
using TMPro;

public class FloatingScoreController : MonoBehaviour
{
    public TextMeshPro scoreText; // Il testo da aggiornare
    public float floatSpeed = 1f; // Velocità con cui sale
    public float duration = 1.5f; // Tempo prima di autodistruggersi

    public void Initialize(int score, Vector3 spawnPosition)
    {
        scoreText.text = $"+{score}";   
        transform.position = spawnPosition; // Posiziona sopra la piattaforma
/*
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
*/
        Destroy(gameObject, duration); // Distrugge l'oggetto dopo un po'
    }

    void Update()
    {
        // Mantiene il testo rivolto verso la telecamera principale
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0); // Corregge l'inversione del testo

        // Movimento verso l'alto
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
/*
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
*/
    }
}
