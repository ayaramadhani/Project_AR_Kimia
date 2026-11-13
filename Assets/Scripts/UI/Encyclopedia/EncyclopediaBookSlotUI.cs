using System;
using System.Text;
using ARKimia.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARKimia.UI
{
    public class EncyclopediaBookSlotUI : MonoBehaviour
    {
        public enum SlotKind
        {
            Element,
            Compound
        }

        [SerializeField] private Image frameImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Button button;
        [SerializeField] private Sprite defaultFrameSprite;
        [SerializeField] private Sprite selectedFrameSprite;

        private Action<EncyclopediaBookSlotUI> clickHandler;
        private bool isBoundClick;

        public SlotKind Kind { get; private set; }
        public ElementSO Element { get; private set; }
        public CompoundSO Compound { get; private set; }
        public string EntryKey { get; private set; }
        public bool IsUnlocked { get; private set; }
        public bool IsSelected { get; private set; }

        public Image FrameImage => frameImage;
        public TMP_Text LabelText => labelText;
        public Button SlotButton => button;

        public void ConfigureReferences(
            Image frame,
            TMP_Text label,
            Button btn,
            Sprite defaultFrame,
            Sprite selectedFrame)
        {
            frameImage = frame;
            labelText = label;
            button = btn;
            defaultFrameSprite = defaultFrame;
            selectedFrameSprite = selectedFrame;
        }

        public void BindElement(ElementSO element, Action<EncyclopediaBookSlotUI> onClick)
        {
            Kind = SlotKind.Element;
            Element = element;
            Compound = null;
            EntryKey = element != null ? element.Symbol : string.Empty;
            BindClick(onClick);
        }

        public void BindCompound(CompoundSO compound, Action<EncyclopediaBookSlotUI> onClick)
        {
            Kind = SlotKind.Compound;
            Element = null;
            Compound = compound;
            EntryKey = compound != null ? compound.ChemicalFormula : string.Empty;
            BindClick(onClick);
        }

        public void ApplyState(bool unlocked, bool selected)
        {
            IsUnlocked = unlocked;
            IsSelected = unlocked && selected;

            if (frameImage != null)
            {
                Sprite targetSprite = IsSelected && selectedFrameSprite != null
                    ? selectedFrameSprite
                    : defaultFrameSprite;
                if (targetSprite != null)
                    frameImage.sprite = targetSprite;
                frameImage.color = Color.white;
            }

            if (labelText != null)
            {
                if (!unlocked)
                {
                    labelText.text = "???";
                    labelText.fontSize = 68f;
                }
                else if (Kind == SlotKind.Element && Element != null)
                {
                    string sym = Element.Symbol ?? string.Empty;
                    labelText.text = sym;
                    labelText.fontSize = sym.Length <= 1 ? 82f : 62f;
                }
                else if (Kind == SlotKind.Compound && Compound != null)
                {
                    string raw = Compound.ChemicalFormula ?? string.Empty;
                    labelText.text = FormatFormulaRichText(raw);
                    if (raw.Length <= 3)
                        labelText.fontSize = 54f;
                    else if (raw.Length <= 4)
                        labelText.fontSize = 48f;
                    else
                        labelText.fontSize = 36f;
                }
            }
        }

        public static string FormatFormulaRichText(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return string.Empty;

            var sb = new StringBuilder(formula.Length * 2);
            bool inDigits = false;
            for (int i = 0; i < formula.Length; i++)
            {
                char c = formula[i];
                if (char.IsDigit(c))
                {
                    if (!inDigits)
                    {
                        sb.Append("<sub>");
                        inDigits = true;
                    }
                    sb.Append(c);
                }
                else
                {
                    if (inDigits)
                    {
                        sb.Append("</sub>");
                        inDigits = false;
                    }
                    sb.Append(c);
                }
            }

            if (inDigits)
                sb.Append("</sub>");

            return sb.ToString();
        }

        private void BindClick(Action<EncyclopediaBookSlotUI> onClick)
        {
            clickHandler = onClick;
            if (button == null || isBoundClick)
                return;

            button.onClick.AddListener(HandleClicked);
            isBoundClick = true;
        }

        private void HandleClicked()
        {
            clickHandler?.Invoke(this);
        }
    }
}

