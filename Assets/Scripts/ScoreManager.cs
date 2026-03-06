using UnityEngine;
using TMPro; // Necessario per TextMeshProUGUI

public class ScoreManager : MonoBehaviour
{
    public TMP_Text scoreText;
    public TMP_Text bestScoreText; // Mostrato nella schermata Game Over

    private int currentScore = 0;
    private int bestScore = 0;

    [Header("Score Audio Settings")]
    public AudioSource scoreAudioSource; // AudioSource dedicato per questi suoni

    // --- AudioClip aggiornati con Tooltip ---
    [Tooltip("Suono per +1 punto (es. Planet Normal Hit)")]
    public AudioClip scoreSoundPlus1;
    [Tooltip("Suono per +2 punti (es. Planet Center Hit 1)")]
    public AudioClip scoreSoundPlus2;
    [Tooltip("Suono per +4 punti (es. Planet Center Hit 2)")]
    public AudioClip scoreSoundPlus4;
    // --- NUOVO +5 ---
    [Tooltip("Suono per +5 punti (es. Platform Normal Hit)")]
    public AudioClip scoreSoundPlus5;
    [Tooltip("Suono per +6 punti (es. Planet Center Hit 3)")]
    public AudioClip scoreSoundPlus6;
    [Tooltip("Suono per +8 punti (es. Planet Center Hit 4)")]
    public AudioClip scoreSoundPlus8;
    // --- NUOVO +10 ---
    [Tooltip("Suono per +10 punti (es. Platform Center Hit)")]
    public AudioClip scoreSoundPlus10;
    // Potresti aggiungere casi per 12, 18, 24 se il moltiplicatore Planet può generare questi valori
    // public AudioClip scoreSoundPlus12;
    // public AudioClip scoreSoundPlus18;
    // public AudioClip scoreSoundPlus24;
    [Tooltip("Suono per +30 punti (es. Planet Center Hit 5 x3 Multiplier - assumendo 10*3)")]
    public AudioClip scoreSoundPlus30;

    [Range(0f, 1f)] public float scoreSoundVolume = 1f; // Volume per questi suoni

    void Start()
    {
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        UpdateScoreUI();
        UpdateBestScoreUI();
    }

    public void AddScore(int points)
    {
        if (points <= 0) return;

        currentScore += points;

        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
            UpdateBestScoreUI();
        }

        UpdateScoreUI();
        PlayScoreSound(points); // Chiama il metodo aggiornato
    }

    private void PlayScoreSound(int points)
    {
        if (scoreAudioSource == null || !SoundManager.soundEnabled) return;

        AudioClip clipToPlay = null;

        // --- Switch aggiornato ---
        switch (points)
        {
            case 1: clipToPlay = scoreSoundPlus1; break;
            case 2: clipToPlay = scoreSoundPlus2; break;
            case 4: clipToPlay = scoreSoundPlus4; break;
            case 5: clipToPlay = scoreSoundPlus5; break; // Gestisce +5 punti (Platform normale)
            case 6: clipToPlay = scoreSoundPlus6; break;
            case 8: clipToPlay = scoreSoundPlus8; break;
            case 10: clipToPlay = scoreSoundPlus10; break; // Gestisce +10 punti (Platform centro)
            // Aggiungi qui altri casi se necessario per i moltiplicatori Planet (12, 18, 24?)
            // case 12: clipToPlay = scoreSoundPlus12; break;
            // case 18: clipToPlay = scoreSoundPlus18; break;
            // case 24: clipToPlay = scoreSoundPlus24; break;
            case 30: clipToPlay = scoreSoundPlus30; break; // Gestisce +30 (es. 10*3)
            default:
                // Se i punti non corrispondono a un caso specifico, non riprodurre suono.
                // Potresti aggiungere un suono generico qui se vuoi.
                // Debug.Log($"ScoreManager: Nessun suono specifico per {points} punti.");
                break;
        }

        // Riproduci il clip selezionato se è stato assegnato
        if (clipToPlay != null)
        {
            scoreAudioSource.PlayOneShot(clipToPlay, scoreSoundVolume);
        }
        // else Debug.LogWarning($"ScoreManager: AudioClip non assegnato per {points} punti.", this); // Log opzionale se vuoi sapere quando manca un clip
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "+ " + currentScore.ToString();
        }
    }

    private void UpdateBestScoreUI()
    {
        if (bestScoreText != null)
        {
            bestScoreText.text = "Best: " + bestScore;
        }
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public int GetBestScore()
    {
        return bestScore;
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
    }
}