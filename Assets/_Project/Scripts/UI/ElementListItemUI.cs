using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ARKimia.Data;

public class ElementListItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public TMP_Text symbolText, nameText;
    public GameObject unreadBadge;

    [Header("Long-press")]
    public float longPressSeconds = 0.5f;

    ElementSO element;
    CompoundSO compound;
    string plainSymbol;
    ScrollRect scroll;
    ElementSidebarUI sidebar;
    RectTransform ghost;
    bool draggingItem;
    bool wasDragged;

    bool pressing, longPressFired;
    float pressTime;
    Vector2 pressPos;
    Vector3 baseScale = Vector3.one;

    public ElementSO Element => element;
    public CompoundSO Compound => compound;
    public bool IsUnread => unreadBadge != null && unreadBadge.activeSelf;

    static string Sub(string f) => Regex.Replace(f ?? "", "[0-9]+", "<sub>$0</sub>");

    void Awake()
    {
        baseScale = transform.localScale;
        EnsureUnreadBadge();
    }

    public void Setup(ElementSO e, bool isUnread = false)
    {
        element = e;
        compound = null;
        plainSymbol = e != null ? e.Symbol : "";
        if (symbolText != null) symbolText.text = plainSymbol;
        if (nameText != null) nameText.text = e != null ? e.ElementName : "";
        Cache();
        SetUnread(isUnread);
    }

    public void Setup(CompoundSO c, bool isUnread = false)
    {
        compound = c;
        element = null;
        plainSymbol = c != null ? c.ChemicalFormula : "";
        if (symbolText != null) symbolText.text = Sub(plainSymbol);
        if (nameText != null) nameText.text = c != null ? c.CompoundName : "";
        Cache();
        SetUnread(isUnread);
    }

    public void SetUnread(bool unread)
    {
        EnsureUnreadBadge();
        if (unreadBadge != null)
            unreadBadge.SetActive(unread);
    }

    void EnsureUnreadBadge()
    {
        if (unreadBadge != null)
            return;

        Transform existing = transform.Find("UnreadBadge");
        if (existing != null)
        {
            unreadBadge = existing.gameObject;
            return;
        }

        var go = new GameObject("UnreadBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-14f, -14f);
        rt.sizeDelta = new Vector2(20f, 20f);

        var img = go.GetComponent<Image>();
        Sprite circle = Resources.Load<Sprite>("UI/circle_white");
        if (circle != null) img.sprite = circle;
        img.color = new Color32(229, 37, 42, 255);
        img.raycastTarget = false;

        unreadBadge = go;
        unreadBadge.SetActive(false);
    }

    void Cache()
    {
        scroll = GetComponentInParent<ScrollRect>();
        sidebar = GetComponentInParent<ElementSidebarUI>();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        pressing = true;
        longPressFired = false;
        wasDragged = false;
        pressTime = Time.unscaledTime;
        pressPos = e.position;
    }

    public void OnPointerUp(PointerEventData e)
    {
        CancelPress();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (wasDragged || draggingItem || longPressFired)
        {
            longPressFired = false;
            return;
        }

        FireLongPress();
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

        Vector2 cur = Pointer.current != null ? Pointer.current.position.ReadValue() : pressPos;
        float limit = Mathf.Max(20f, Screen.dpi * 0.08f);
        if ((cur - pressPos).magnitude > limit)
        {
            CancelPress();
            return;
        }

        float t = (Time.unscaledTime - pressTime) / longPressSeconds;
        transform.localScale = baseScale * Mathf.Lerp(1f, 0.96f, Mathf.Clamp01(t));
        if (t >= 1f) FireLongPress();
    }

    public void TriggerLongPressImmediate()
    {
        FireLongPress();
    }

    void FireLongPress()
    {
        pressing = false;
        longPressFired = true;
        transform.localScale = baseScale;
        SetUnread(false);

        if (element != null)
        {
            QuickdropEvents.RaiseElementInfoRequested(element);
        }
        else if (compound != null)
        {
            QuickdropEvents.RaiseCompoundInfoRequested(compound);
        }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        CancelPress();
        wasDragged = true;
        if (longPressFired)
        {
            draggingItem = false;
            return;
        }

        Vector2 d = e.position - e.pressPosition;
        draggingItem = Mathf.Abs(d.x) > Mathf.Abs(d.y);

        if (!draggingItem)
        {
            if (scroll) scroll.OnBeginDrag(e);
            return;
        }
        CreateGhost(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        if (longPressFired) return;
        if (!draggingItem)
        {
            if (scroll) scroll.OnDrag(e);
            return;
        }
        if (ghost) ghost.position = e.position;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (longPressFired) return;
        if (!draggingItem)
        {
            if (scroll) scroll.OnEndDrag(e);
            return;
        }

        if (ghost) Destroy(ghost.gameObject);
        draggingItem = false;

        if (sidebar != null && RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)sidebar.transform, e.position, null)) return;

        if (element != null)
        {
            QuickdropEvents.RaiseElementDropped(element, e.position);
        }
        else if (compound != null)
        {
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
        if (symbolText != null) t.font = symbolText.font;
        t.fontSize = 84;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1, 1, 1, 0.9f);

        ghost = (RectTransform)go.transform;
        ghost.sizeDelta = new Vector2(160, 120);
        ghost.position = pos;
    }

    void OnDisable() { CancelPress(); if (ghost) Destroy(ghost.gameObject); }
    void OnDestroy() { if (ghost) Destroy(ghost.gameObject); }
}
