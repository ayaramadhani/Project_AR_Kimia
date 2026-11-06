using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombineSlotUI : MonoBehaviour
{
    public TMP_Text symbolText, countText;
    public Button minusBtn, plusBtn;

    public void Setup(ElementData e, int count, ElementInventory inv)
    {
        symbolText.text = e.symbol;
        countText.text = count.ToString();
        minusBtn.onClick.AddListener(() => inv.Remove(e));
        plusBtn.onClick.AddListener(() => inv.Add(e));
    }
}
