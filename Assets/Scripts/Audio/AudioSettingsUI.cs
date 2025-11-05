using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for audio settings.
/// Attach to your Settings panel.
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [Header("Volume Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Volume Labels (Optional)")]
    public TextMeshProUGUI masterVolumeLabel;
    public TextMeshProUGUI musicVolumeLabel;
    public TextMeshProUGUI sfxVolumeLabel;

    [Header("Test SFX")]
    public SFXLibrary sfxLibrary;

    void Start()
    {
        SetupSliders();
        LoadCurrentVolumes();
    }

    void SetupSliders()
    {
        // Configure sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    void LoadCurrentVolumes()
    {
        if (AudioManager.Instance == null) return;

        // Load saved volumes into sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
            UpdateVolumeLabel(masterVolumeLabel, masterVolumeSlider.value);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
            UpdateVolumeLabel(musicVolumeLabel, musicVolumeSlider.value);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
            UpdateVolumeLabel(sfxVolumeLabel, sfxVolumeSlider.value);
        }
    }

    void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
        UpdateVolumeLabel(masterVolumeLabel, value);
    }

    void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
        UpdateVolumeLabel(musicVolumeLabel, value);
    }

    void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
            
            // Play test sound when adjusting SFX volume
            if (sfxLibrary != null && sfxLibrary.uiSelect != null)
            {
                AudioManager.Instance.PlaySFX(sfxLibrary.uiSelect);
            }
        }
        UpdateVolumeLabel(sfxVolumeLabel, value);
    }

    void UpdateVolumeLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    /// <summary>
    /// Reset all volumes to default
    /// </summary>
    public void ResetToDefaults()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.SetMasterVolume(AudioManager.Instance.defaultMasterVolume);
        AudioManager.Instance.SetMusicVolume(AudioManager.Instance.defaultMusicVolume);
        AudioManager.Instance.SetSFXVolume(AudioManager.Instance.defaultSFXVolume);

        LoadCurrentVolumes();
    }
}