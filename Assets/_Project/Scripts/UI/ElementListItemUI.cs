using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ElementListItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public TMP_Text symbolText, nameText;

    [Header("Long-press")]
    public float longPressSeconds = 0.5f;

    ElementData element;
    CompoundData compound;
    string plainSymbol;
    ScrollRect scroll;
    ElementSidebarUI sidebar;
    RectTransform ghost;
    bool draggingItem;

    bool pressing, longPressFired;
    float pressTime;
    Vector2 pressPos;
    Vector3 baseScale = Vector3.one;

    static string Sub(string f) => Regex.Replace(f, "[0-9]+", "<sub>$0</sub>");

    void Awake() => baseScale = transform.localScale;

    public void Setup(ElementData e)
    {
        element = e; compound = null;
        plainSymbol = e.symbol;
        symbolText.text = e.symbol;
        nameText.text = e.displayName;
        Cache();
    }

    public void Setup(CompoundData c)
    {
        compound = c; element = null;
        plainSymbol = c.formula;
        symbolText.text = Sub(c.formula);
        nameText.text = c.displayName;
        Cache();
    }

    void Cache()
    {
        scroll = GetComponentInParent<ScrollRect>();
        sidebar = GetComponentInParent<ElementSidebarUI>();
    }

    // ---------- Long-press ----------
    public void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        pressing = true;
        longPressFired = false;
        pressTime = Time.unscaledTime;
        pressPos = e.position;
    }

    public void OnPointerUp(PointerEventData e)
    {
        CancelPress();
        longPressFired = false;
    }

    void CancelPress()
    {
        pressing = false;
        transform.localScale = baseScale;
    }

    void Update()
    {
        if (!pressing) return;

        Vector2 cur = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
        float limit = Mathf.Max(20f, Screen.dpi * 0.08f);        // toleransi gerak jari
        if ((cur - pressPos).magnitude > limit) { CancelPress(); return; }

        float t = (Time.unscaledTime - pressTime) / longPressSeconds;
        transform.localScale = baseScale * Mathf.Lerp(1f, 0.96f, Mathf.Clamp01(t));   // umpan balik menekan
        if (t >= 1f) FireLongPress();
    }

    void FireLongPress()
    {
        pressing = false;
        longPressFired = true;
        transform.localScale = baseScale;

        if (element != null)
        {
            Debug.Log("Long-press unsur: " + element.displayName);
            QuickdropEvents.RaiseElementInfoRequested(element);
        }
        else if (compound != null)
        {
            Debug.Log("Long-press senyawa: " + compound.displayName);
            QuickdropEvents.RaiseCompoundInfoRequested(compound);
        }
    }

    // ---------- Drag and drop ----------
    public void OnBeginDrag(PointerEventData e)
    {
        CancelPress();
        if (longPressFired) { draggingItem = false; return; }   // setelah long-press, jangan mulai drag

        Vector2 d = e.position - e.pressPosition;
        draggingItem = Mathf.Abs(d.x) > Mathf.Abs(d.y);

        if (!draggingItem) { if (scroll) scroll.OnBeginDrag(e); return; }
        CreateGhost(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        if (longPressFired) return;
        if (!draggingItem) { if (scroll) scroll.OnDrag(e); return; }
        if (ghost) ghost.position = e.position;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (longPressFired) return;
        if (!draggingItem) { if (scroll) scroll.OnEndDrag(e); return; }

        if (ghost) Destroy(ghost.gameObject);
        draggingItem = false;

        // dilepas di atas sidebar = batal
        if (sidebar != null && RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)sidebar.transform, e.position, null)) return;

        if (element != null)
        {
            Debug.Log("Drop unsur: " + element.displayName);
            QuickdropEvents.RaiseElementDropped(element, e.position);
        }
        else if (compound != null)
        {
            Debug.Log("Drop senyawa: " + compound.displayName);
            QuickdropEvents.RaiseCompoundDropped(compound, e.position);
        }
    }

    void CreateGhost(Vector2 pos)
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup));
        go.GetComponent<CanvasGroup>().blocksRaycasts = false;
        go.transform.SetParent(canvas.rootCanvas.transform, false);
        go.transform.SetAsLastSibling();

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = plainSymbol;
        t.font = symbolText.font;
        t.fontSize = 120;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1, 1, 1, 0.9f);

        ghost = (RectTransform)go.transform;
        ghost.sizeDelta = new Vector2(220, 160);
        ghost.position = pos;
    }

    void OnDisable() { CancelPress(); if (ghost) Destroy(ghost.gameObject); }
    void OnDestroy() { if (ghost) Destroy(ghost.gameObject); }
}