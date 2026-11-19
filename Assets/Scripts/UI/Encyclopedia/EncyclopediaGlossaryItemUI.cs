using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARKimia.UI
{
    public class EncyclopediaGlossaryItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text termText;
        [SerializeField] private Image selectionArrow;
        [SerializeField] private Button button;
        [SerializeField] private Material defaultTextMaterial;
        [SerializeField] private Material selectedTextMaterial;

        private int termIndex;
        private Action<int> onClicked;
        private bool isBoundClick;

        public TMP_Text TermText => termText;
        public Image SelectionArrow => selectionArrow;
        public Button ItemButton => button;
        public int TermIndex => termIndex;

        public void ConfigureReferences(
            TMP_Text label,
            Image arrow,
            Button btn,
            Material defaultMat,
            Material selectedMat)
        {
            termText = label;
            selectionArrow = arrow;
            button = btn;
            defaultTextMaterial = defaultMat;
            selectedTextMaterial = selectedMat;
        }

        public void Bind(string term, int index, Action<int> clickCallback)
        {
            termIndex = index;
            onClicked = clickCallback;

            if (termText != null)
                termText.text = term;

            SetSelected(false);

            if (button != null && !isBoundClick)
            {
                button.onClick.AddListener(HandleClicked);
                isBoundClick = true;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionArrow != null)
                selectionArrow.gameObject.SetActive(selected);

            if (termText != null)
            {
                Material targetMat = selected ? selectedTextMaterial : defaultTextMaterial;
                if (targetMat != null)
                    termText.fontSharedMaterial = targetMat;
                termText.color = new Color32(45, 44, 91, 255);
            }
        }

        private void HandleClicked()
        {
            onClicked?.Invoke(termIndex);
        }
    }
}
