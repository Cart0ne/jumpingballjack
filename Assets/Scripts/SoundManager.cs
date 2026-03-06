using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static bool soundEnabled = true;
    public AudioSource backgroundMusicSource;
    private const string SoundEnabledKey = "SoundEnabled";
    private const string MusicEnabledKey = "MusicEnabled";
    public static bool musicEnabled = true;

    private static SoundManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            soundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
            musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            UpdateEffectsSound();
            UpdateBackgroundMusic();

            if (backgroundMusicSource != null && musicEnabled && !backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Play();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateEffectsSound();
    }

    public static void EnableSoundEffects(bool enable)
    {
        soundEnabled = enable;
        UpdateEffectsSound();
        PlayerPrefs.SetInt(SoundEnabledKey, soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        // DEBUG RIMOSSO: Debug.Log("Effetti sonori " + (soundEnabled ? "abilitati" : "disabilitati"));
    }

    private static void UpdateEffectsSound()
    {
        if (instance == null) return;

        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource audioSource in allAudioSources)
        {
            if (audioSource == instance.backgroundMusicSource)
                continue;
            audioSource.mute = !soundEnabled;
        }
    }

    public void EnableBackgroundMusic(bool enable) // Metodo di istanza
    {
        musicEnabled = enable; // Modifica variabile statica
        UpdateBackgroundMusic(); // Chiama metodo di istanza
        PlayerPrefs.SetInt(MusicEnabledKey, musicEnabled ? 1 : 0);
        PlayerPrefs.Save();
        // DEBUG RIMOSSO: Debug.Log("Musica " + (musicEnabled ? "abilitata" : "disabilitata"));

        if (backgroundMusicSource != null && musicEnabled && !backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Play();
        }
    }

    private void UpdateBackgroundMusic() // Metodo di istanza
    {
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.mute = !musicEnabled;
            if (musicEnabled && !backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Play();
            }
            else if (!musicEnabled && backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Pause();
            }
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("Riferimento all'AudioSource della colonna sonora non assegnato nello SoundManager!");
#endif
        }
    }

    public void OnSoundEffectsToggleValueChanged(bool isOn)
    {
        EnableSoundEffects(isOn);
    }

    public void OnBackgroundMusicToggleValueChanged(bool isOn)
    {
        EnableBackgroundMusic(isOn); // Chiama il metodo di istanza
    }
}