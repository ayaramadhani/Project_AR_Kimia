
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ARScreenshotController : MonoBehaviour
{
    [Header("UI yang Disembunyikan Saat Screenshot")]
    [SerializeField] private GameObject[] uiToHide;

    [Header("Screenshot Preview")]
    [SerializeField] private GameObject screenshotPanel;
    [SerializeField] private RawImage screenshotPreview;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;

    [Header("Kamera AR")]
    [SerializeField] private Camera arCamera;

    [Header("UI yang Disembunyikan Selama Panel Screenshot Terbuka")]
    [SerializeField] private GameObject[] uiHiddenWhilePanelOpen;

    private Texture2D currentScreenshot;
    private bool isCapturing = false;

    private bool[] panelHiddenStates;
    private bool isPanelUIHidden = false;

    // =====================================
    // 1. MENGAMBIL SCREENSHOT
    // =====================================

    public void TakeScreenshot()
    {
        if (isCapturing)
            return;

        if (screenshotPanel == null)
        {
            Debug.LogError("Screenshot Panel belum diatur!");
            return;
        }

        if (screenshotPreview == null)
        {
            Debug.LogError("Screenshot Preview belum diatur!");
            return;
        }

        StartCoroutine(CaptureARScreen());
    }

    private IEnumerator CaptureARScreen()
    {
        isCapturing = true;

        int uiCount = uiToHide != null ? uiToHide.Length : 0;
        bool[] previousStates = new bool[uiCount];

        Texture2D newScreenshot = null;

        try
        {
            // Simpan kondisi awal dan sembunyikan UI.
            for (int i = 0; i < uiCount; i++)
            {
                if (uiToHide[i] != null)
                {
                    previousStates[i] = uiToHide[i].activeSelf;
                    uiToHide[i].SetActive(false);
                }
            }

            // Sembunyikan panel screenshot.
            screenshotPanel.SetActive(false);

            // Perbarui Canvas.
            Canvas.ForceUpdateCanvases();

            // Tunggu sampai rendering selesai.
            yield return new WaitForEndOfFrame();

            int w = Screen.width;
            int h = Screen.height;

            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevTarget = arCamera.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            arCamera.targetTexture = rt;
            arCamera.Render();

            RenderTexture.active = rt;
            newScreenshot = new Texture2D(w, h, TextureFormat.RGB24, false);
            newScreenshot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            newScreenshot.Apply();

            arCamera.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            Destroy(rt);

            // =====================================
            // DEBUG HASIL SCREENSHOT
            // =====================================

            if (newScreenshot != null)
            {
                Debug.Log("Screenshot berhasil dibuat.");

                Debug.Log("Resolusi: " +
                    newScreenshot.width + " x " +
                    newScreenshot.height);

                Color centerPixel = newScreenshot.GetPixel(
                    newScreenshot.width / 2,
                    newScreenshot.height / 2
                );

                Debug.Log("Warna pixel tengah: " + centerPixel);
                Debug.Log("Alpha pixel tengah: " + centerPixel.a);

                Color[] pixels = newScreenshot.GetPixels();

                int visiblePixels = 0;

                foreach (Color pixel in pixels)
                {
                    if (pixel.a > 0.01f)
                    {
                        visiblePixels++;
                    }
                }

                Debug.Log("Total pixel: " + pixels.Length);
                Debug.Log("Pixel tidak transparan: " + visiblePixels);
                Debug.Log("Format Texture: " + newScreenshot.format);
            }
            else
            {
                Debug.LogError("Texture screenshot NULL!");
            }
        }
        finally
        {
            // Kembalikan kondisi UI seperti semula.
            for (int i = 0; i < uiCount; i++)
            {
                if (uiToHide[i] != null)
                {
                    uiToHide[i].SetActive(previousStates[i]);
                }
            }

            isCapturing = false;
        }

        // Periksa apakah screenshot berhasil dibuat.
        if (newScreenshot == null)
        {
            Debug.LogError("Screenshot gagal diambil!");
            yield break;
        }

        if (newScreenshot.width <= 0 || newScreenshot.height <= 0)
        {
            Debug.LogError("Resolusi screenshot tidak valid!");
            Destroy(newScreenshot);
            yield break;
        }

        // Hapus screenshot sebelumnya.
        if (currentScreenshot != null)
        {
            Destroy(currentScreenshot);
        }

        // Simpan screenshot terbaru.
        currentScreenshot = newScreenshot;

        // =====================================
        // 2. TAMPILKAN SCREENSHOT PADA RAW IMAGE
        // =====================================

        screenshotPanel.SetActive(true);
        HideUIWhilePanelOpen();

        Canvas.ForceUpdateCanvases();

        // Aktifkan RawImage.
        screenshotPreview.gameObject.SetActive(true);
        screenshotPreview.enabled = true;

        // Pastikan warna RawImage tidak transparan.
        screenshotPreview.color = Color.white;

        // Masukkan Texture ke RawImage.
        screenshotPreview.texture = currentScreenshot;

        // Atur rasio gambar.
        if (aspectRatioFitter != null)
        {
            aspectRatioFitter.aspectMode =
                AspectRatioFitter.AspectMode.FitInParent;

            aspectRatioFitter.aspectRatio =
                (float)currentScreenshot.width /
                currentScreenshot.height;
        }

        // Perbarui tampilan UI.
        screenshotPreview.SetAllDirty();
        Canvas.ForceUpdateCanvases();

        // =====================================
        // 3. DEBUG TAMPILAN PREVIEW
        // =====================================

        Debug.Log("========== SCREENSHOT PREVIEW ==========");

        Debug.Log("Screenshot berhasil ditampilkan.");

        Debug.Log("Texture tersedia: " +
            (screenshotPreview.texture != null));

        Debug.Log("RawImage aktif: " +
            screenshotPreview.gameObject.activeInHierarchy);

        Debug.Log("RawImage enabled: " +
            screenshotPreview.enabled);

        Debug.Log("Resolusi screenshot: " +
            currentScreenshot.width + " x " +
            currentScreenshot.height);

        Debug.Log("Ukuran RawImage: " +
            screenshotPreview.rectTransform.rect.size);

        Debug.Log("========================================");
    }

    // =====================================
    // 4. DOWNLOAD SCREENSHOT
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

        string folderPath = Path.Combine(
            Application.dataPath,
            "../ScreenshotTest"
        );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, fileName);

        File.WriteAllBytes(filePath, imageBytes);

        Debug.Log("Screenshot tersimpan di: " + filePath);

#elif UNITY_ANDROID

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

#else

        Debug.LogWarning(
            "Fitur download belum dikonfigurasi untuk platform ini."
        );

#endif
    }

    // =====================================
    // 5. SHARE SCREENSHOT
    // =====================================

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

#if UNITY_ANDROID || UNITY_IOS

        new NativeShare()
            .AddFile(filePath)
            .SetSubject("Screenshot AR Kimia")
            .SetText("Lihat hasil eksplorasi AR Kimia saya!")
            .Share();

#else

        Debug.LogWarning(
            "Fitur share hanya dikonfigurasi untuk Android dan iOS."
        );

#endif
    }

    // =====================================
    // 6. KEMBALI KE AR CAMERA
    // =====================================

    public void CloseScreenshotPanel()
    {
        if (screenshotPanel != null)
        {
            screenshotPanel.SetActive(false);
        }

        RestoreUIAfterPanelClosed();

        Debug.Log("Kembali ke halaman AR Camera.");
    }

    // =====================================
    // 7. MEMBERSIHKAN MEMORI
    // =====================================

    private void OnDestroy()
    {
        RestoreUIAfterPanelClosed();

        if (screenshotPreview != null)
        {
            screenshotPreview.texture = null;
        }

        if (currentScreenshot != null)
        {
            Destroy(currentScreenshot);
            currentScreenshot = null;
        }
    }

    private void HideUIWhilePanelOpen()
    {
        if (isPanelUIHidden || uiHiddenWhilePanelOpen == null)
            return;

        panelHiddenStates = new bool[uiHiddenWhilePanelOpen.Length];

        for (int i = 0; i < uiHiddenWhilePanelOpen.Length; i++)
        {
            if (uiHiddenWhilePanelOpen[i] != null)
            {
                panelHiddenStates[i] = uiHiddenWhilePanelOpen[i].activeSelf;
                uiHiddenWhilePanelOpen[i].SetActive(false);
            }
        }

        isPanelUIHidden = true;
    }

    private void RestoreUIAfterPanelClosed()
    {
        if (!isPanelUIHidden || uiHiddenWhilePanelOpen == null)
            return;

        for (int i = 0; i < uiHiddenWhilePanelOpen.Length; i++)
        {
            if (uiHiddenWhilePanelOpen[i] != null)
            {
                uiHiddenWhilePanelOpen[i].SetActive(panelHiddenStates[i]);
            }
        }

        isPanelUIHidden = false;
    }

}
