using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioMixerSettings : MonoBehaviour
{
    public AudioMixer audioMixer;

    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider musicSlider;

    const string MASTER = "MasterVolume";
    const string SFX = "SFXVolume";
    const string MUSIC = "MusicVolume";

    void Start()
    {
        if (!PlayerPrefs.HasKey(MASTER))
        {
            InitializeVolumeSettings();
        }
        masterSlider.value = PlayerPrefs.GetFloat(MASTER);
        sfxSlider.value = PlayerPrefs.GetFloat(SFX);
        musicSlider.value = PlayerPrefs.GetFloat(MUSIC);

        SetMasterVolume(masterSlider.value);
        SetSFXVolume(sfxSlider.value);
        SetMusicVolume(musicSlider.value);
        
    }

    public void SetMasterVolume(float value)
    {
        SetVolume(MASTER, value);
        PlayerPrefs.SetFloat(MASTER, value);
    }

    public void SetSFXVolume(float value)
    {
        SetVolume(SFX, value);
        PlayerPrefs.SetFloat(SFX, value);
    }

    public void SetMusicVolume(float value)
    {
        SetVolume(MUSIC, value);
        PlayerPrefs.SetFloat(MUSIC, value);
    }

    void SetVolume(string name, float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        float dB = Mathf.Log10(value) * 20f;
        audioMixer.SetFloat(name, dB);
    }

    void InitializeVolumeSettings()
    {
        PlayerPrefs.SetFloat(MASTER, 1);
        PlayerPrefs.SetFloat(SFX, 1);
        PlayerPrefs.SetFloat(MUSIC, 1);
    }
}