using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElementListItemUI : MonoBehaviour
{
    public TMP_Text symbolText, nameText;
    public Button button;

    public void Setup(ElementData e)
    {
        symbolText.text = e.symbol;
        nameText.text = e.displayName;
        button.onClick.AddListener(() => MarkerView.OnMarkerTapped?.Invoke(e));
    }
}
