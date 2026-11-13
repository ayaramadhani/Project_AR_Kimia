using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ARKimia.Data;

[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler))]
public class EncyclopediaPopup : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public GameObject panel;
    public RectTransform card;
    public TMP_Text categoryText, symbolText, nameText, infoText, descText, funFactText;
    public Button closeButton;

    [Header("Tampilan")]
    public Image accentLine;
    public GameObject funFactBox;
    public Color unsurColor = new Color32(255, 181, 46, 255);
    public Color senyawaColor = new Color32(92, 224, 208, 255);

    [Header("Data")]
    public List<EncyclopediaEntry> entries = new();

    public bool debugReport = false;

    CanvasGroup group;
    Coroutine anim;
    Vector2 cardBase;
    Color accent;

    static string Sub(string f) => Regex.Replace(f ?? "", "[0-9]+", "<sub>$0</sub>");

    void Awake()
    {
        if (transform.parent != null) transform.SetParent(null, false);

        var cv = GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;

        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

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
        if (panel != null && panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    EncyclopediaEntry Find(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        foreach (var e in entries)
            if (e != null && string.Equals(e.key, key, System.StringComparison.OrdinalIgnoreCase)) return e;
        return null;
    }

    public void ShowElement(ElementSO e)
    {
        if (e == null || panel == null) return;
        var entry = Find(e.Symbol);
        ApplyAccent(false);
        if (categoryText) categoryText.text = "UNSUR";
        if (symbolText) symbolText.text = e.Symbol;
        if (nameText) nameText.text = e.ElementName;
        if (infoText) infoText.text = (entry != null && !string.IsNullOrWhiteSpace(entry.subtitle))
            ? entry.subtitle : "Nomor atom " + e.AtomicNumber;
        Fill(entry, e.Description, e.FunFact);
        Open();
    }

    public void ShowCompound(CompoundSO c)
    {
        if (c == null || panel == null) return;
        var entry = Find(c.ChemicalFormula);
        ApplyAccent(true);
        if (categoryText) categoryText.text = "SENYAWA";
        if (symbolText) symbolText.text = Sub(c.ChemicalFormula);
        if (nameText) nameText.text = c.CompoundName;
        if (infoText) infoText.text = (entry != null && !string.IsNullOrWhiteSpace(entry.subtitle))
            ? entry.subtitle : "Rumus " + c.ChemicalFormula;
        Fill(entry, c.Description, c.Applications);
        Open();
    }

    void ApplyAccent(bool compound)
    {
        accent = compound ? senyawaColor : unsurColor;
        if (categoryText) categoryText.color = accent;
        if (accentLine) accentLine.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        if (funFactBox && funFactBox.TryGetComponent(out Image box))
            box.color = new Color(accent.r, accent.g, accent.b, 0.16f);
        if (funFactText) funFactText.color = new Color32(255, 235, 214, 255);
    }

    void Fill(EncyclopediaEntry entry, string fallbackDesc, string fallbackFact)
    {
        string d = entry != null ? entry.description : null;
        if (string.IsNullOrWhiteSpace(d))
            d = string.IsNullOrWhiteSpace(fallbackDesc) ? "Informasi lengkap belum tersedia." : fallbackDesc;
        if (descText) descText.text = d;

        string fact = (entry != null && !string.IsNullOrWhiteSpace(entry.funFact)) ? entry.funFact : fallbackFact;
        bool hasFact = !string.IsNullOrWhiteSpace(fact);
        GameObject factRoot = funFactBox != null ? funFactBox : (funFactText != null ? funFactText.gameObject : null);
        if (factRoot != null) factRoot.SetActive(hasFact);
        if (hasFact && funFactText != null)
            funFactText.text = "<size=75%><color=#" + ColorUtility.ToHtmlStringRGB(accent) +
                               ">TAHUKAH KAMU?</color></size>\n" + fact;
    }

    void Open()
    {
        if (group != null) group.alpha = 0f;
        panel.SetActive(true);
        if (card != null) card.gameObject.SetActive(true);
        if (anim != null) StopCoroutine(anim);
        if (Application.isPlaying && isActiveAndEnabled)
            anim = StartCoroutine(PopIn());
        else if (group != null)
            group.alpha = 1f;
    }

    public void Close()
    {
        if (anim != null) StopCoroutine(anim);
        if (panel != null) panel.SetActive(false);
    }

    IEnumerator PopIn()
    {
        Canvas.ForceUpdateCanvases();
        if (card != null) LayoutRebuilder.ForceRebuildLayoutImmediate(card);
        yield return null;
        if (card != null) LayoutRebuilder.ForceRebuildLayoutImmediate(card);

        const float dur = 0.22f;
        const float rise = 26f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            if (group != null) group.alpha = k;
            if (card != null)
            {
                card.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, k);
                card.anchoredPosition = cardBase + new Vector2(0f, Mathf.Lerp(-rise, 0f, k));
            }
            yield return null;
        }
        if (group != null) group.alpha = 1f;
        if (card != null)
        {
            card.localScale = Vector3.one;
            card.anchoredPosition = cardBase;
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (panel == null || !panel.activeSelf) return;
        if (card != null && RectTransformUtility.RectangleContainsScreenPoint(card, e.position, e.pressEventCamera)) return;
        Close();
    }
}
