using TMPro;
using UnityEngine;

public class ElementSidebarUI : MonoBehaviour
{
    public ChemistryDatabase db;
    public Transform listParent;
    public ElementListItemUI itemPrefab;
    public TMP_InputField searchInput;

    void Start()
    {
        searchInput.onValueChanged.AddListener(Populate);
        Populate("");
    }

    void Populate(string filter)
    {
        foreach (Transform t in listParent) Destroy(t.gameObject);
        string f = (filter ?? "").ToLower();

        foreach (var e in db.elements)
        {
            if (e == null) continue;
            if (f.Length > 0 &&
                !e.displayName.ToLower().Contains(f) &&
                !e.symbol.ToLower().Contains(f)) continue;

            Instantiate(itemPrefab, listParent).Setup(e);
        }
    }
}
