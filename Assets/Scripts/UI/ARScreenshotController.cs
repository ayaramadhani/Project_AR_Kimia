
using System.Collections;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ARScreenshotController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject leftSidebar;
    [SerializeField] private GameObject screenshotPanel;
    [SerializeField] private RawImage screenshotPreview;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;

    private Texture2D currentScreenshot;
    private bool isCapturing = false;

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

        // Sembunyikan UI sementara.
        leftSidebar.SetActive(false);
        screenshotPanel.SetActive(false);

        // Tunggu sampai frame selesai dirender.
        yield return new WaitForEndOfFrame();

        // Ambil screenshot.
        Texture2D newScreenshot =
            ScreenCapture.CaptureScreenshotAsTexture();

        // Tampilkan kembali UI.
        leftSidebar.SetActive(true);
        screenshotPanel.SetActive(true);

        // Hapus gambar sebelumnya.
        if (currentScreenshot != null)
        {
            Destroy(currentScreenshot);
        }

        currentScreenshot = newScreenshot;

        // Tampilkan screenshot di preview.
        screenshotPreview.texture = currentScreenshot;

        // Sesuaikan rasio gambar.
        if (aspectRatioFitter != null)
        {
            aspectRatioFitter.aspectRatio =
                (float)currentScreenshot.width /
                currentScreenshot.height;

            aspectRatioFitter.aspectMode =
                AspectRatioFitter.AspectMode.FitInParent;
        }

        isCapturing = false;
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
    screenshotPanel.SetActive(false);

    // Menampilkan kembali sidebar AR
    leftSidebar.SetActive(true);

    Debug.Log("Kembali ke halaman AR Camera.");
}

}
