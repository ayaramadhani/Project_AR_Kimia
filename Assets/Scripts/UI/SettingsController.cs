
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsController : MonoBehaviour
{
    [Header("Settings Panel")]
    public GameObject settingsPanel;

    [Header("Dim Backdrop")]
    public GameObject settingsBackdrop;

    [Header("Volume Sliders")]
    public Slider musicSlider;
    public Slider soundSlider;

    [Header("Volume Text")]
    public TMP_Text musicValueText;
    public TMP_Text soundValueText;

    private const float NominalPanelWidth = 1860f;
    private const float NominalPanelHeight = 1060f;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

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

        FitSettingsPanelToViewport();

        // Panel Settings & backdrop disembunyikan saat game dimulai.
        if (settingsBackdrop != null)
            settingsBackdrop.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private void LateUpdate()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            FitSettingsPanelToViewport();
        }
    }

    public void FitSettingsPanelToViewport()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (settingsPanel == null)
            return;

        RectTransform panelRt = settingsPanel.GetComponent<RectTransform>();
        if (panelRt == null)
            return;

        RectTransform parentRt = panelRt.parent as RectTransform;
        if (parentRt == null || parentRt.rect.width <= 0f || parentRt.rect.height <= 0f)
            return;

        float widthScale = (parentRt.rect.width * 0.88f) / NominalPanelWidth;
        float heightScale = (parentRt.rect.height * 0.86f) / NominalPanelHeight;
        float uniformScale = Mathf.Clamp(Mathf.Min(1f, Mathf.Min(widthScale, heightScale)), 0.35f, 1f);
        panelRt.localScale = new Vector3(uniformScale, uniformScale, 1f);
    }

    public void OpenSettings()
    {
        if (settingsBackdrop != null)
            settingsBackdrop.SetActive(true);
        FitSettingsPanelToViewport();
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        if (settingsBackdrop != null)
            settingsBackdrop.SetActive(false);
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
