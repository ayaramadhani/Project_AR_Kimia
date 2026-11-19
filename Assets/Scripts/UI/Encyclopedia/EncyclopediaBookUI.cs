using System;
using System.Collections.Generic;
using ARKimia.Data;
using ARKimia.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ARKimia.UI
{
    public class EncyclopediaBookUI : MonoBehaviour
    {
        public enum BookTab
        {
            Index,
            Glosarium
        }

        [Serializable]
        public class GlossaryTerm
        {
            public string term;
            [TextArea(2, 6)] public string definition;
        }

        private static readonly string[] CanonicalElementOrder =
        {
            "H", "O", "C", "N", "Na", "Cl", "S", "Mg", "Ca", "Fe"
        };

        private static readonly string[] CanonicalCompoundOrder =
        {
            "H2O", "NaCl", "CO2", "H2SO4", "Fe2O3"
        };

        [Header("Root & Navigation")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button backdropButton;
        [SerializeField] private Button closeButton;

        [Header("Bookmark Tabs")]
        [SerializeField] private RectTransform bookmarkIndexRect;
        [SerializeField] private Button bookmarkIndexButton;
        [SerializeField] private RectTransform bookmarkGlosariumRect;
        [SerializeField] private Button bookmarkGlosariumButton;
        [SerializeField] private float activeBookmarkY = 70f;
        [SerializeField] private float inactiveBookmarkY = 31f;
        [SerializeField] private RectTransform cornerRibbonRect;
        [SerializeField] private Image cornerRibbonImage;
        [SerializeField] private TMP_Text cornerRibbonText;
        [SerializeField] private Sprite ribbonIndexSprite;
        [SerializeField] private Sprite ribbonGlosariumSprite;

        [Header("Independent Scroll Views")]
        [SerializeField] private ScrollRect leftScrollRect;
        [SerializeField] private ScrollRect rightScrollRect;

        [Header("Left Page Containers")]
        [SerializeField] private GameObject leftIndexGridRoot;
        [SerializeField] private RectTransform indexSlotsParent;
        [SerializeField] private EncyclopediaBookSlotUI indexSlotTemplate;
        [SerializeField] private GameObject leftGlossaryListRoot;
        [SerializeField] private RectTransform glossaryItemsParent;
        [SerializeField] private GameObject glossarySectionTemplate;
        [SerializeField] private EncyclopediaGlossaryItemUI glossaryItemTemplate;

        [Header("Right Page Display")]
        [SerializeField] private GameObject rightPromptRoot;
        [SerializeField] private TMP_Text rightPromptText;
        [SerializeField] private GameObject rightDetailRoot;
        [SerializeField] private TMP_Text rightDetailBadgeText;
        [SerializeField] private TMP_Text rightDetailTitleText;
        [SerializeField] private TMP_Text rightDetailSubtitleText;
        [SerializeField] private TMP_Text rightDetailBodyText;
        [SerializeField] private GameObject rightFunFactBox;
        [SerializeField] private TMP_Text rightFunFactText;

        [Header("Data")]
        [SerializeField] private ChemistryDatabase database;
        [SerializeField] private List<EncyclopediaEntry> entries = new List<EncyclopediaEntry>();
        [SerializeField] private List<GlossaryTerm> glossaryTerms = new List<GlossaryTerm>();

        private readonly List<EncyclopediaBookSlotUI> spawnedSlots = new List<EncyclopediaBookSlotUI>();
        private readonly List<EncyclopediaGlossaryItemUI> spawnedGlossaryItems = new List<EncyclopediaGlossaryItemUI>();
        private readonly Dictionary<string, EncyclopediaEntry> entryLookup =
            new Dictionary<string, EncyclopediaEntry>(StringComparer.OrdinalIgnoreCase);

        private ElementCombinationManager boundManager;
        private BookTab currentTab = BookTab.Index;
        private EncyclopediaBookSlotUI selectedSlot;
        private int selectedGlossaryIndex = -1;
        private bool slotsBuilt;
        private bool glossaryBuilt;
        private bool buttonsWired;

        public GameObject PanelRoot => panelRoot;
        public BookTab CurrentTab => currentTab;
        public ScrollRect LeftScrollRect => leftScrollRect;
        public ScrollRect RightScrollRect => rightScrollRect;
        public TMP_Text CornerRibbonText => cornerRibbonText;
        public GameObject RightPromptRoot => rightPromptRoot;
        public TMP_Text RightPromptText => rightPromptText;
        public GameObject RightDetailRoot => rightDetailRoot;
        public TMP_Text RightDetailTitleText => rightDetailTitleText;
        public TMP_Text RightDetailBodyText => rightDetailBodyText;
        public RectTransform IndexSlotsParent => indexSlotsParent;
        public RectTransform GlossaryItemsParent => glossaryItemsParent;
        public RectTransform BookmarkIndexRect => bookmarkIndexRect;
        public RectTransform BookmarkGlosariumRect => bookmarkGlosariumRect;
        public Button BookmarkIndexButton => bookmarkIndexButton;
        public Button BookmarkGlosariumButton => bookmarkGlosariumButton;
        public Button CloseButton => closeButton;
        public EncyclopediaBookSlotUI SelectedSlot => selectedSlot;
        public IReadOnlyList<EncyclopediaBookSlotUI> SpawnedSlots => spawnedSlots;
        public IReadOnlyList<EncyclopediaGlossaryItemUI> SpawnedGlossaryItems => spawnedGlossaryItems;

        public void ConfigureReferences(
            GameObject modalRoot,
            Button backdropBtn,
            Button closeBtn,
            RectTransform idxBookmarkRt,
            Button idxBookmarkBtn,
            RectTransform glosBookmarkRt,
            Button glosBookmarkBtn,
            RectTransform ribbonRt,
            Image ribbonImg,
            TMP_Text ribbonTxt,
            Sprite indexRibbonSpr,
            Sprite glosariumRibbonSpr,
            ScrollRect leftScroll,
            ScrollRect rightScroll,
            GameObject indexGridObj,
            RectTransform indexParent,
            EncyclopediaBookSlotUI slotTemplate,
            GameObject glossaryListObj,
            RectTransform glossaryParent,
            GameObject glossarySecTemplate,
            EncyclopediaGlossaryItemUI glossaryTemplate,
            GameObject promptObj,
            TMP_Text promptTxt,
            GameObject detailObj,
            TMP_Text badgeTxt,
            TMP_Text titleTxt,
            TMP_Text subtitleTxt,
            TMP_Text bodyTxt,
            GameObject funFactBoxObj,
            TMP_Text funFactTxt,
            ChemistryDatabase db,
            List<EncyclopediaEntry> entryAssets)
        {
            panelRoot = modalRoot;
            backdropButton = backdropBtn;
            closeButton = closeBtn;
            bookmarkIndexRect = idxBookmarkRt;
            bookmarkIndexButton = idxBookmarkBtn;
            bookmarkGlosariumRect = glosBookmarkRt;
            bookmarkGlosariumButton = glosBookmarkBtn;
            cornerRibbonRect = ribbonRt;
            cornerRibbonImage = ribbonImg;
            cornerRibbonText = ribbonTxt;
            ribbonIndexSprite = indexRibbonSpr;
            ribbonGlosariumSprite = glosariumRibbonSpr;
            leftScrollRect = leftScroll;
            rightScrollRect = rightScroll;
            leftIndexGridRoot = indexGridObj;
            indexSlotsParent = indexParent;
            indexSlotTemplate = slotTemplate;
            leftGlossaryListRoot = glossaryListObj;
            glossaryItemsParent = glossaryParent;
            glossarySectionTemplate = glossarySecTemplate;
            glossaryItemTemplate = glossaryTemplate;
            rightPromptRoot = promptObj;
            rightPromptText = promptTxt;
            rightDetailRoot = detailObj;
            rightDetailBadgeText = badgeTxt;
            rightDetailTitleText = titleTxt;
            rightDetailSubtitleText = subtitleTxt;
            rightDetailBodyText = bodyTxt;
            rightFunFactBox = funFactBoxObj;
            rightFunFactText = funFactTxt;
            database = db;
            entries = entryAssets ?? new List<EncyclopediaEntry>();
        }

        private void Awake()
        {
            WireButtons();
            BuildEntryLookup();
            EnsureDefaultGlossaryTerms();
        }

        private void OnEnable()
        {
            WireButtons();
            QuickdropEvents.ElementInfoRequested += HandleElementInfoRequested;
            QuickdropEvents.CompoundInfoRequested += HandleCompoundInfoRequested;
            TryBindManager();
            RefreshIndexSlotStates();
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            QuickdropEvents.ElementInfoRequested -= HandleElementInfoRequested;
            QuickdropEvents.CompoundInfoRequested -= HandleCompoundInfoRequested;
            UnbindManager();
        }

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
                return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void EnsureInitialized()
        {
            WireButtons();
            BuildEntryLookup();
            EnsureDefaultGlossaryTerms();
            EnsureDatabaseLoaded();
            TryBindManager();
            BuildIndexSlotsIfNeeded();
            BuildGlossaryItemsIfNeeded();
            RefreshTabVisuals();
            RefreshIndexSlotStates();
            RefreshRightPage();
        }

        public void OpenIndex() => Open(BookTab.Index);

        public void OpenGlossary() => Open(BookTab.Glosarium);

        public void Open(BookTab tab = BookTab.Index)
        {
            EnsureInitialized();
            if (panelRoot != null)
                panelRoot.SetActive(true);

            FitBookToViewport();
            SetTab(tab);
        }

        public void ToggleOpen()
        {
            if (panelRoot != null && panelRoot.activeSelf)
                Close();
            else
                Open(currentTab);
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void SetTab(BookTab tab)
        {
            EnsureInitialized();
            currentTab = tab;

            if (tab == BookTab.Index)
                selectedGlossaryIndex = -1;
            else
                selectedSlot = null;

            RefreshTabVisuals();
            RefreshIndexSlotStates();
            RefreshGlossaryStates();
            RefreshRightPage();

            ResetScrollToTop(leftScrollRect);
            ResetScrollToTop(rightScrollRect);
        }

        public void OpenElement(ElementSO element)
        {
            if (element == null)
                return;

            EnsureInitialized();
            if (boundManager != null)
                boundManager.MarkElementRead(element);

            if (panelRoot != null)
                panelRoot.SetActive(true);

            FitBookToViewport();
            currentTab = BookTab.Index;
            RefreshTabVisuals();

            selectedSlot = FindSlotByElement(element);
            RefreshIndexSlotStates();
            RefreshRightPage();

            ResetScrollToTop(leftScrollRect);
            ResetScrollToTop(rightScrollRect);
        }

        public void OpenCompound(CompoundSO compound)
        {
            if (compound == null)
                return;

            EnsureInitialized();
            if (boundManager != null)
                boundManager.MarkCompoundRead(compound);

            if (panelRoot != null)
                panelRoot.SetActive(true);

            FitBookToViewport();
            currentTab = BookTab.Index;
            RefreshTabVisuals();

            selectedSlot = FindSlotByCompound(compound);
            RefreshIndexSlotStates();
            RefreshRightPage();

            ResetScrollToTop(leftScrollRect);
            ResetScrollToTop(rightScrollRect);
        }

        public void RefreshIndexSlotStates()
        {
            TryBindManager();
            if (selectedSlot != null && !IsSlotUnlocked(selectedSlot))
                selectedSlot = null;

            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                EncyclopediaBookSlotUI slot = spawnedSlots[i];
                if (slot == null)
                    continue;

                bool unlocked = IsSlotUnlocked(slot);
                bool selected = unlocked && slot == selectedSlot;
                slot.ApplyState(unlocked, selected);
            }
        }

        private void WireButtons()
        {
            if (buttonsWired)
                return;

            if (bookmarkIndexButton != null)
                bookmarkIndexButton.onClick.AddListener(() => SetTab(BookTab.Index));
            if (bookmarkGlosariumButton != null)
                bookmarkGlosariumButton.onClick.AddListener(() => SetTab(BookTab.Glosarium));
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (backdropButton != null)
                backdropButton.onClick.AddListener(Close);

            buttonsWired = true;
        }

        private void EnsureDatabaseLoaded()
        {
            if (database == null)
                database = Resources.Load<ChemistryDatabase>("ChemistryData/ChemistryDatabase");
        }

        private void TryBindManager()
        {
            ElementCombinationManager mgr = boundManager != null
                ? boundManager
                : (ElementCombinationManager.Instance != null
                    ? ElementCombinationManager.Instance
                    : FindAnyObjectByType<ElementCombinationManager>());

            if (mgr == null || mgr == boundManager)
                return;

            boundManager = mgr;
            boundManager.OnElementDiscovered += HandleElementDiscovered;
            boundManager.OnCompoundDiscovered += HandleCompoundDiscovered;
        }

        private void UnbindManager()
        {
            if (boundManager == null)
                return;

            boundManager.OnElementDiscovered -= HandleElementDiscovered;
            boundManager.OnCompoundDiscovered -= HandleCompoundDiscovered;
            boundManager = null;
        }

        private void HandleElementDiscovered(ElementSO _)
        {
            EnsureInitialized();
            RefreshIndexSlotStates();
        }

        private void HandleCompoundDiscovered(CompoundSO _)
        {
            EnsureInitialized();
            RefreshIndexSlotStates();
        }

        private void HandleElementInfoRequested(ElementSO element)
        {
            OpenElement(element);
        }

        private void HandleCompoundInfoRequested(CompoundSO compound)
        {
            OpenCompound(compound);
        }

        private void BuildEntryLookup()
        {
            entryLookup.Clear();
            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                EncyclopediaEntry e = entries[i];
                if (e != null && !string.IsNullOrEmpty(e.key))
                    entryLookup[e.key] = e;
            }
        }

        private void EnsureDefaultGlossaryTerms()
        {
            if (glossaryTerms != null && glossaryTerms.Count > 0)
                return;

            glossaryTerms = new List<GlossaryTerm>
            {
                new GlossaryTerm
                {
                    term = "Asam",
                    definition = "Zat kimia yang melepaskan ion hidrogen (H+) saat dilarutkan dalam air dan biasanya memiliki pH kurang dari 7, seperti asam sulfat (H2SO4)."
                },
                new GlossaryTerm
                {
                    term = "Atom",
                    definition = "Satuan terkecil dari suatu unsur kimia yang masih mempertahankan sifat unsur tersebut, tersusun dari inti atom (proton dan neutron) serta elektron di sekitarnya."
                },
                new GlossaryTerm
                {
                    term = "Elektron",
                    definition = "Partikel subatom bermuatan negatif yang bergerak mengelilingi inti atom dalam kulit atau orbital elektron dan berperan dalam pembentukan ikatan kimia."
                },
                new GlossaryTerm
                {
                    term = "Garam",
                    definition = "Senyawa ion yang terbentuk dari tarik-menarik ion positif (kation logam) dan ion negatif (anion), contohnya natrium klorida (NaCl)."
                },
                new GlossaryTerm
                {
                    term = "Ikatan Ion",
                    definition = "Ikatan kimia yang terbentuk karena serah terima elektron antara atom logam yang melepas elektron dan atom nonlogam yang menerima elektron."
                },
                new GlossaryTerm
                {
                    term = "Ikatan Kovalen",
                    definition = "Ikatan kimia yang terjadi ketika dua atom nonlogam saling berbagi pasangan elektron agar mencapai konfigurasi stabil, seperti pada molekul air (H2O)."
                },
                new GlossaryTerm
                {
                    term = "Massa Atom",
                    definition = "Massa rata-rata sebuah atom suatu unsur yang dinyatakan dalam satuan massa atom (sma), dipengaruhi oleh jumlah proton dan neutron dalam intinya."
                },
                new GlossaryTerm
                {
                    term = "Molekul",
                    definition = "Gabungan dua atau lebih atom yang terikat kuat secara kimia sebagai satu kesatuan netral, misalnya molekul air (H2O) dan karbon dioksida (CO2)."
                },
                new GlossaryTerm
                {
                    term = "Neutron",
                    definition = "Partikel subatom tidak bermuatan (netral) yang berada di dalam inti atom bersama proton."
                },
                new GlossaryTerm
                {
                    term = "Nomor Atom",
                    definition = "Angka yang menunjukkan jumlah proton dalam inti suatu atom dan menjadi identitas unik setiap unsur kimia dalam tabel periodik."
                },
                new GlossaryTerm
                {
                    term = "Oksida",
                    definition = "Senyawa kimia yang terbentuk dari gabungan unsur oksigen dengan satu unsur lain, misalnya besi(III) oksida (Fe2O3) atau karat besi."
                },
                new GlossaryTerm
                {
                    term = "Proton",
                    definition = "Partikel subatom bermuatan positif (+) yang terdapat di dalam inti atom."
                },
                new GlossaryTerm
                {
                    term = "Reaksi Kimia",
                    definition = "Proses ketika satu atau lebih zat pereaksi berubah menjadi zat baru (senyawa) dengan susunan atom dan sifat yang berbeda."
                },
                new GlossaryTerm
                {
                    term = "Rumus Kimia",
                    definition = "Notasi singkat menggunakan lambang unsur dan angka indeks bawah (subskrip) untuk menunjukkan jenis serta jumlah atom penyusun senyawa."
                },
                new GlossaryTerm
                {
                    term = "Senyawa",
                    definition = "Zat murni yang tersusun dari dua atau lebih unsur kimia berbeda yang bergabung secara kimia dengan perbandingan massa dan atom yang tetap."
                },
                new GlossaryTerm
                {
                    term = "Unsur",
                    definition = "Zat murni paling sederhana yang tidak dapat diuraikan lagi menjadi zat lain melalui reaksi kimia biasa, seperti Hidrogen (H) dan Oksigen (O)."
                }
            };

            glossaryTerms.Sort((a, b) => string.Compare(a.term, b.term, StringComparison.OrdinalIgnoreCase));
        }

        private void BuildIndexSlotsIfNeeded()
        {
            if (slotsBuilt || indexSlotsParent == null || indexSlotTemplate == null || database == null)
                return;

            indexSlotTemplate.gameObject.SetActive(false);

            List<ElementSO> orderedElements = BuildOrderedElements();
            for (int i = 0; i < orderedElements.Count; i++)
            {
                EncyclopediaBookSlotUI slot = Instantiate(indexSlotTemplate, indexSlotsParent);
                slot.name = $"Slot_Element_{orderedElements[i].Symbol}";
                slot.gameObject.SetActive(true);
                slot.BindElement(orderedElements[i], HandleSlotClicked);
                spawnedSlots.Add(slot);
            }

            List<CompoundSO> orderedCompounds = BuildOrderedCompounds();
            for (int i = 0; i < orderedCompounds.Count; i++)
            {
                EncyclopediaBookSlotUI slot = Instantiate(indexSlotTemplate, indexSlotsParent);
                slot.name = $"Slot_Compound_{orderedCompounds[i].ChemicalFormula}";
                slot.gameObject.SetActive(true);
                slot.BindCompound(orderedCompounds[i], HandleSlotClicked);
                spawnedSlots.Add(slot);
            }

            slotsBuilt = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(indexSlotsParent);
        }

        private List<ElementSO> BuildOrderedElements()
        {
            var remaining = new List<ElementSO>(database.Elements);
            var ordered = new List<ElementSO>(remaining.Count);

            for (int i = 0; i < CanonicalElementOrder.Length; i++)
            {
                string sym = CanonicalElementOrder[i];
                int idx = remaining.FindIndex(e => e != null && string.Equals(e.Symbol, sym, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    ordered.Add(remaining[idx]);
                    remaining.RemoveAt(idx);
                }
            }

            for (int i = 0; i < remaining.Count; i++)
            {
                if (remaining[i] != null)
                    ordered.Add(remaining[i]);
            }

            return ordered;
        }

        private List<CompoundSO> BuildOrderedCompounds()
        {
            var remaining = new List<CompoundSO>(database.Compounds);
            var ordered = new List<CompoundSO>(remaining.Count);

            for (int i = 0; i < CanonicalCompoundOrder.Length; i++)
            {
                string formula = CanonicalCompoundOrder[i];
                int idx = remaining.FindIndex(c => c != null && string.Equals(c.ChemicalFormula, formula, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    ordered.Add(remaining[idx]);
                    remaining.RemoveAt(idx);
                }
            }

            for (int i = 0; i < remaining.Count; i++)
            {
                if (remaining[i] != null)
                    ordered.Add(remaining[i]);
            }

            return ordered;
        }

        private void BuildGlossaryItemsIfNeeded()
        {
            if (glossaryBuilt || glossaryItemsParent == null || glossaryItemTemplate == null)
                return;

            glossaryItemTemplate.gameObject.SetActive(false);
            if (glossarySectionTemplate != null)
                glossarySectionTemplate.SetActive(false);

            glossaryTerms.Sort((a, b) => string.Compare(a.term, b.term, StringComparison.OrdinalIgnoreCase));

            char lastLetter = '\0';
            for (int i = 0; i < glossaryTerms.Count; i++)
            {
                string termName = glossaryTerms[i].term ?? string.Empty;
                char initial = termName.Length > 0 ? char.ToUpperInvariant(termName[0]) : '#';

                if (glossarySectionTemplate != null && initial != lastLetter)
                {
                    lastLetter = initial;
                    GameObject sectionObj = Instantiate(glossarySectionTemplate, glossaryItemsParent);
                    sectionObj.name = $"Section_{initial}";
                    sectionObj.SetActive(true);
                    TMP_Text letterTxt = sectionObj.GetComponentInChildren<TMP_Text>(true);
                    if (letterTxt != null)
                        letterTxt.text = initial.ToString();
                }

                EncyclopediaGlossaryItemUI item = Instantiate(glossaryItemTemplate, glossaryItemsParent);
                item.name = $"Glossary_{termName}";
                item.gameObject.SetActive(true);
                item.Bind(termName, i, HandleGlossaryClicked);
                spawnedGlossaryItems.Add(item);
            }

            glossaryBuilt = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(glossaryItemsParent);
        }

        private void RefreshTabVisuals()
        {
            bool isGlossary = currentTab == BookTab.Glosarium;

            if (cornerRibbonText != null)
                cornerRibbonText.text = isGlossary ? "Glosarium" : "Index";

            if (cornerRibbonImage != null)
            {
                Sprite targetBanner = isGlossary && ribbonGlosariumSprite != null
                    ? ribbonGlosariumSprite
                    : ribbonIndexSprite;
                if (targetBanner != null)
                    cornerRibbonImage.sprite = targetBanner;
            }

            if (cornerRibbonRect != null)
            {
                if (isGlossary)
                {
                    cornerRibbonRect.anchoredPosition = new Vector2(-82f, 52f);
                    cornerRibbonRect.sizeDelta = new Vector2(424.51f, 238.96f);
                }
                else
                {
                    cornerRibbonRect.anchoredPosition = new Vector2(-57.26f, 20.92f);
                    cornerRibbonRect.sizeDelta = new Vector2(293.9f, 188.98f);
                }
            }

            if (leftIndexGridRoot != null)
                leftIndexGridRoot.SetActive(!isGlossary);
            if (leftGlossaryListRoot != null)
                leftGlossaryListRoot.SetActive(isGlossary);

            if (leftScrollRect != null)
            {
                RectTransform targetContent = isGlossary
                    ? glossaryItemsParent
                    : indexSlotsParent;
                if (targetContent != null)
                    leftScrollRect.content = targetContent;
            }

            UpdateBookmarkPose(bookmarkIndexRect, !isGlossary);
            UpdateBookmarkPose(bookmarkGlosariumRect, isGlossary);
        }

        private void UpdateBookmarkPose(RectTransform bookmarkRt, bool isActive)
        {
            if (bookmarkRt == null)
                return;

            Vector2 pos = bookmarkRt.anchoredPosition;
            pos.y = isActive ? activeBookmarkY : inactiveBookmarkY;
            bookmarkRt.anchoredPosition = pos;
        }

        private bool IsSlotUnlocked(EncyclopediaBookSlotUI slot)
        {
            if (slot == null || boundManager == null)
                return false;

            if (slot.Kind == EncyclopediaBookSlotUI.SlotKind.Element)
                return boundManager.IsElementDiscovered(slot.Element);

            return boundManager.IsCompoundDiscovered(slot.Compound);
        }

        private EncyclopediaBookSlotUI FindSlotByElement(ElementSO element)
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null && spawnedSlots[i].Element == element)
                    return spawnedSlots[i];
            }
            return null;
        }

        private EncyclopediaBookSlotUI FindSlotByCompound(CompoundSO compound)
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null && spawnedSlots[i].Compound == compound)
                    return spawnedSlots[i];
            }
            return null;
        }

        private void HandleSlotClicked(EncyclopediaBookSlotUI slot)
        {
            if (slot == null || !IsSlotUnlocked(slot))
                return;

            selectedSlot = slot;
            if (slot.Kind == EncyclopediaBookSlotUI.SlotKind.Element && boundManager != null)
                boundManager.MarkElementRead(slot.Element);
            else if (slot.Kind == EncyclopediaBookSlotUI.SlotKind.Compound && boundManager != null)
                boundManager.MarkCompoundRead(slot.Compound);

            RefreshIndexSlotStates();
            RefreshRightPage();
            ResetScrollToTop(rightScrollRect);
        }

        private void HandleGlossaryClicked(int index)
        {
            if (index < 0 || index >= glossaryTerms.Count)
                return;

            selectedGlossaryIndex = index;
            RefreshGlossaryStates();
            RefreshRightPage();
            ResetScrollToTop(rightScrollRect);
        }

        private static void ResetScrollToTop(ScrollRect scrollRect)
        {
            if (scrollRect == null)
                return;

            scrollRect.StopMovement();
            if (scrollRect.content != null)
            {
                if (scrollRect.transform is RectTransform rt && rt.lossyScale.x > 0.0001f)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                }

                scrollRect.content.anchoredPosition = Vector2.zero;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (panelRoot != null && panelRoot.activeSelf)
                FitBookToViewport();
        }

        private void FitBookToViewport()
        {
            if (panelRoot == null)
                return;

            Transform bookTransform = panelRoot.transform.Find("BookRoot");
            if (!(bookTransform is RectTransform bookRt) || !(panelRoot.transform is RectTransform overlayRt))
                return;

            Rect rect = overlayRt.rect;
            if (rect.width < 100f || rect.height < 100f)
                return;

            // BookRoot spans +70 bookmark overhang above and 903px down (~1010px with safety padding)
            float fitH = (rect.height - 28f) / 1010f;
            float fitW = (rect.width - 48f) / 1920f;
            float scale = Mathf.Clamp(Mathf.Min(fitH, fitW), 0.50f, 0.86f);
            bookRt.localScale = new Vector3(scale, scale, 1f);
            bookRt.anchoredPosition = new Vector2(18f * (scale / 0.86f), -22f * (scale / 0.86f));
        }

        private void RefreshGlossaryStates()
        {
            for (int i = 0; i < spawnedGlossaryItems.Count; i++)
            {
                if (spawnedGlossaryItems[i] != null)
                    spawnedGlossaryItems[i].SetSelected(i == selectedGlossaryIndex);
            }
        }

        private void RefreshRightPage()
        {
            if (currentTab == BookTab.Index)
            {
                if (selectedSlot == null || !IsSlotUnlocked(selectedSlot))
                {
                    ShowRightPrompt("Pilihlah Unsur/Senyawa yang ingin dilihat informasinya!");
                    return;
                }

                ShowSelectedIndexDetail(selectedSlot);
                return;
            }

            if (selectedGlossaryIndex < 0 || selectedGlossaryIndex >= glossaryTerms.Count)
            {
                ShowRightPrompt("Pilihlah istilah yang ingin dilihat penjelasannya!");
                return;
            }

            ShowSelectedGlossaryDetail(glossaryTerms[selectedGlossaryIndex]);
        }

        private void ShowRightPrompt(string message)
        {
            if (rightPromptRoot != null)
                rightPromptRoot.SetActive(true);
            if (rightDetailRoot != null)
                rightDetailRoot.SetActive(false);
            if (rightPromptText != null)
                rightPromptText.text = message;
        }

        private void ShowSelectedIndexDetail(EncyclopediaBookSlotUI slot)
        {
            if (rightPromptRoot != null)
                rightPromptRoot.SetActive(false);
            if (rightDetailRoot != null)
                rightDetailRoot.SetActive(true);

            string badge;
            string title;
            string subtitle;
            string body;
            string funFact;

            if (slot.Kind == EncyclopediaBookSlotUI.SlotKind.Element && slot.Element != null)
            {
                ElementSO elem = slot.Element;
                entryLookup.TryGetValue(elem.Symbol ?? string.Empty, out EncyclopediaEntry entry);

                badge = elem.Symbol;
                title = elem.ElementName;
                subtitle = entry != null && !string.IsNullOrEmpty(entry.subtitle)
                    ? entry.subtitle
                    : $"Nomor atom {elem.AtomicNumber}";
                body = entry != null && !string.IsNullOrEmpty(entry.description)
                    ? entry.description
                    : elem.Description;
                funFact = entry != null && !string.IsNullOrEmpty(entry.funFact)
                    ? entry.funFact
                    : elem.FunFact;
            }
            else if (slot.Kind == EncyclopediaBookSlotUI.SlotKind.Compound && slot.Compound != null)
            {
                CompoundSO comp = slot.Compound;
                entryLookup.TryGetValue(comp.ChemicalFormula ?? string.Empty, out EncyclopediaEntry entry);

                badge = EncyclopediaBookSlotUI.FormatFormulaRichText(comp.ChemicalFormula);
                title = comp.CompoundName;
                subtitle = entry != null && !string.IsNullOrEmpty(entry.subtitle)
                    ? entry.subtitle
                    : $"Rumus {comp.ChemicalFormula}";
                body = entry != null && !string.IsNullOrEmpty(entry.description)
                    ? entry.description
                    : comp.Description;
                funFact = entry != null && !string.IsNullOrEmpty(entry.funFact)
                    ? entry.funFact
                    : comp.Applications;
            }
            else
            {
                ShowRightPrompt("Pilihlah Unsur/Senyawa yang ingin dilihat informasinya!");
                return;
            }

            if (rightDetailBadgeText != null)
                rightDetailBadgeText.text = badge;
            if (rightDetailTitleText != null)
                rightDetailTitleText.text = title;
            if (rightDetailSubtitleText != null)
                rightDetailSubtitleText.text = subtitle;
            if (rightDetailBodyText != null)
                rightDetailBodyText.text = body;

            bool hasFunFact = !string.IsNullOrEmpty(funFact);
            if (rightFunFactBox != null)
                rightFunFactBox.SetActive(hasFunFact);
            if (rightFunFactText != null && hasFunFact)
                rightFunFactText.text = $"Tahukah Kamu?\n{funFact}";

            RebuildRightDetailLayout();
        }

        private void ShowSelectedGlossaryDetail(GlossaryTerm term)
        {
            if (rightPromptRoot != null)
                rightPromptRoot.SetActive(false);
            if (rightDetailRoot != null)
                rightDetailRoot.SetActive(true);

            string firstChar = !string.IsNullOrEmpty(term.term)
                ? term.term.Substring(0, 1).ToUpperInvariant()
                : "A";

            if (rightDetailBadgeText != null)
                rightDetailBadgeText.text = firstChar;
            if (rightDetailTitleText != null)
                rightDetailTitleText.text = term.term;
            if (rightDetailSubtitleText != null)
                rightDetailSubtitleText.text = "ISTILAH GLOSARIUM KIMIA";
            if (rightDetailBodyText != null)
                rightDetailBodyText.text = term.definition;
            if (rightFunFactBox != null)
                rightFunFactBox.SetActive(false);

            RebuildRightDetailLayout();
        }

        private void RebuildRightDetailLayout()
        {
            if (rightDetailRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rightDetailRoot.GetComponent<RectTransform>());
            }

            if (rightScrollRect != null && rightScrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rightScrollRect.content);
            }
        }
    }
}
