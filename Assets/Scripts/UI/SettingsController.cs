
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsController : MonoBehaviour
{
    [Header("Settings Panel")]
    public GameObject settingsPanel;

    [Header("Volume Sliders")]
    public Slider musicSlider;
    public Slider soundSlider;

    [Header("Volume Text")]
    public TMP_Text musicValueText;
    public TMP_Text soundValueText;

    private void Start()
    {
        // Membaca volume yang tersimpan.
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 100f);
        float savedSound = PlayerPrefs.GetFloat("SoundVolume", 100f);

        // Memasang nilai awal slider.
        musicSlider.value = savedMusic;
        soundSlider.value = savedSound;

        // Memperbarui angka yang ditampilkan.
        UpdateMusicValue(savedMusic);
        UpdateSoundValue(savedSound);

        // Panel Settings disembunyikan saat game dimulai.
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void UpdateMusicValue(float value)
    {
        int volume = Mathf.RoundToInt(value);

        musicValueText.text = volume.ToString();

        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    public void UpdateSoundValue(float value)
    {
        int volume = Mathf.RoundToInt(value);

        soundValueText.text = volume.ToString();

        PlayerPrefs.SetFloat("SoundVolume", value);
        PlayerPrefs.Save();
    }
}
