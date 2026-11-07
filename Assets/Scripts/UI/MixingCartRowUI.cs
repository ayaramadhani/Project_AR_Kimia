using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ARKimia.Data;
using ARKimia.Gameplay;

namespace ARKimia.UI
{
    public class MixingCartRowUI : MonoBehaviour
    {
        [SerializeField] private Text symbolText;
        [SerializeField] private Text quantityText;
        [SerializeField] private Button minusButton;
        [SerializeField] private Image minusIcon;
        [SerializeField] private Button plusButton;
        [SerializeField] private Image plusIcon;

        private ElementSO boundElement;
        private Action<ElementSO, int> onAdjustRequested;
        private Coroutine popCoroutine;
        private int lastDisplayedQuantity = -1;

        private static readonly Color EnabledTint = Color.white;
        private static readonly Color DisabledTint = new Color(0.55f, 0.55f, 0.58f, 0.55f);

        public ElementSO BoundElement => boundElement;

        public void ConfigureReferences(
            Text symbol,
            Text quantity,
            Button minusBtn,
            Image minusImg,
            Button plusBtn,
            Image plusImg)
        {
            symbolText = symbol;
            quantityText = quantity;
            minusButton = minusBtn;
            minusIcon = minusImg;
            plusButton = plusBtn;
            plusIcon = plusImg;
        }

        public void Bind(ElementSO element, int initialQuantity, int minAllowedQuantity, Action<ElementSO, int> adjustCallback)
        {
            boundElement = element;
            onAdjustRequested = adjustCallback;
            lastDisplayedQuantity = -1;

            if (symbolText != null)
            {
                symbolText.text = element != null ? element.Symbol : "?";
            }

            if (minusButton != null)
            {
                minusButton.onClick.RemoveAllListeners();
                minusButton.onClick.AddListener(HandleMinusClicked);
            }

            if (plusButton != null)
            {
                plusButton.onClick.RemoveAllListeners();
                plusButton.onClick.AddListener(HandlePlusClicked);
            }

            SetState(initialQuantity, minAllowedQuantity, animatePop: false);
        }

        private void OnDisable()
        {
            if (popCoroutine != null)
            {
                StopCoroutine(popCoroutine);
                popCoroutine = null;
            }

            if (quantityText != null)
            {
                quantityText.rectTransform.localScale = Vector3.one;
            }
        }

        public void SetState(int quantity, int minAllowedQuantity, bool animatePop)
        {
            bool quantityChanged = lastDisplayedQuantity != quantity;
            lastDisplayedQuantity = quantity;

            if (quantityText != null)
            {
                quantityText.text = quantity.ToString();
            }

            bool canDecrement = quantity > minAllowedQuantity;
            if (minusButton != null)
            {
                minusButton.interactable = canDecrement;
            }

            if (minusIcon != null)
            {
                minusIcon.color = canDecrement ? EnabledTint : DisabledTint;
            }

            bool canIncrement = quantity < ElementCombinationManager.MaxQuantityPerElement;
            if (plusButton != null)
            {
                plusButton.interactable = canIncrement;
            }

            if (plusIcon != null)
            {
                plusIcon.color = canIncrement ? EnabledTint : DisabledTint;
            }

            if (animatePop && quantityChanged && Application.isPlaying && isActiveAndEnabled && quantityText != null)
            {
                if (popCoroutine != null)
                {
                    StopCoroutine(popCoroutine);
                }
                popCoroutine = StartCoroutine(PopQuantityRoutine());
            }
        }

        private void HandleMinusClicked()
        {
            if (boundElement != null)
            {
                onAdjustRequested?.Invoke(boundElement, -1);
            }
        }

        private void HandlePlusClicked()
        {
            if (boundElement != null)
            {
                onAdjustRequested?.Invoke(boundElement, 1);
            }
        }

        private IEnumerator PopQuantityRoutine()
        {
            RectTransform rt = quantityText.rectTransform;
            const float upDuration = 0.07f;
            const float downDuration = 0.10f;
            const float peakScale = 1.38f;

            float t = 0f;
            while (t < upDuration)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, peakScale, Mathf.Clamp01(t / upDuration));
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            t = 0f;
            while (t < downDuration)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(peakScale, 1f, Mathf.Clamp01(t / downDuration));
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            rt.localScale = Vector3.one;
            popCoroutine = null;
        }
    }
}
