using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ElementSidebarUI : MonoBehaviour
{
    public enum Tab { Unsur, Senyawa }

    [Header("Data")]
    public ChemistryDatabase db;

    [Header("Daftar")]
    public Transform listParent;
    public ElementListItemUI itemPrefab;

    [Header("Search dan Tab")]
    public TMP_InputField searchInput;
    public Button unsurTab;
    public Button senyawaTab;
    public Tab startTab = Tab.Unsur;

    [Header("Pesan kosong (opsional)")]
    public TMP_Text emptyText;

    [Header("Warna tab")]
    public Color activeColor = new Color32(255, 235, 214, 255);
    public Color inactiveColor = new Color32(255, 235, 214, 45);
    public Color activeTextColor = new Color32(47, 43, 92, 255);
    public Color inactiveTextColor = new Color32(255, 235, 214, 255);

    Tab current;
    ScrollRect scroll;

    void Start()
    {
        if (db == null) db = FindFirstObjectByType<ChemistryDatabase>();
        if (db == null) { Debug.LogError("ElementSidebarUI: ChemistryDatabase tidak ditemukan di scene"); return; }
        if (listParent == null || itemPrefab == null || searchInput == null)
        {
            Debug.LogError("ElementSidebarUI: kolom List Parent, Item Prefab, atau Search Input masih kosong");
            return;
        }

        scroll = listParent.GetComponentInParent<ScrollRect>();

        if (unsurTab != null) unsurTab.onClick.AddListener(() => SetTab(Tab.Unsur));
        else Debug.LogWarning("ElementSidebarUI: kolom Unsur Tab kosong");
        if (senyawaTab != null) senyawaTab.onClick.AddListener(() => SetTab(Tab.Senyawa));
        else Debug.LogWarning("ElementSidebarUI: kolom Senyawa Tab kosong");

        searchInput.onValueChanged.AddListener(_ => Refresh());
        SetTab(startTab);
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

    void Refresh()
    {
        // bersihkan daftar (dimatikan dulu supaya layout tidak menghitung objek yang akan dihapus)
        foreach (Transform t in listParent)
        {
            t.gameObject.SetActive(false);
            Destroy(t.gameObject);
        }

        string f = (searchInput.text ?? "").Trim().ToLower();
        int count = 0;

        if (current == Tab.Unsur)
{
    foreach (var e in db.elements.Where(x => x != null).OrderBy(x => x.atomicNumber))
    {
        if (!Match(f, e.displayName, e.symbol)) continue;
        Instantiate(itemPrefab, listParent).Setup(e);
        count++;
    }
}
        else if (current == Tab.Senyawa)
        {
            foreach (var c in db.compounds.Where(x => x != null).OrderBy(x => x.name))
            {
                if (!Match(f, c.name, c.formula)) continue;
                Instantiate(itemPrefab, listParent).Setup(c);
                count++;
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

        // kembali ke atas daftar setiap kali isi berganti
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