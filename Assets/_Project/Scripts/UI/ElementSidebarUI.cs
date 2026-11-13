using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ARKimia.Data;
using ARKimia.Gameplay;

public class ElementSidebarUI : MonoBehaviour
{
    public enum Tab { Unsur, Senyawa }

    [Header("Data")]
    public ChemistryDatabase db;
    public bool onlyShowDiscovered = true;

    [Header("Daftar")]
    public Transform listParent;
    public ElementListItemUI itemPrefab;

    [Header("Search dan Tab")]
    public TMP_InputField searchInput;
    public Button unsurTab;
    public Button senyawaTab;
    public Tab startTab = Tab.Unsur;

    [Header("Notifikasi & Pesan Kosong")]
    public GameObject sidebarUnreadBadge;
    public TMP_Text emptyText;

    [Header("Warna tab")]
    public Color activeColor = new Color32(255, 235, 214, 255);
    public Color inactiveColor = new Color32(255, 235, 214, 45);
    public Color activeTextColor = new Color32(47, 43, 92, 255);
    public Color inactiveTextColor = new Color32(255, 235, 214, 255);

    Tab current;
    ScrollRect scroll;
    ElementCombinationManager boundManager;
    bool initialized;

    public Tab CurrentTab => current;

    void Awake()
    {
        EnsureDatabase();
        if (listParent != null)
            scroll = listParent.GetComponentInParent<ScrollRect>();
    }

    void OnEnable()
    {
        EnsureDatabase();
        BindManager();
        if (initialized)
            Refresh();
    }

    void Start()
    {
        EnsureDatabase();
        BindManager();

        if (listParent == null || itemPrefab == null || searchInput == null)
        {
            Debug.LogError("ElementSidebarUI: kolom List Parent, Item Prefab, atau Search Input masih kosong");
            return;
        }

        scroll = listParent.GetComponentInParent<ScrollRect>();

        if (unsurTab != null) unsurTab.onClick.AddListener(() => SetTab(Tab.Unsur));
        if (senyawaTab != null) senyawaTab.onClick.AddListener(() => SetTab(Tab.Senyawa));

        searchInput.onValueChanged.AddListener(_ => Refresh());
        initialized = true;
        SetTab(startTab);
    }

    void OnDisable()
    {
        UnbindManager();
    }

    void EnsureDatabase()
    {
        if (db == null && ElementCombinationManager.Instance != null)
            db = ElementCombinationManager.Instance.Database;
        if (db == null)
            db = Resources.Load<ChemistryDatabase>("ChemistryData/ChemistryDatabase");
    }

    void BindManager()
    {
        ElementCombinationManager target = ElementCombinationManager.Instance != null
            ? ElementCombinationManager.Instance
            : FindAnyObjectByType<ElementCombinationManager>();

        if (target == null || target == boundManager)
            return;

        UnbindManager();
        boundManager = target;
        if (db == null && boundManager.Database != null)
            db = boundManager.Database;

        boundManager.OnElementDiscovered += HandleElementDiscovered;
        boundManager.OnCompoundDiscovered += HandleCompoundDiscovered;
        boundManager.OnCombinationSuccess += HandleCompoundDiscovered;
        boundManager.OnUnreadStateChanged += RefreshUnreadBadges;
    }

    void UnbindManager()
    {
        if (boundManager == null)
            return;

        boundManager.OnElementDiscovered -= HandleElementDiscovered;
        boundManager.OnCompoundDiscovered -= HandleCompoundDiscovered;
        boundManager.OnCombinationSuccess -= HandleCompoundDiscovered;
        boundManager.OnUnreadStateChanged -= RefreshUnreadBadges;
        boundManager = null;
    }

    void HandleElementDiscovered(ElementSO _)
    {
        Refresh();
    }

    void HandleCompoundDiscovered(CompoundSO _)
    {
        Refresh();
    }

    public void SetTab(Tab tab)
    {
        current = tab;
        ApplyTabVisual(unsurTab, tab == Tab.Unsur);
        ApplyTabVisual(senyawaTab, tab == Tab.Senyawa);
        Refresh();
    }

    void ApplyTabVisual(Button b, bool active)
    {
        if (b == null) return;
        if (b.targetGraphic is Image img) img.color = active ? activeColor : inactiveColor;
        var label = b.GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = active ? activeTextColor : inactiveTextColor;
    }

    public void RefreshUnreadBadges()
    {
        if (sidebarUnreadBadge != null)
        {
            sidebarUnreadBadge.SetActive(boundManager != null && boundManager.HasAnyUnread);
        }

        if (listParent == null)
            return;

        ElementListItemUI[] items = listParent.GetComponentsInChildren<ElementListItemUI>(false);
        for (int i = 0; i < items.Length; i++)
        {
            ElementListItemUI item = items[i];
            if (item == null) continue;
            if (item.Element != null)
                item.SetUnread(boundManager != null && boundManager.IsElementUnread(item.Element));
            else if (item.Compound != null)
                item.SetUnread(boundManager != null && boundManager.IsCompoundUnread(item.Compound));
        }
    }

    public void Refresh()
    {
        EnsureDatabase();
        BindManager();
        if (listParent == null || itemPrefab == null)
            return;

        var toDestroy = new List<GameObject>();
        foreach (Transform t in listParent)
        {
            if (t != null)
            {
                t.gameObject.SetActive(false);
                toDestroy.Add(t.gameObject);
            }
        }
        for (int i = 0; i < toDestroy.Count; i++)
        {
            if (Application.isPlaying)
                Destroy(toDestroy[i]);
            else
                DestroyImmediate(toDestroy[i]);
        }

        string f = (searchInput != null && searchInput.text != null) ? searchInput.text.Trim().ToLower() : "";
        int count = 0;

        if (current == Tab.Unsur)
        {
            IReadOnlyList<ElementSO> source = (onlyShowDiscovered && boundManager != null)
                ? boundManager.DiscoveredElementOrder
                : (db != null ? db.Elements : null);

            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    ElementSO e = source[i];
                    if (e == null || !Match(f, e.ElementName, e.Symbol, e.ElementId)) continue;
                    var item = Instantiate(itemPrefab, listParent);
                    item.name = $"SidebarItem_{e.Symbol}";
                    item.gameObject.SetActive(true);
                    bool unread = boundManager != null && boundManager.IsElementUnread(e);
                    item.Setup(e, unread);
                    count++;
                }
            }
        }
        else
        {
            IReadOnlyList<CompoundSO> source = (onlyShowDiscovered && boundManager != null)
                ? boundManager.DiscoveredCompoundOrder
                : (db != null ? db.Compounds : null);

            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    CompoundSO c = source[i];
                    if (c == null || !Match(f, c.CompoundName, c.ChemicalFormula, c.CompoundId)) continue;
                    var item = Instantiate(itemPrefab, listParent);
                    item.name = $"SidebarItem_{c.ChemicalFormula}";
                    item.gameObject.SetActive(true);
                    bool unread = boundManager != null && boundManager.IsCompoundUnread(c);
                    item.Setup(c, unread);
                    count++;
                }
            }
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(count == 0);
            if (count == 0)
                emptyText.text = f.Length > 0
                    ? "Tidak ditemukan"
                    : (current == Tab.Unsur ? "Belum ada unsur" : "Belum ada senyawa");
        }

        RefreshUnreadBadges();

        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    static bool Match(string f, params string[] fields)
    {
        if (f.Length == 0) return true;
        foreach (var s in fields)
            if (!string.IsNullOrEmpty(s) && s.ToLower().Contains(f)) return true;
        return false;
    }
}
