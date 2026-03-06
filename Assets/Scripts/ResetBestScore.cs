using UnityEngine;

public class ResetBestScore : MonoBehaviour
{
    public void ResetScore()
    {
        PlayerPrefs.SetInt("BestScore", 0);
        PlayerPrefs.Save(); // Assicurati di salvare le modifiche

        // Opzionale: Aggiorna la visualizzazione del punteggio migliore nella schermata iniziale
        StartGameActions startGameActions = FindFirstObjectByType<StartGameActions>();
        if (startGameActions != null && startGameActions.startBestScore != null)
        {
            startGameActions.startBestScore.text = "Best Score: 0";
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
    }
}