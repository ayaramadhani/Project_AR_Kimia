using UnityEngine;
using UnityEngine.UI;

public class CombinePanelUI : MonoBehaviour
{
    public Transform slotParent;
    public CombineSlotUI slotPrefab;
    public Button combineButton, closeButton;
    public ReactionService reaction;
    public ResultPopupUI resultPopup;

    ElementInventory inv;

    public void Init(ElementInventory inventory)
    {
        inv = inventory;
        inv.OnChanged += Refresh;
        combineButton.onClick.AddListener(OnCombine);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        Refresh();
    }

    void Refresh()
    {
        foreach (Transform t in slotParent) Destroy(t.gameObject);
        foreach (var kv in inv.Items)
            Instantiate(slotPrefab, slotParent).Setup(kv.Key, kv.Value, inv);

        combineButton.gameObject.SetActive(inv.Items.Count > 0);
        if (inv.Items.Count == 0) gameObject.SetActive(false);
    }

    void OnCombine()
    {
        var result = reaction.TryCombine(inv);
        resultPopup.Show(result);
        if (result != null) inv.Clear();
    }
}
