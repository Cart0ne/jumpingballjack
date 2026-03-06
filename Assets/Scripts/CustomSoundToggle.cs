using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomSoundToggle : MonoBehaviour, IPointerDownHandler
{
    public Image leverOnImage;
    public Image leverOffImage;
    public Image iconSoundOnImage;
    public Image iconSoundOffImage;

    private bool soundEnabled;

    void OnEnable() // Modificato da Start a OnEnable
    {
        // Ottieni lo stato CORRENTE del suono da SoundManager
        soundEnabled = SoundManager.soundEnabled;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Inverti lo stato del suono
        soundEnabled = !soundEnabled;
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        UpdateVisuals();
        SoundManager.EnableSoundEffects(soundEnabled); // Aggiorna lo stato degli effetti sonori tramite SoundManager
    }

    private void UpdateVisuals()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        if (soundEnabled)
        {
            leverOnImage.enabled = true;
            leverOffImage.enabled = false;
            iconSoundOnImage.enabled = true;
            iconSoundOffImage.enabled = false;
        }
        else
        {
            leverOnImage.enabled = false;
            leverOffImage.enabled = true;
            iconSoundOnImage.enabled = false;
            iconSoundOffImage.enabled = true;
        }
    }
}