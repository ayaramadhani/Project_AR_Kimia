
using System.Collections;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ARScreenshotController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject leftSidebar;
    [SerializeField] private GameObject[] additionalHudRoots;
    [SerializeField] private GameObject screenshotPanel;
    [SerializeField] private RawImage screenshotPreview;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;

    private Texture2D currentScreenshot;
    private bool isCapturing = false;
    private bool[] previousHudActiveStates;
    private bool previousLeftSidebarActive = true;

    public GameObject ScreenshotPanel => screenshotPanel;

    public void ConfigureReferences(
        GameObject sidebar,
        GameObject[] extraHudRoots,
        GameObject panel,
        RawImage preview,
        AspectRatioFitter fitter)
    {
        leftSidebar = sidebar;
        additionalHudRoots = extraHudRoots;
        screenshotPanel = panel;
        screenshotPreview = preview;
        aspectRatioFitter = fitter;
    }

    // =====================================
    // 1. MENGAMBIL SCREENSHOT
    // =====================================

    public void TakeScreenshot()
    {
        if (isCapturing)
            return;

        StartCoroutine(CaptureARScreen());
    }

    private IEnumerator CaptureARScreen()
    {
        isCapturing = true;

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

        RestoreHudVisibility();
        if (screenshotPanel != null)
            screenshotPanel.SetActive(true);

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

        isCapturing = false;
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

#elif UNITY_ANDROID

        // Simpan ke galeri Android.
        NativeGallery.SaveImageToGallery(
            imageBytes,
            "AR_Kimia",
            fileName,
            (success, path) =>
            {
                if (success)
                {
                    Debug.Log("Berhasil disimpan: " + path);
                }
                else
                {
                    Debug.LogError("Gagal menyimpan screenshot.");
                }
            }
        );

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
        if (currentScreenshot != null)
        {
            Destroy(currentScreenshot);
        }
    }

public void CloseScreenshotPanel()
{
    // Menutup halaman screenshot
    if (screenshotPanel != null)
        screenshotPanel.SetActive(false);

    // Menampilkan kembali sidebar AR bila sebelumnya disembunyikan
    if (leftSidebar != null)
        leftSidebar.SetActive(true);

    Debug.Log("Kembali ke halaman AR Camera.");
}

}
