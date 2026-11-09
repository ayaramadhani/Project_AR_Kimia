using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler))]
public class EncyclopediaPopup : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public GameObject panel;            // Overlay (latar gelap + kartu)
    public RectTransform card;
    public TMP_Text categoryText, symbolText, nameText, infoText, descText, funFactText;
    public Button closeButton;

    [Header("Tampilan")]
    public Image accentLine;            // garis pemisah di bawah header
    public GameObject funFactBox;       // kotak "Tahukah kamu?"
    public Color unsurColor = new Color32(255, 181, 46, 255);
    public Color senyawaColor = new Color32(92, 224, 208, 255);

    [Header("Data")]
    public List<EncyclopediaEntry> entries = new();

    public bool debugReport = false;

    CanvasGroup group;
    Coroutine anim;
    Vector2 cardBase;
    Color accent;

    static string Sub(string f) => Regex.Replace(f, "[0-9]+", "<sub>$0</sub>");

    void Awake()
    {
        // Canvas mandiri di akar scene supaya selalu di atas UI lain
        if (transform.parent != null) transform.SetParent(null, false);

        var cv = GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;

        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        if (panel == null) { Debug.LogError("EncyclopediaPopup: kolom Panel kosong"); return; }
        if (card == null) Debug.LogError("EncyclopediaPopup: kolom Card kosong");
        else
        {
            if (!card.IsChildOf(panel.transform))
                Debug.LogError("EncyclopediaPopup: Card harus berada DI DALAM Overlay (Panel).");
            cardBase = card.anchoredPosition;
        }

        if (!panel.TryGetComponent(out group)) group = panel.AddComponent<CanvasGroup>();
        if (closeButton) closeButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    void OnEnable()
    {
        QuickdropEvents.ElementInfoRequested += ShowElement;
        QuickdropEvents.CompoundInfoRequested += ShowCompound;
    }

    void OnDisable()
    {
        QuickdropEvents.ElementInfoRequested -= ShowElement;
        QuickdropEvents.CompoundInfoRequested -= ShowCompound;
    }

    void Update()
    {
        // tombol Back Android = Escape
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    EncyclopediaEntry Find(string key)
    {
        foreach (var e in entries)
            if (e != null && e.key == key) return e;
        return null;
    }

    void ShowElement(ElementData e)
    {
        if (e == null) return;
        var entry = Find(e.symbol);
        ApplyAccent(false);
        categoryText.text = "UNSUR";
        symbolText.text = e.symbol;
        nameText.text = e.displayName;
        infoText.text = (entry != null && !string.IsNullOrWhiteSpace(entry.subtitle))
            ? entry.subtitle : "Nomor atom " + e.atomicNumber;
        Fill(entry, null);
        Open();
        Debug.Log("Ensiklopedia dibuka: " + e.displayName);
    }

    void ShowCompound(CompoundData c)
    {
        if (c == null) return;
        var entry = Find(c.formula);
        ApplyAccent(true);
        categoryText.text = "SENYAWA";
        symbolText.text = Sub(c.formula);
        nameText.text = c.displayName;
        infoText.text = (entry != null && !string.IsNullOrWhiteSpace(entry.subtitle))
            ? entry.subtitle : "Rumus " + c.formula;
        Fill(entry, c.description);
        Open();
        Debug.Log("Ensiklopedia dibuka: " + c.displayName);
    }

    void ApplyAccent(bool compound)
    {
        accent = compound ? senyawaColor : unsurColor;
        categoryText.color = accent;
        if (accentLine) accentLine.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        if (funFactBox && funFactBox.TryGetComponent(out Image box))
            box.color = new Color(accent.r, accent.g, accent.b, 0.16f);
        funFactText.color = new Color32(255, 235, 214, 255);
    }

    void Fill(EncyclopediaEntry entry, string fallbackDesc)
    {
        string d = entry != null ? entry.description : null;
        if (string.IsNullOrWhiteSpace(d))
            d = string.IsNullOrWhiteSpace(fallbackDesc) ? "Informasi lengkap belum tersedia." : fallbackDesc;
        descText.text = d;

        bool hasFact = entry != null && !string.IsNullOrWhiteSpace(entry.funFact);
        GameObject factRoot = funFactBox != null ? funFactBox : funFactText.gameObject;
        factRoot.SetActive(hasFact);
        if (hasFact)
            funFactText.text = "<size=75%><color=#" + ColorUtility.ToHtmlStringRGB(accent) +
                               ">TAHUKAH KAMU?</color></size>\n" + entry.funFact;
    }

    void Open()
    {
        group.alpha = 0f;                       // cegah kedipan dari pembukaan sebelumnya
        panel.SetActive(true);
        if (card != null) card.gameObject.SetActive(true);
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(PopIn());
    }

    public void Close()
    {
        if (anim != null) StopCoroutine(anim);
        panel.SetActive(false);
    }

    IEnumerator PopIn()
    {
        Canvas.ForceUpdateCanvases();
        if (card != null) LayoutRebuilder.ForceRebuildLayoutImmediate(card);
        yield return null;                      // tunggu tinggi teks selesai dihitung
        if (card != null) LayoutRebuilder.ForceRebuildLayoutImmediate(card);

        const float dur = 0.22f;
        const float rise = 26f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            group.alpha = k;
            if (card != null)
            {
                card.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, k);
                card.anchoredPosition = cardBase + new Vector2(0f, Mathf.Lerp(-rise, 0f, k));
            }
            yield return null;
        }
        group.alpha = 1f;
        if (card != null)
        {
            card.localScale = Vector3.one;
            card.anchoredPosition = cardBase;
        }
        if (debugReport) Debug.Log("[Ensiklopedia] ukuran kartu=" + card.rect.size);
    }

    // Ketuk area gelap di luar kartu = tutup
    public void OnPointerClick(PointerEventData e)
    {
        if (!panel.activeSelf) return;
        if (card != null && RectTransformUtility.RectangleContainsScreenPoint(card, e.position, e.pressEventCamera)) return;
        Close();
    }
}