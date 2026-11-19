
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ARScreenshotController : MonoBehaviour
{
    [Header("UI yang Disembunyikan Saat Screenshot")]
    [SerializeField] private GameObject[] uiToHide;

    [Header("UI References")]
    [SerializeField] private GameObject leftSidebar;
    [SerializeField] private GameObject[] additionalHudRoots;

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

    public GameObject ScreenshotPanel => screenshotPanel;

    // =====================================
    // KONFIGURASI REFERENSI
    // =====================================

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

        if (arCamera == null)
        {
            Debug.LogError("AR Camera belum diatur!");
            return;
        }

        StartCoroutine(CaptureARScreen());
    }

    private IEnumerator CaptureARScreen()
    {
        isCapturing = true;

        Texture2D newScreenshot = null;

        // Menyimpan status awal setiap UI yang akan disembunyikan.
        Dictionary<GameObject, bool> previousStates =
            new Dictionary<GameObject, bool>();

        RenderTexture rt = null;
        RenderTexture previousTarget = null;
        RenderTexture previousActive = null;

        try
        {
            // Sembunyikan UI yang tidak ingin masuk screenshot.
            if (uiToHide != null)
            {
                foreach (GameObject ui in uiToHide)
                    HideAndRemember(ui, previousStates);
            }

            // Sembunyikan sidebar.
            HideAndRemember(leftSidebar, previousStates);

            // Sembunyikan HUD tambahan.
            if (additionalHudRoots != null)
            {
                foreach (GameObject hud in additionalHudRoots)
                    HideAndRemember(hud, previousStates);
            }

            // Sembunyikan panel screenshot sementara.
            HideAndRemember(screenshotPanel, previousStates);

            // Perbarui Canvas sebelum mengambil gambar.
            Canvas.ForceUpdateCanvases();

            // Tunggu sampai proses rendering selesai.
            yield return new WaitForEndOfFrame();

            int width = Screen.width;
            int height = Screen.height;

            if (width <= 0 || height <= 0)
            {
                Debug.LogError("Resolusi layar tidak valid!");
                yield break;
            }

            rt = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32
            );

            previousTarget = arCamera.targetTexture;
            previousActive = RenderTexture.active;

            arCamera.targetTexture = rt;
            arCamera.Render();

            RenderTexture.active = rt;

            newScreenshot = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false
            );

            newScreenshot.ReadPixels(
                new Rect(0, 0, width, height),
                0,
                0
            );

            newScreenshot.Apply();

            Debug.Log("Screenshot berhasil dibuat.");
            Debug.Log("Resolusi: " + width + " x " + height);
        }
        finally
        {
            // Kembalikan target kamera.
            if (arCamera != null)
                arCamera.targetTexture = previousTarget;

            RenderTexture.active = previousActive;

            // Hapus RenderTexture sementara.
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }

            // Kembalikan seluruh UI ke kondisi sebelumnya.
            RestoreCaptureUI(previousStates);

            isCapturing = false;
        }

        // Periksa hasil screenshot.
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
            Destroy(currentScreenshot);

        currentScreenshot = newScreenshot;

        // =====================================
        // 2. MENAMPILKAN SCREENSHOT PREVIEW
        // =====================================

        screenshotPanel.SetActive(true);

        // Sembunyikan UI tertentu selama preview terbuka.
        HideUIWhilePanelOpen();

        Canvas.ForceUpdateCanvases();

        screenshotPreview.gameObject.SetActive(true);
        screenshotPreview.enabled = true;
        screenshotPreview.color = Color.white;
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

        screenshotPreview.SetAllDirty();
        Canvas.ForceUpdateCanvases();

        // Debug preview.
        Debug.Log("========== SCREENSHOT PREVIEW ==========");
        Debug.Log("Screenshot berhasil ditampilkan.");
        Debug.Log("Texture tersedia: " +
            (screenshotPreview.texture != null));
        Debug.Log("RawImage aktif: " +
            screenshotPreview.gameObject.activeInHierarchy);
        Debug.Log("RawImage enabled: " +
            screenshotPreview.enabled);
        Debug.Log("Resolusi: " +
            currentScreenshot.width + " x " +
            currentScreenshot.height);
        Debug.Log("Ukuran RawImage: " +
            screenshotPreview.rectTransform.rect.size);
        Debug.Log("========================================");
    }

    // =====================================
    // HELPER: MENYEMBUNYIKAN UI
    // =====================================

    private void HideAndRemember(
        GameObject ui,
        Dictionary<GameObject, bool> previousStates)
    {
        if (ui == null || previousStates.ContainsKey(ui))
            return;

        previousStates.Add(ui, ui.activeSelf);
        ui.SetActive(false);
    }

    // =====================================
    // HELPER: MENGEMBALIKAN UI
    // =====================================

    private void RestoreCaptureUI(
        Dictionary<GameObject, bool> previousStates)
    {
        foreach (KeyValuePair<GameObject, bool> item in previousStates)
        {
            if (item.Key != null)
                item.Key.SetActive(item.Value);
        }
    }

    // =====================================
    // 3. UI SELAMA PANEL SCREENSHOT TERBUKA
    // =====================================

    private void HideUIWhilePanelOpen()
    {
        if (isPanelUIHidden || uiHiddenWhilePanelOpen == null)
            return;

        panelHiddenStates =
            new bool[uiHiddenWhilePanelOpen.Length];

        for (int i = 0; i < uiHiddenWhilePanelOpen.Length; i++)
        {
            GameObject ui = uiHiddenWhilePanelOpen[i];

            if (ui != null)
            {
                panelHiddenStates[i] = ui.activeSelf;
                ui.SetActive(false);
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
            GameObject ui = uiHiddenWhilePanelOpen[i];

            if (ui != null && panelHiddenStates != null &&
                i < panelHiddenStates.Length)
            {
                ui.SetActive(panelHiddenStates[i]);
            }
        }

        isPanelUIHidden = false;
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
            Directory.CreateDirectory(folderPath);

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
                    Debug.Log("Berhasil disimpan: " + path);
                else
                    Debug.LogError("Gagal menyimpan screenshot.");
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
            screenshotPanel.SetActive(false);

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
            screenshotPreview.texture = null;

        if (currentScreenshot != null)
        {
            Destroy(currentScreenshot);
            currentScreenshot = null;
        }
    }
}
