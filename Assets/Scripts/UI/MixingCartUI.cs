using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ARKimia.Data;
using ARKimia.Gameplay;

namespace ARKimia.UI
{
    public class MixingCartUI : MonoBehaviour
    {
        [Header("Safe Area")]
        [SerializeField] private RectTransform safeAreaRect;

        [Header("Frame 49 - Minimized")]
        [SerializeField] private GameObject frame49MinimizedRoot;
        [SerializeField] private Button expandButton;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private Text badgeCountText;

        [Header("Frame 52 - Expanded")]
        [SerializeField] private GameObject frame52ExpandedRoot;
        [SerializeField] private Button minimizeButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private RectTransform rowsContainer;
        [SerializeField] private MixingCartRowUI rowTemplate;

        [Header("Gabungkan Button")]
        [SerializeField] private GameObject combineButtonRoot;
        [SerializeField] private Button combineButton;
        [SerializeField] private Image combineButtonImage;
        [SerializeField] private Image combineGlowImage;
        [SerializeField] private Sprite gabungActiveSprite;
        [SerializeField] private Sprite gabungDisabledSprite;

        [Header("Discovery Popup")]
        [SerializeField] private GameObject discoveryPopupRoot;
        [SerializeField] private Text popupTitleText;
        [SerializeField] private Text popupCompoundText;
        [SerializeField] private Text popupDescriptionText;
        [SerializeField] private Button popupContinueButton;

        [Header("New Element Toast")]
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private Text toastText;
        [SerializeField] private float toastDuration = 2.0f;

        private readonly Dictionary<ElementSO, MixingCartRowUI> activeRows = new Dictionary<ElementSO, MixingCartRowUI>();
        private readonly List<ElementSO> currentToastBatch = new List<ElementSO>();
        private float toastTimer;
        private ElementCombinationManager boundManager;
        private bool isMinimized;
        private Coroutine glowPulseCoroutine;
        private Coroutine panelPopCoroutine;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        public GameObject ToastRoot => toastRoot;
        public Text ToastText => toastText;
        public Button ResetButton => resetButton;

        public void ConfigureReferences(
            RectTransform safeArea,
            GameObject frame49Root,
            Button expandBtn,
            GameObject badgeObj,
            Text badgeText,
            GameObject frame52Root,
            Button minimizeBtn,
            RectTransform container,
            MixingCartRowUI template,
            GameObject combineRoot,
            Button combineBtn,
            Image combineImg,
            Image glowImg,
            Sprite activeSprite,
            Sprite disabledSprite,
            GameObject popupRoot,
            Text titleText,
            Text compoundText,
            Text descText,
            Button continueBtn,
            Button resetBtn = null,
            GameObject toastObj = null,
            Text toastTxt = null)
        {
            safeAreaRect = safeArea;
            frame49MinimizedRoot = frame49Root;
            expandButton = expandBtn;
            badgeRoot = badgeObj;
            badgeCountText = badgeText;
            frame52ExpandedRoot = frame52Root;
            minimizeButton = minimizeBtn;
            resetButton = resetBtn;
            rowsContainer = container;
            rowTemplate = template;
            combineButtonRoot = combineRoot;
            combineButton = combineBtn;
            combineButtonImage = combineImg;
            combineGlowImage = glowImg;
            gabungActiveSprite = activeSprite;
            gabungDisabledSprite = disabledSprite;
            discoveryPopupRoot = popupRoot;
            popupTitleText = titleText;
            popupCompoundText = compoundText;
            popupDescriptionText = descText;
            popupContinueButton = continueBtn;
            toastRoot = toastObj;
            toastText = toastTxt;
        }

        private void Awake()
        {
            if (rowTemplate != null)
            {
                rowTemplate.gameObject.SetActive(false);
            }

            if (expandButton != null)
            {
                expandButton.onClick.AddListener(HandleExpandClicked);
            }

            if (minimizeButton != null)
            {
                minimizeButton.onClick.AddListener(HandleMinimizeClicked);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(HandleResetClicked);
            }

            if (combineButton != null)
            {
                combineButton.onClick.AddListener(HandleCombineClicked);
            }

            if (popupContinueButton != null)
            {
                popupContinueButton.onClick.AddListener(HandlePopupContinueClicked);
            }

            if (discoveryPopupRoot != null)
            {
                discoveryPopupRoot.SetActive(false);
            }

            if (toastRoot != null)
            {
                toastRoot.SetActive(false);
            }

            ApplySafeAreaIfChanged(force: true);
        }

        private void OnEnable()
        {
            BindToManager();
            SyncAllFromManager();
        }

        private void Start()
        {
            if (boundManager == null)
            {
                BindToManager();
                SyncAllFromManager();
            }
        }

        private void OnDisable()
        {
            UnbindFromManager();
            StopGlowPulse();
        }

        private void Update()
        {
            ApplySafeAreaIfChanged(force: false);

            if (toastRoot != null && toastRoot.activeSelf)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f)
                {
                    HideToastImmediate();
                }
            }
        }

        public void HideToastImmediate()
        {
            toastTimer = 0f;
            currentToastBatch.Clear();
            if (toastRoot != null)
            {
                toastRoot.SetActive(false);
            }
        }

        private void BindToManager()
        {
            ElementCombinationManager target = ElementCombinationManager.Instance != null
                ? ElementCombinationManager.Instance
                : FindAnyObjectByType<ElementCombinationManager>();

            if (target == null || target == boundManager)
                return;

            UnbindFromManager();
            boundManager = target;
            boundManager.OnElementQuantityChanged += HandleElementQuantityChanged;
            boundManager.OnElementDiscovered += HandleElementDiscovered;
            boundManager.OnCombinationSuccess += HandleCombinationSuccess;
            boundManager.OnCartReset += HandleCartReset;
            boundManager.OnReactionLockChanged += HandleReactionLockChanged;
        }

        private void UnbindFromManager()
        {
            if (boundManager == null)
                return;

            boundManager.OnElementQuantityChanged -= HandleElementQuantityChanged;
            boundManager.OnElementDiscovered -= HandleElementDiscovered;
            boundManager.OnCombinationSuccess -= HandleCombinationSuccess;
            boundManager.OnCartReset -= HandleCartReset;
            boundManager.OnReactionLockChanged -= HandleReactionLockChanged;
            boundManager = null;
        }

        private void HandleElementDiscovered(ElementSO element)
        {
            if (element == null || toastRoot == null || toastText == null)
                return;

            if (!currentToastBatch.Contains(element))
            {
                currentToastBatch.Add(element);
            }

            if (currentToastBatch.Count == 1)
            {
                toastText.text = $"✨ Unsur Baru: {element.ElementName}";
            }
            else
            {
                toastText.text = $"✨ {currentToastBatch.Count} Unsur Baru Ditemukan!";
            }

            toastTimer = toastDuration;
            toastRoot.SetActive(true);
        }

        private void SyncAllFromManager()
        {
            ClearAllRowInstances();

            if (boundManager != null)
            {
                IReadOnlyList<ElementSO> order = boundManager.ActiveElementOrder;
                for (int i = 0; i < order.Count; i++)
                {
                    ElementSO element = order[i];
                    int qty = boundManager.GetQuantity(element);
                    if (qty > 0)
                    {
                        CreateOrUpdateRow(element, qty, boundManager.GetMinAllowedQuantity(element), animatePop: false);
                    }
                }

                foreach (var target in boundManager.TrackedTargets)
                {
                    if (target != null && target.ElementData != null && boundManager.HasZeroedPhysicalMarker(target.ElementData))
                    {
                        CreateOrUpdateRow(target.ElementData, 0, boundManager.GetMinAllowedQuantity(target.ElementData), animatePop: false);
                    }
                }
            }

            RefreshViewStates(animateExpand: false);
        }

        private void HandleElementQuantityChanged(ElementSO element, int quantity)
        {
            if (element == null)
                return;

            bool wasEmpty = activeRows.Count == 0;
            bool keepAtZero = quantity <= 0 && boundManager != null && boundManager.HasZeroedPhysicalMarker(element);

            if (quantity <= 0 && !keepAtZero)
            {
                if (activeRows.TryGetValue(element, out MixingCartRowUI row))
                {
                    activeRows.Remove(element);
                    if (row != null)
                    {
                        DestroyRowObject(row.gameObject);
                    }
                }
            }
            else
            {
                if (wasEmpty && discoveryPopupRoot != null && discoveryPopupRoot.activeSelf)
                {
                    discoveryPopupRoot.SetActive(false);
                }

                int displayQuantity = Mathf.Max(0, quantity);
                int minAllowed = boundManager != null ? boundManager.GetMinAllowedQuantity(element) : 0;
                CreateOrUpdateRow(element, displayQuantity, minAllowed, animatePop: true);
            }

            if (activeRows.Count == 0)
            {
                isMinimized = false;
            }

            RefreshViewStates(animateExpand: wasEmpty && activeRows.Count > 0);
        }

        private void CreateOrUpdateRow(ElementSO element, int quantity, int minAllowed, bool animatePop)
        {
            if (activeRows.TryGetValue(element, out MixingCartRowUI existingRow) && existingRow != null)
            {
                existingRow.SetState(quantity, minAllowed, animatePop);
                return;
            }

            if (rowTemplate == null || rowsContainer == null)
                return;

            MixingCartRowUI newRow = Instantiate(rowTemplate, rowsContainer);
            newRow.name = $"Row_{element.Symbol}";
            newRow.gameObject.SetActive(true);
            newRow.Bind(element, quantity, minAllowed, HandleRowAdjustRequested);
            activeRows[element] = newRow;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rowsContainer);
        }

        private void HandleRowAdjustRequested(ElementSO element, int delta)
        {
            if (boundManager != null)
            {
                boundManager.AddQuantity(element, delta);
            }
        }

        private void HandleResetClicked()
        {
            if (boundManager != null)
            {
                boundManager.ZeroAllElementsAndClearModels();
            }
        }

        private void HandleCartReset()
        {
            ClearAllRowInstances();
            isMinimized = false;
            RefreshViewStates(animateExpand: false);
        }

        private void HandleReactionLockChanged(bool locked)
        {
            RefreshCombineButtonState();
        }

        private void HandleCombinationSuccess(CompoundSO compound)
        {
            if (compound == null || discoveryPopupRoot == null)
                return;

            if (popupTitleText != null)
            {
                popupTitleText.text = "Senyawa Baru Ditemukan!";
            }

            if (popupCompoundText != null)
            {
                string prettyFormula = FormatFormulaRichText(compound.ChemicalFormula, 24);
                popupCompoundText.text = $"{prettyFormula}  —  {compound.CompoundName}";
            }

            if (popupDescriptionText != null)
            {
                popupDescriptionText.text = string.IsNullOrWhiteSpace(compound.Description)
                    ? $"Berhasil membentuk senyawa {compound.CompoundName}."
                    : compound.Description;
            }

            discoveryPopupRoot.SetActive(true);

            if (popupContinueButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(popupContinueButton.gameObject);
            }
        }

        private void HandleMinimizeClicked()
        {
            if (activeRows.Count == 0)
                return;

            SyncPanelPositions(fromExpandedToMinimized: true);
            isMinimized = true;
            RefreshViewStates(animateExpand: false);
        }

        private void HandleExpandClicked()
        {
            if (activeRows.Count == 0)
                return;

            var draggable = frame49MinimizedRoot != null ? frame49MinimizedRoot.GetComponent<UIDraggablePanel>() : null;
            if (draggable != null && draggable.IsDragging)
                return;

            SyncPanelPositions(fromExpandedToMinimized: false);
            isMinimized = false;
            RefreshViewStates(animateExpand: true);
        }

        private void SyncPanelPositions(bool fromExpandedToMinimized)
        {
            if (frame49MinimizedRoot == null || frame52ExpandedRoot == null)
                return;

            var f49Rt = frame49MinimizedRoot.GetComponent<RectTransform>();
            var f52Rt = frame52ExpandedRoot.GetComponent<RectTransform>();
            if (f49Rt == null || f52Rt == null)
                return;

            if (fromExpandedToMinimized)
            {
                f49Rt.anchoredPosition = f52Rt.anchoredPosition;
                frame49MinimizedRoot.GetComponent<UIDraggablePanel>()?.ClampToBoundary();
            }
            else
            {
                f52Rt.anchoredPosition = f49Rt.anchoredPosition;
                frame52ExpandedRoot.GetComponent<UIDraggablePanel>()?.ClampToBoundary();
            }
        }

        private void HandleCombineClicked()
        {
            if (boundManager != null)
            {
                boundManager.TryCombine(out _);
            }
        }

        private void HandlePopupContinueClicked()
        {
            if (discoveryPopupRoot != null)
            {
                discoveryPopupRoot.SetActive(false);
            }

            if (boundManager != null)
            {
                boundManager.UnlockReactionCamera();
            }
        }

        private void RefreshViewStates(bool animateExpand)
        {
            int count = activeRows.Count;
            int activeTypeCount = boundManager != null ? boundManager.ActiveElementTypeCount : count;
            bool showMinimized = count > 0 && isMinimized;
            bool showExpanded = count > 0 && !isMinimized;

            if (frame49MinimizedRoot != null)
            {
                frame49MinimizedRoot.SetActive(showMinimized);
            }

            if (badgeRoot != null)
            {
                badgeRoot.SetActive(activeTypeCount > 0);
            }

            if (badgeCountText != null)
            {
                badgeCountText.text = activeTypeCount.ToString();
            }

            if (frame52ExpandedRoot != null)
            {
                frame52ExpandedRoot.SetActive(showExpanded);
                if (showExpanded && animateExpand && Application.isPlaying && isActiveAndEnabled)
                {
                    if (panelPopCoroutine != null)
                    {
                        StopCoroutine(panelPopCoroutine);
                    }
                    panelPopCoroutine = StartCoroutine(PopPanelRoutine(frame52ExpandedRoot.transform as RectTransform));
                }
            }

            RefreshCombineButtonState();
        }

        private void RefreshCombineButtonState()
        {
            if (combineButton == null || combineButtonRoot == null)
                return;

            bool hasRelatedRecipe = boundManager != null && !boundManager.IsReactionLocked && boundManager.HasRelatedRecipeForActiveElements();
            CompoundSO validMatch = boundManager != null && !boundManager.IsReactionLocked
                ? boundManager.EvaluateCurrentCombination()
                : null;

            bool shouldShowButton = activeRows.Count > 0 && (hasRelatedRecipe || validMatch != null);
            combineButtonRoot.SetActive(shouldShowButton);

            if (!shouldShowButton)
            {
                StopGlowPulse();
                return;
            }

            bool isReady = validMatch != null;
            combineButton.interactable = isReady;

            if (combineButtonImage != null)
            {
                Sprite targetSprite = isReady ? gabungActiveSprite : gabungDisabledSprite;
                if (targetSprite != null)
                {
                    combineButtonImage.sprite = targetSprite;
                }
            }

            if (isReady)
            {
                if (combineGlowImage != null)
                {
                    combineGlowImage.gameObject.SetActive(true);
                }
                if (glowPulseCoroutine == null && Application.isPlaying && isActiveAndEnabled)
                {
                    glowPulseCoroutine = StartCoroutine(GlowPulseRoutine());
                }
            }
            else
            {
                StopGlowPulse();
            }
        }

        private void StopGlowPulse()
        {
            if (glowPulseCoroutine != null)
            {
                StopCoroutine(glowPulseCoroutine);
                glowPulseCoroutine = null;
            }

            if (combineGlowImage != null)
            {
                combineGlowImage.gameObject.SetActive(false);
            }

            if (combineButton != null)
            {
                combineButton.transform.localScale = Vector3.one;
            }
        }

        private IEnumerator GlowPulseRoutine()
        {
            Transform btnTransform = combineButton != null ? combineButton.transform : null;
            float phase = 0f;

            while (true)
            {
                phase += Time.unscaledDeltaTime * 4.2f;
                float wave = (Mathf.Sin(phase) + 1f) * 0.5f;

                if (btnTransform != null)
                {
                    float s = Mathf.Lerp(1f, 1.045f, wave);
                    btnTransform.localScale = new Vector3(s, s, 1f);
                }

                if (combineGlowImage != null)
                {
                    Color c = combineGlowImage.color;
                    c.a = Mathf.Lerp(0.32f, 0.85f, wave);
                    combineGlowImage.color = c;
                }

                yield return null;
            }
        }

        private IEnumerator PopPanelRoutine(RectTransform target)
        {
            if (target == null)
                yield break;

            const float duration = 0.14f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                float s = Mathf.Lerp(0.88f, 1f, eased);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
            panelPopCoroutine = null;
        }

        private void ClearAllRowInstances()
        {
            foreach (KeyValuePair<ElementSO, MixingCartRowUI> kvp in activeRows)
            {
                if (kvp.Value != null)
                {
                    DestroyRowObject(kvp.Value.gameObject);
                }
            }
            activeRows.Clear();
        }

        private static void DestroyRowObject(GameObject obj)
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        private void ApplySafeAreaIfChanged(bool force)
        {
            if (safeAreaRect == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safe = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == lastSafeArea && screenSize == lastScreenSize)
                return;

            lastSafeArea = safe;
            lastScreenSize = screenSize;

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
        }

        private static string FormatFormulaRichText(string formula, int subscriptFontSize)
        {
            if (string.IsNullOrEmpty(formula))
                return string.Empty;

            var sb = new StringBuilder(formula.Length * 2);
            for (int i = 0; i < formula.Length; i++)
            {
                char ch = formula[i];
                if (char.IsDigit(ch) && i > 0)
                {
                    sb.Append("<size=").Append(subscriptFontSize).Append('>').Append(ch).Append("</size>");
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }
}
