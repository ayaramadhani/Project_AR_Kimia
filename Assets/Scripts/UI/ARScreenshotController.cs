
using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARScreenshotController : MonoBehaviour
{
    private const float ReferenceStageWidth = 1920f;
    private const float ReferenceStageHeight = 1080f;

    [Header("UI References")]
    [SerializeField] private GameObject leftSidebar;
    [SerializeField] private GameObject[] additionalHudRoots;
    [SerializeField] private GameObject screenshotPanel;
    [SerializeField] private RectTransform screenshotStage;
    [SerializeField] private RawImage screenshotPreview;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;

    [Header("Save Notification Toast")]
    [SerializeField] private GameObject saveToastRoot;
    [SerializeField] private TMP_Text saveToastText;
    [SerializeField] private float saveToastDuration = 2.5f;

    private Texture2D currentScreenshot;
    private bool isCapturing = false;
    private bool[] previousHudActiveStates;
    private bool previousLeftSidebarActive = true;
    private float saveToastTimer = 0f;
    private volatile string pendingToastMessage;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    public GameObject ScreenshotPanel => screenshotPanel;
    public RectTransform ScreenshotStage => screenshotStage;
    public GameObject SaveToastRoot => saveToastRoot;
    public TMP_Text SaveToastText => saveToastText;

    public void ConfigureReferences(
        GameObject sidebar,
        GameObject[] extraHudRoots,
        GameObject panel,
        RawImage preview,
        AspectRatioFitter fitter,
        RectTransform stage = null,
        GameObject toastRoot = null,
        TMP_Text toastText = null)
    {
        leftSidebar = sidebar;
        additionalHudRoots = extraHudRoots;
        screenshotPanel = panel;
        screenshotPreview = preview;
        aspectRatioFitter = fitter;
        screenshotStage = stage;
        saveToastRoot = toastRoot;
        saveToastText = toastText;
    }

    private void Update()
    {
        if (!string.IsNullOrEmpty(pendingToastMessage))
        {
            string msg = pendingToastMessage;
            pendingToastMessage = null;
            ApplyToastImmediate(msg);
        }

        if (saveToastRoot != null && saveToastRoot.activeSelf)
        {
            saveToastTimer -= Time.unscaledDeltaTime;
            if (saveToastTimer <= 0f)
            {
                HideSaveToastImmediate();
            }
        }
    }

    private void LateUpdate()
    {
        if (screenshotPanel != null && screenshotPanel.activeInHierarchy &&
            (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight))
        {
            FitStageToViewport();
        }
    }

    public void FitStageToViewport()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (screenshotStage == null)
            return;

        RectTransform parentRt = screenshotStage.parent as RectTransform;
        if (parentRt == null || parentRt.rect.width <= 0f || parentRt.rect.height <= 0f)
            return;

        float scale = Mathf.Min(
            parentRt.rect.width / ReferenceStageWidth,
            parentRt.rect.height / ReferenceStageHeight);
        if (scale <= 0f)
            return;

        screenshotStage.localScale = new Vector3(scale, scale, 1f);
    }

    public void ShowSaveToast(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        pendingToastMessage = message;
        try
        {
            ApplyToastImmediate(message);
            pendingToastMessage = null;
        }
        catch
        {
            // Deferred to main-thread Update() when invoked from a native async callback thread.
        }
    }

    public void HideSaveToastImmediate()
    {
        pendingToastMessage = null;
        saveToastTimer = 0f;
        if (saveToastRoot != null)
            saveToastRoot.SetActive(false);
    }

    private void ApplyToastImmediate(string message)
    {
        if (saveToastText != null)
            saveToastText.text = message;

        if (saveToastRoot != null)
        {
            saveToastTimer = saveToastDuration;
            saveToastRoot.SetActive(true);
        }
    }

    // =====================================
    // 1. MENGAMBIL SCREENSHOT
    // =====================================

    public void TakeScreenshot()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogError("ARScreenshotController harus berada pada GameObject aktif untuk menjalankan coroutine screenshot.");
            return;
        }

        if (isCapturing)
            return;

        HideSaveToastImmediate();
        StartCoroutine(CaptureARScreen());
    }

    private IEnumerator CaptureARScreen()
    {
        isCapturing = true;
        try
        {
            // Sembunyikan UI sementara, simpan status aktif tiap panel.
            if (leftSidebar != null)
            {
                previousLeftSidebarActive = leftSidebar.activeSelf;
                leftSidebar.SetActive(false);
            }

            if (additionalHudRoots != null && additionalHudRoots.Length > 0)
            {
                if (previousHudActiveStates == null || previousHudActiveStates.Length != additionalHudRoots.Length)
                    previousHudActiveStates = new bool[additionalHudRoots.Length];

                for (int i = 0; i < additionalHudRoots.Length; i++)
                {
                    GameObject hud = additionalHudRoots[i];
                    previousHudActiveStates[i] = hud != null && hud.activeSelf;
                    if (hud != null)
                        hud.SetActive(false);
                }
            }

            if (screenshotPanel != null)
                screenshotPanel.SetActive(false);

            // Tunggu sampai frame selesai dirender.
            yield return new WaitForEndOfFrame();

            // Ambil screenshot.
            Texture2D newScreenshot =
                ScreenCapture.CaptureScreenshotAsTexture();

            if (screenshotPanel != null)
            {
                screenshotPanel.SetActive(true);
                FitStageToViewport();
            }
            else
            {
                RestoreHudVisibility();
            }

            // Hapus gambar sebelumnya.
            if (currentScreenshot != null)
            {
                Destroy(currentScreenshot);
            }

            currentScreenshot = newScreenshot;

            // Tampilkan screenshot di preview.
            if (screenshotPreview != null)
                screenshotPreview.texture = currentScreenshot;

            // Sesuaikan rasio gambar.
            if (aspectRatioFitter != null && currentScreenshot != null)
            {
                aspectRatioFitter.aspectRatio =
                    (float)currentScreenshot.width /
                    currentScreenshot.height;

                aspectRatioFitter.aspectMode =
                    AspectRatioFitter.AspectMode.FitInParent;
            }
        }
        finally
        {
            isCapturing = false;
        }
    }

    private void RestoreHudVisibility()
    {
        if (leftSidebar != null)
            leftSidebar.SetActive(previousLeftSidebarActive);

        if (additionalHudRoots != null && previousHudActiveStates != null)
        {
            int count = Mathf.Min(additionalHudRoots.Length, previousHudActiveStates.Length);
            for (int i = 0; i < count; i++)
            {
                if (additionalHudRoots[i] != null)
                    additionalHudRoots[i].SetActive(previousHudActiveStates[i]);
            }
        }
    }

    // =====================================
    // 2. DOWNLOAD SCREENSHOT
    // =====================================

    public void DownloadScreenshot()
    {
        if (currentScreenshot == null)
        {
            Debug.LogWarning("Belum ada screenshot.");
            return;
        }

        string fileName = "AR_Kimia_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

        byte[] imageBytes = currentScreenshot.EncodeToPNG();

#if UNITY_EDITOR

        // Simpan ke komputer saat diuji di Unity Editor.
        string folderPath = Path.Combine(
            Application.dataPath, "../ScreenshotTest"
        );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, fileName);

        File.WriteAllBytes(filePath, imageBytes);

        Debug.Log("Screenshot tersimpan di: " + filePath);
        ShowSaveToast("Screenshot berhasil disimpan!");

#elif UNITY_ANDROID || UNITY_IOS

        // Simpan ke galeri Android/iOS.
        NativeGallery.SaveImageToGallery(
            imageBytes,
            "AR_Kimia",
            fileName,
            (success, path) =>
            {
                if (success)
                {
                    Debug.Log("Berhasil disimpan: " + path);
                    ShowSaveToast("Screenshot berhasil disimpan ke galeri!");
                }
                else
                {
                    Debug.LogError("Gagal menyimpan screenshot.");
                    ShowSaveToast("Gagal menyimpan screenshot.");
                }
            }
        );

#else

        string folderPath = Path.Combine(Application.persistentDataPath, "Screenshots");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
        string filePath = Path.Combine(folderPath, fileName);
        File.WriteAllBytes(filePath, imageBytes);
        Debug.Log("Screenshot tersimpan di: " + filePath);
        ShowSaveToast("Screenshot berhasil disimpan!");

#endif
    }

    public void ShareScreenshot()
    {
        if (currentScreenshot == null)
        {
            Debug.LogWarning("Belum ada screenshot untuk dibagikan.");
            return;
        }

        byte[] imageBytes = currentScreenshot.EncodeToPNG();
        string fileName = "AR_Kimia_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string filePath = Path.Combine(
            Application.temporaryCachePath,
            fileName
        );

        File.WriteAllBytes(filePath, imageBytes);

        new NativeShare()
            .AddFile(filePath)
            .SetSubject("Screenshot AR Kimia")
            .SetText("Lihat hasil eksplorasi AR Kimia saya!")
            .Share();
    }

    // =====================================
    // 3. MEMBERSIHKAN MEMORI
    // =====================================

    private void OnDestroy()
    {
        HideSaveToastImmediate();
        RestoreHudVisibility();

        if (screenshotPreview != null)
            screenshotPreview.texture = null;

        if (currentScreenshot != null)
        {
            Destroy(currentScreenshot);
            currentScreenshot = null;
        }
    }

    public void CloseScreenshotPanel()
    {
        HideSaveToastImmediate();

        if (screenshotPanel != null)
            screenshotPanel.SetActive(false);

        RestoreHudVisibility();

        Debug.Log("Kembali ke halaman AR Camera.");
    }

}
