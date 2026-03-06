using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomMusicToggle : MonoBehaviour, IPointerDownHandler
{
    public Image leverOnImage;
    public Image leverOffImage;
    public Image iconMusicOnImage;
    public Image iconMusicOffImage;

    private bool musicEnabled;

    void Start()
    {
        // Ottieni lo stato iniziale della musica da SoundManager
        musicEnabled = SoundManager.musicEnabled;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Inverti lo stato della musica
        musicEnabled = !musicEnabled;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        UpdateVisuals();
        // Trova l'istanza di SoundManager e chiama il metodo per la musica
        SoundManager soundManager = FindFirstObjectByType<SoundManager>();
        if (soundManager != null)
        {
            soundManager.EnableBackgroundMusic(musicEnabled);
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        }
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("CustomMusicToggle: SoundManager non trovato nella scena!");
#endif
        }
    }

    private void UpdateVisuals()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        if (musicEnabled)
        {
            leverOnImage.enabled = true;
            leverOffImage.enabled = false;
            iconMusicOnImage.enabled = true;
            iconMusicOffImage.enabled = false;
        }
        else
        {
            leverOnImage.enabled = false;
            leverOffImage.enabled = true;
            iconMusicOnImage.enabled = false;
            iconMusicOffImage.enabled = true;
        }
    }
}