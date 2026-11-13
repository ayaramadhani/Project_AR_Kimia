using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ARKimia.Data;
using ARKimia.Gameplay;
using ARKimia.UI;

namespace ARKimia.EditorTools
{
    public static class MixingCartVerificationRunner
    {
        public static void SetupAndVerifyBatch()
        {
            MixingCartSceneBuilder.SetupBatchEntry();
            VerifyMechanicsInMemory();
            // Re-open saved scene cleanly so in-memory test mutations are discarded.
            EditorSceneManager.OpenScene("Assets/Scenes/ARScene.unity", OpenSceneMode.Single);
            Debug.Log("[MixingCartVerificationRunner] ALL 11 SI-A MECHANIC CHECKS PASSED.");
        }

        [MenuItem("AR Kimia/Verify Mixing Cart Mechanics (Si A)")]
        public static void VerifyMechanicsInMemory()
        {
            ElementCombinationManager manager = UnityEngine.Object.FindAnyObjectByType<ElementCombinationManager>();
            MixingCartUI ui = UnityEngine.Object.FindAnyObjectByType<MixingCartUI>();

            Assert(manager != null, "ElementCombinationManager missing in scene.");
            Assert(ui != null, "MixingCartUI missing in scene.");

            // Invoke Awake/OnEnable lifecycle on in-memory objects if running in EditMode batch.
            var managerAwake = typeof(ElementCombinationManager).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var managerOnEnable = typeof(ElementCombinationManager).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var uiAwake = typeof(MixingCartUI).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var uiOnEnable = typeof(MixingCartUI).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            managerAwake?.Invoke(manager, null);
            managerOnEnable?.Invoke(manager, null);
            uiAwake?.Invoke(ui, null);
            uiOnEnable?.Invoke(ui, null);

            ChemistryDatabase db = manager.Database;
            Assert(db != null, "ChemistryDatabase failed to load.");

            ElementSO elemH = db.GetElementBySymbol("H");
            ElementSO elemO = db.GetElementBySymbol("O");
            Assert(elemH != null && elemO != null, "Elements H or O missing from ChemistryDatabase.");

            Transform safeArea = ui.transform.Find("SafeArea");
            GameObject frame49 = safeArea.Find("Frame49_Minimized").gameObject;
            GameObject frame52 = safeArea.Find("Frame52_Expanded").gameObject;
            GameObject gabungRoot = safeArea.Find("GabungkanRoot").gameObject;
            GameObject popupRoot = safeArea.Find("DiscoveryPopup").gameObject;
            Button expandBtn = frame49.GetComponent<Button>();
            Text badgeTxt = frame49.transform.Find("ActiveCountBadge/CountText").GetComponent<Text>();
            Button closeBtn = frame52.transform.Find("MinimizeButton_X").GetComponent<Button>();
            Button combineBtn = gabungRoot.transform.Find("GabungkanButton").GetComponent<Button>();
            GameObject glowObj = gabungRoot.transform.Find("GlowBackplate").gameObject;
            Button continueBtn = popupRoot.transform.Find("PopupInner/ContinueButton").GetComponent<Button>();
            Text popupTitle = popupRoot.transform.Find("PopupInner/HeaderBanner/PopupTitle").GetComponent<Text>();
            Text popupCompound = popupRoot.transform.Find("PopupInner/PopupCompound").GetComponent<Text>();

            // 1. Default hidden state
            manager.ResetCart();
            Assert(!frame49.activeSelf && !frame52.activeSelf && !gabungRoot.activeSelf, "Case 1 failed: Panels must start hidden.");

            EncyclopediaBookUI bookUi = safeArea.GetComponentInChildren<EncyclopediaBookUI>(true);
            Assert(bookUi != null && !bookUi.PanelRoot.activeSelf, "Case 1b failed: EncyclopediaBookUI must exist and start closed.");
            var bookAwake = typeof(EncyclopediaBookUI).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var bookOnEnable = typeof(EncyclopediaBookUI).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            bookAwake?.Invoke(bookUi, null);
            bookOnEnable?.Invoke(bookUi, null);
            bookUi.EnsureInitialized();

            Assert(bookUi.SpawnedSlots.Count == 15, $"Case 1c failed: Expected 15 index slots (10 elements + 5 compounds), got {bookUi.SpawnedSlots.Count}.");
            for (int i = 0; i < bookUi.SpawnedSlots.Count; i++)
            {
                EncyclopediaBookSlotUI slot = bookUi.SpawnedSlots[i];
                Assert(!slot.IsUnlocked && !slot.IsSelected && slot.LabelText.text == "???",
                    $"Case 1d failed: Slot {i} must start locked as '???'.");
            }

            bookUi.Open(EncyclopediaBookUI.BookTab.Index);
            Assert(bookUi.PanelRoot.activeSelf && bookUi.CornerRibbonText.text == "Index" && bookUi.RightPromptRoot.activeSelf && !bookUi.RightDetailRoot.activeSelf,
                "Case 1e failed: Opening Encyclopedia initially must show Index prompt on right page.");
            bookUi.SpawnedSlots[0].SlotButton.onClick.Invoke();
            Assert(bookUi.SelectedSlot == null && !bookUi.RightDetailRoot.activeSelf,
                "Case 1f failed: Clicking locked slot must not select it.");
            bookUi.Close();

            // 2. 0 -> 1 auto-expand via QuickDrop
            bool added = manager.TryAddFromQuickDrop(elemO);
            Assert(added && manager.GetQuantity(elemO) == 1, "Case 2 failed: QuickDrop O should set quantity 1.");
            Assert(frame52.activeSelf && !frame49.activeSelf, "Case 2 failed: Frame 52 must auto-expand on 0->1.");
            Assert(!gabungRoot.activeSelf, "Case 2 failed: Gabungkan must stay hidden for a single element O=1.");

            // 3. Max cap = 10 & Plus button disabled
            manager.SetQuantity(elemO, 15);
            Assert(manager.GetQuantity(elemO) == 10 && !manager.CanIncrement(elemO), "Case 3 failed: Quantity must clamp at 10.");
            MixingCartRowUI rowO = frame52.GetComponentInChildren<MixingCartRowUI>(false);
            Assert(rowO != null, "Case 3 failed: Active row for O not found.");
            Button plusBtn = rowO.transform.Find("CounterBar/PlusButton").GetComponent<Button>();
            Assert(!plusBtn.interactable, "Case 3 failed: Plus button must disable at 10.");

            // 4. Minimize X -> Frame 49 with badge
            closeBtn.onClick.Invoke();
            Assert(!frame52.activeSelf && frame49.activeSelf, "Case 4 failed: Clicking X must minimize Frame 52 into Frame 49.");
            Assert(badgeTxt.text == "1" && manager.GetQuantity(elemO) == 10, "Case 4 failed: Frame 49 badge and cart state must persist.");

            // 5. Expand Frame 49 -> Frame 52
            expandBtn.onClick.Invoke();
            Assert(frame52.activeSelf && !frame49.activeSelf, "Case 5 failed: Clicking Frame 49 must re-open Frame 52.");

            // 6. Decrement to 0 -> row & panel disappear
            manager.SetQuantity(elemO, 0);
            Assert(manager.GetQuantity(elemO) == 0 && !frame52.activeSelf && !frame49.activeSelf && !gabungRoot.activeSelf,
                "Case 6 failed: Reaching 0 must hide row and panels.");

            // 7. Related elements with wrong counts -> disabled button; correct counts -> active; unrelated -> hidden
            manager.SetQuantity(elemH, 1);
            manager.SetQuantity(elemO, 1);
            Assert(gabungRoot.activeSelf && !combineBtn.interactable, "Case 7a failed: H=1,O=1 must show disabled Gabungkan.");
            manager.SetQuantity(elemH, 2);
            CompoundSO match = manager.EvaluateCurrentCombination();
            Assert(match != null && match.ChemicalFormula == "H2O", "Case 7 failed: H=2, O=1 must match H2O.");
            Assert(combineBtn.interactable && glowObj.activeSelf, "Case 7 failed: Gabungkan button must be enabled and glowing.");

            // 8. Click Gabungkan -> camera lock, molecule spawn, cart reset to 0, victory popup shown
            combineBtn.onClick.Invoke();
            Assert(manager.IsReactionLocked, "Case 8 failed: Reaction must lock camera.");
            Assert(manager.ActiveCompoundVisual != null, "Case 8 failed: 3D compound molecule must spawn.");
            Assert(manager.ActiveElementTypeCount == 0 && !frame52.activeSelf && !gabungRoot.activeSelf,
                "Case 8 failed: Cart must reset to 0 and hide counter panel.");
            Assert(popupRoot.activeSelf && popupTitle.text == "Senyawa Baru Ditemukan!" && popupCompound.text.Contains("Air"),
                "Case 8 failed: Victory popup must display compound info.");

            // 9. Click Lanjut -> popup closes & camera unlocks
            continueBtn.onClick.Invoke();
            Assert(!popupRoot.activeSelf && !manager.IsReactionLocked, "Case 9 failed: Continue button must close popup and unlock camera.");

            // 10. Physical marker can decrement 1->0 while keeping row visible at 0, excluding it from active recipe until re-incremented or card lost
            GameObject targetHObj = GameObject.Find("ImageTarget_H");
            GameObject targetOObj = GameObject.Find("ImageTarget_O");
            GameObject targetNaObj = GameObject.Find("ImageTarget_Na");
            GameObject targetClObj = GameObject.Find("ImageTarget_Cl");
            Assert(targetHObj != null && targetOObj != null && targetNaObj != null && targetClObj != null,
                "Case 10 failed: Required ImageTargets missing in ARScene.");

            var handlerH = targetHObj.GetComponent<ARKimia.Tracking.ElementTargetHandler>();
            var handlerO = targetOObj.GetComponent<ARKimia.Tracking.ElementTargetHandler>();
            var handlerNa = targetNaObj.GetComponent<ARKimia.Tracking.ElementTargetHandler>();
            var handlerCl = targetClObj.GetComponent<ARKimia.Tracking.ElementTargetHandler>();
            var handleDetected = typeof(ElementCombinationManager).GetMethod("HandleTargetDetected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var handleLost = typeof(ElementCombinationManager).GetMethod("HandleTargetLost", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            handleDetected.Invoke(manager, new object[] { handlerH });
            MixingCartRowUI rowH = frame52.GetComponentInChildren<MixingCartRowUI>(false);
            Assert(rowH != null, "Case 10a failed: Row H should appear when physical card detected.");
            Button minusBtnH = rowH.transform.Find("CounterBar/MinusButton").GetComponent<Button>();
            Button plusBtnH = rowH.transform.Find("CounterBar/PlusButton").GetComponent<Button>();
            Assert(manager.GetQuantity(elemH) == 1 && minusBtnH.interactable,
                "Case 10a failed: At H=1 with 1 physical card, minus button must be enabled.");

            minusBtnH.onClick.Invoke();
            Assert(manager.GetQuantity(elemH) == 0 && !minusBtnH.interactable && plusBtnH.interactable && frame52.activeSelf,
                "Case 10b failed: Pressing minus from 1->0 must keep row visible at 0 while physical card is tracked.");
            Assert(handlerH.IsVisualSuppressed, "Case 10b failed: Zeroed physical card must suppress 3D Bohr visual.");

            plusBtnH.onClick.Invoke();
            Assert(manager.GetQuantity(elemH) == 1 && minusBtnH.interactable && !handlerH.IsVisualSuppressed,
                "Case 10c failed: Pressing plus from 0->1 must restore quantity 1 and unsuppress 3D Bohr visual.");

            // Multi-marker exclusion: Na, H, O all tracked simultaneously; zeroing Na lets H=2 + O=1 match H2O.
            handleDetected.Invoke(manager, new object[] { handlerO });
            handleDetected.Invoke(manager, new object[] { handlerNa });
            manager.AddQuantity(elemH, 1); // H=2, O=1, Na=1 -> invalid with Na active
            Assert(manager.EvaluateCurrentCombination() == null && !gabungRoot.activeSelf,
                "Case 10d failed: H=2, O=1, Na=1 must not match H2O and Gabungkan must be hidden.");

            manager.AddQuantity(handlerNa.ElementData, -1); // Na=0 (still tracked), H=2, O=1
            CompoundSO multiMatch = manager.EvaluateCurrentCombination();
            Assert(multiMatch != null && multiMatch.ChemicalFormula == "H2O" && combineBtn.interactable,
                "Case 10d failed: Zeroing Na while tracked must allow H=2 + O=1 to match H2O.");
            Assert(frame52.GetComponentsInChildren<MixingCartRowUI>(false).Length == 3,
                "Case 10d failed: Row Na must remain visible at 0 while physical card is tracked.");

            handleLost.Invoke(manager, new object[] { handlerNa });
            handleLost.Invoke(manager, new object[] { handlerO });
            manager.SetQuantity(elemH, 0);
            handleLost.Invoke(manager, new object[] { handlerH });
            Assert(manager.GetQuantity(elemH) == 0 && !frame52.activeSelf,
                "Case 10e failed: Lifting physical card at 0 must remove row and hide panel.");

            // 11. Midpoint spawn between 2 combined physical cards (Na + Cl -> NaCl)
            Vector3 origNaPos = targetNaObj.transform.position;
            Vector3 origClPos = targetClObj.transform.position;
            targetNaObj.transform.position = new Vector3(-0.20f, 0f, 0.50f);
            targetClObj.transform.position = new Vector3(0.20f, 0f, 0.50f);

            handleDetected.Invoke(manager, new object[] { handlerNa });
            handleDetected.Invoke(manager, new object[] { handlerCl });

            bool combinedNaCl = manager.TryCombine(out CompoundSO nacl);
            Assert(combinedNaCl && nacl != null && nacl.ChemicalFormula == "NaCl", "Case 11a failed: Na + Cl should combine into NaCl.");
            Assert(manager.ActiveCompoundVisual != null, "Case 11b failed: NaCl 3D visual missing.");

            Vector3 spawnedPos = manager.ActiveCompoundVisual.transform.position;
            Assert(Mathf.Abs(spawnedPos.x) < 0.001f && Mathf.Abs(spawnedPos.z - 0.50f) < 0.001f,
                $"Case 11c failed: Molecule must spawn at midpoint (0, y, 0.5), got {spawnedPos}.");

            continueBtn.onClick.Invoke();
            handleLost.Invoke(manager, new object[] { handlerNa });
            handleLost.Invoke(manager, new object[] { handlerCl });
            targetNaObj.transform.position = origNaPos;
            targetClObj.transform.position = origClPos;
            manager.ClearActiveCompoundVisual();

            // 12. Verify enlarged Figma Frame 52 dimensions & UIDraggablePanel dynamic movement
            RectTransform f52Rt = frame52.GetComponent<RectTransform>();
            RectTransform f49Rt = frame49.GetComponent<RectTransform>();
            var drag52 = frame52.GetComponent<UIDraggablePanel>();
            var drag49 = frame49.GetComponent<UIDraggablePanel>();

            Assert(drag52 != null && drag49 != null, "Case 12a failed: UIDraggablePanel must be attached to both Frame 52 and Frame 49.");
            Assert(f52Rt.sizeDelta.x >= 169f && f52Rt.sizeDelta.y >= 566f,
                $"Case 12b failed: Frame 52 panel size should match/exceed Figma 169x566, got {f52Rt.sizeDelta}.");
            Assert(f49Rt.sizeDelta.x >= 130f && f49Rt.sizeDelta.y >= 130f,
                $"Case 12c failed: Minimized button should be enlarged (>= 130x130), got {f49Rt.sizeDelta}.");

            Transform rowTpl = f52Rt.Find("InnerPanel/ScrollViewport/RowsContainer/RowTemplate");
            Assert(rowTpl != null, "Case 12d failed: RowTemplate missing in hierarchy.");
            RectTransform rowRt = rowTpl.GetComponent<RectTransform>();
            RectTransform symRt = rowTpl.Find("SymbolBox") as RectTransform;
            RectTransform barRt = rowTpl.Find("CounterBar") as RectTransform;
            Assert(rowRt.sizeDelta.x >= 125f && rowRt.sizeDelta.y >= 185f,
                $"Case 12e failed: Row template size should match Figma elem-count 125x185, got {rowRt.sizeDelta}.");
            Assert(symRt != null && symRt.sizeDelta.x >= 125f && symRt.sizeDelta.y >= 125f,
                $"Case 12f failed: SymbolBox should be >= 125x125, got {symRt?.sizeDelta}.");
            Assert(barRt != null && barRt.sizeDelta.x >= 125f && barRt.sizeDelta.y >= 45f,
                $"Case 12g failed: CounterBar should be >= 125x45, got {barRt?.sizeDelta}.");

            // Test top-only drag handle, ScrollRect preservation, and position sync between Frame 49 and Frame 52
            Transform scrollVp = f52Rt.Find("InnerPanel/ScrollViewport");
            Assert(scrollVp != null && scrollVp.GetComponent<ScrollRect>() != null,
                "Case 12h failed: ScrollViewport must have ScrollRect attached for scrolling element cards.");
            Assert(symRt.GetComponent<UIDragForwarder>() == null,
                "Case 12h failed: SymbolBox must NOT have UIDragForwarder so dragging cards scrolls the list.");

            Transform dragGrip = f52Rt.Find("InnerPanel/DragHandleGrip");
            var gripForwarder = dragGrip != null ? dragGrip.GetComponent<UIDragForwarder>() : null;
            Assert(gripForwarder != null && drag52.DragHandle == dragGrip as RectTransform,
                "Case 12i failed: DragHandleGrip at top of Frame 52 must have UIDragForwarder and be wired as DragHandle.");

            Vector2 initialPos = f52Rt.anchoredPosition;
            var pointerEvent = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = new Vector2(200f, 200f),
                delta = Vector2.zero
            };

            // Dragging via top grip handle moves Frame 52 AND syncs Frame 49 position
            gripForwarder.OnPointerDown(pointerEvent);
            pointerEvent.position = new Vector2(260f, 260f);
            pointerEvent.delta = new Vector2(60f, 60f);
            gripForwarder.OnBeginDrag(pointerEvent);
            gripForwarder.OnDrag(pointerEvent);
            gripForwarder.OnEndDrag(pointerEvent);
            Assert(f52Rt.anchoredPosition != initialPos && f49Rt.anchoredPosition == f52Rt.anchoredPosition,
                "Case 12j failed: Dragging top grip handle must move Frame 52 and sync Frame 49 position.");

            // Dragging Frame 49 (minimized icon) and clicking to expand must open Frame 52 at Frame 49's new position
            pointerEvent.position = new Vector2(300f, 300f);
            pointerEvent.delta = Vector2.zero;
            drag49.OnPointerDown(pointerEvent);
            pointerEvent.position = new Vector2(420f, 380f);
            pointerEvent.delta = new Vector2(120f, 80f);
            drag49.OnBeginDrag(pointerEvent);
            drag49.OnDrag(pointerEvent);
            drag49.OnEndDrag(pointerEvent);
            Vector2 movedIconPos = f49Rt.anchoredPosition;
            Assert(movedIconPos != initialPos && f52Rt.anchoredPosition == movedIconPos,
                "Case 12k failed: Moving Frame 49 icon must sync Frame 52 position.");

            f52Rt.anchoredPosition = initialPos;
            f49Rt.anchoredPosition = initialPos;
            drag52.ClampToBoundary();
            drag49.ClampToBoundary();

            // 13. Right Sidebar (ElementSidebarUI, SidebarCollapse, Quickdrop, EncyclopediaBookUI) Integration
            ElementSidebarUI rightSidebar = safeArea.GetComponentInChildren<ElementSidebarUI>(true);
            SidebarCollapse sidebarCollapse = safeArea.GetComponentInChildren<SidebarCollapse>(true);
            DropVisualSpawner dropSpawner = UnityEngine.Object.FindAnyObjectByType<DropVisualSpawner>();

            Assert(rightSidebar != null && sidebarCollapse != null, "Case 13a failed: Right Sidebar (ElementSidebarUI + SidebarCollapse) missing in SafeArea.");
            Assert(UnityEngine.Object.FindAnyObjectByType<EncyclopediaPopup>() == null, "Case 13b failed: Old EncyclopediaPopup should be replaced by EncyclopediaBookUI in ARScene.");
            Assert(dropSpawner != null, "Case 13c failed: DropVisualSpawner missing in scene.");

            var sidebarStart = typeof(ElementSidebarUI).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var collapseAwake = typeof(SidebarCollapse).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var collapseStart = typeof(SidebarCollapse).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var spawnerOnEnable = typeof(DropVisualSpawner).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            collapseAwake?.Invoke(sidebarCollapse, null);
            collapseStart?.Invoke(sidebarCollapse, null);
            sidebarStart?.Invoke(rightSidebar, null);
            spawnerOnEnable?.Invoke(dropSpawner, null);

            // Discovered elements (H, O, Na, Cl from earlier cases) should appear in Unsur tab
            rightSidebar.SetTab(ElementSidebarUI.Tab.Unsur);
            ElementListItemUI[] unsurItems = rightSidebar.listParent.GetComponentsInChildren<ElementListItemUI>(false);
            Assert(unsurItems.Length >= 4, $"Case 13d failed: Discovered elements (H, O, Na, Cl) must appear in Unsur tab, got {unsurItems.Length}.");

            // Discovered compounds (H2O, NaCl from earlier cases) should appear in Senyawa tab
            rightSidebar.SetTab(ElementSidebarUI.Tab.Senyawa);
            ElementListItemUI[] senyawaItems = rightSidebar.listParent.GetComponentsInChildren<ElementListItemUI>(false);
            Assert(senyawaItems.Length >= 2, $"Case 13e failed: Discovered compounds (H2O, NaCl) must appear in Senyawa tab, got {senyawaItems.Length}.");

            // Search filter test
            rightSidebar.searchInput.text = "Air";
            rightSidebar.Refresh();
            ElementListItemUI[] filteredItems = rightSidebar.listParent.GetComponentsInChildren<ElementListItemUI>(false);
            Assert(filteredItems.Length == 1 && filteredItems[0].Compound != null && filteredItems[0].Compound.ChemicalFormula == "H2O",
                "Case 13f failed: Searching 'Air' in Senyawa tab must filter to H2O.");
            rightSidebar.searchInput.text = "";
            rightSidebar.SetTab(ElementSidebarUI.Tab.Unsur);

            // Quickdrop drag-and-drop from Right Sidebar -> increments Left Sidebar MixingCart counter
            manager.ResetCart();
            QuickdropEvents.RaiseElementDropped(elemO, new Vector2(800f, 540f));
            Assert(manager.GetQuantity(elemO) == 1 && frame52.activeSelf,
                "Case 13g failed: Dropping element O from Right Sidebar must add O=1 to Left Sidebar MixingCart.");
            manager.ResetCart();

            // Long-press/tap info request -> opens EncyclopediaBookUI directly on Tab Index with item selected
            QuickdropEvents.RaiseElementInfoRequested(elemO);
            Assert(bookUi.PanelRoot.activeSelf && bookUi.CurrentTab == EncyclopediaBookUI.BookTab.Index &&
                   bookUi.SelectedSlot != null && bookUi.SelectedSlot.Element == elemO &&
                   bookUi.SelectedSlot.IsSelected && bookUi.RightDetailRoot.activeSelf &&
                   bookUi.RightDetailTitleText.text == elemO.ElementName,
                "Case 13h failed: Tapping element O in Quickdrop must open EncyclopediaBookUI with Oksigen selected.");
            bookUi.Close();
            Assert(!bookUi.PanelRoot.activeSelf, "Case 13i failed: Closing EncyclopediaBookUI must hide overlay.");

            // SidebarCollapse toggle test
            Assert(!sidebarCollapse.IsCollapsed, "Case 13j failed: Right Sidebar should start expanded.");
            sidebarCollapse.SetCollapsed(true, instant: true);
            RectTransform toggleBtnRt = sidebarCollapse.toggleButton.GetComponent<RectTransform>();
            Assert(sidebarCollapse.IsCollapsed &&
                   Mathf.Abs(sidebarCollapse.arrowIcon.localEulerAngles.z - 180f) < 1f &&
                   toggleBtnRt.anchoredPosition.x <= -60f &&
                   toggleBtnRt.anchoredPosition.y <= -20f,
                $"Case 13k failed: Collapsing Right Sidebar must slide sidebar, rotate arrow icon 180 degrees, and keep toggle inset from rounded screen corner (pos={toggleBtnRt.anchoredPosition}).");
            sidebarCollapse.SetCollapsed(false, instant: true);

            // 14. Reset button zeroes everything; unrelated pair hides Gabungkan; unread badge clears on read
            ElementSO elemCl = db.GetElementBySymbol("Cl");
            manager.ResetCart();
            manager.SetQuantity(elemO, 1);
            manager.SetQuantity(elemCl, 1);
            Assert(!gabungRoot.activeSelf, "Case 14a failed: O + Cl (unrelated) must hide Gabungkan.");
            manager.SetQuantity(elemH, 2);
            manager.SetQuantity(elemCl, 0);
            Assert(gabungRoot.activeSelf && combineBtn.interactable, "Case 14b failed: H=2,O=1 must show active Gabungkan.");

            var resetBtn = frame52.transform.Find("ResetButton").GetComponent<Button>();
            resetBtn.onClick.Invoke();
            Assert(manager.ActiveElementTypeCount == 0 && !gabungRoot.activeSelf && !frame52.activeSelf,
                "Case 14c failed: Reset must zero all elements and hide panels.");

            manager.RegisterElementDiscovery(elemO);
            manager.MarkElementRead(elemO);
            Assert(!manager.IsElementUnread(elemO), "Case 14e failed: MarkElementRead must clear unread flag.");

            // 15. Cross-Panel Integration: LeftUpSidebar + Screenshot + Exit + Encyclopedia Book states, tabs & independent scrolls
            Transform leftUpTf = safeArea.Find("LeftUpSidebar");
            Assert(leftUpTf != null, "Case 15a failed: LeftUpSidebar missing in SafeArea.");
            Button btnLogout = leftUpTf.Find("ButtonLogout")?.GetComponent<Button>();
            Button btnCamera = leftUpTf.Find("ButtonCamera")?.GetComponent<Button>();
            Button btnEnc = leftUpTf.Find("ButtonEncyclopedia")?.GetComponent<Button>();
            Assert(btnLogout != null && btnCamera != null && btnEnc != null, "Case 15a failed: LeftUpSidebar buttons missing.");
            Assert(btnCamera.onClick.GetPersistentEventCount() >= 1 && btnCamera.onClick.GetPersistentMethodName(0) == "TakeScreenshot",
                "Case 15b failed: ButtonCamera must persistently wire ARScreenshotController.TakeScreenshot.");
            Assert(btnEnc.onClick.GetPersistentEventCount() >= 1 && btnEnc.onClick.GetPersistentMethodName(0) == "OpenIndex",
                "Case 15b failed: ButtonEncyclopedia must persistently wire EncyclopediaBookUI.OpenIndex.");
            Assert(btnLogout.onClick.GetPersistentEventCount() >= 1 && btnLogout.onClick.GetPersistentMethodName(0) == "ShowPopup",
                "Case 15b failed: ButtonLogout must persistently wire ExitConfirmation.ShowPopup.");

            ARScreenshotController screenshotCtrl = safeArea.GetComponentInChildren<ARScreenshotController>(true);
            ExitConfirmation exitCtrl = UnityEngine.Object.FindAnyObjectByType<ExitConfirmation>();
            Assert(screenshotCtrl != null && !screenshotCtrl.ScreenshotPanel.activeSelf, "Case 15c failed: ARScreenshotController panel must start hidden.");
            Assert(exitCtrl != null && !exitCtrl.PopupPanel.activeSelf, "Case 15c failed: ExitConfirmation popup must start hidden.");
            exitCtrl.ShowPopup();
            Assert(exitCtrl.PopupPanel.activeSelf, "Case 15c failed: ShowPopup must open exit confirmation.");
            exitCtrl.HidePopup();
            Assert(!exitCtrl.PopupPanel.activeSelf, "Case 15c failed: HidePopup must close exit confirmation.");

            // Discovered entries survive stage Reset; locked ones stay ???; slot selection & right-page update work
            bookUi.Open(EncyclopediaBookUI.BookTab.Index);
            EncyclopediaBookSlotUI slotH2O = null;
            EncyclopediaBookSlotUI slotFe = null;
            for (int i = 0; i < bookUi.SpawnedSlots.Count; i++)
            {
                var s = bookUi.SpawnedSlots[i];
                if (s.EntryKey == "H2O") slotH2O = s;
                if (s.EntryKey == "Fe") slotFe = s;
            }
            Assert(slotH2O != null && slotH2O.IsUnlocked && slotH2O.LabelText.text.Contains("<sub>2</sub>"),
                "Case 15d failed: H2O discovered before Reset must remain unlocked as 'H<sub>2</sub>O' in Encyclopedia.");
            Assert(slotFe != null && !slotFe.IsUnlocked && slotFe.LabelText.text == "???",
                "Case 15d failed: Undiscovered Fe must remain locked as '???'.");

            slotH2O.SlotButton.onClick.Invoke();
            Assert(bookUi.SelectedSlot == slotH2O && slotH2O.IsSelected && slotH2O.FrameImage.sprite.name == "slot_selected",
                "Case 15e failed: Clicking unlocked H2O must select slot with selected border sprite.");
            Assert(bookUi.RightDetailRoot.activeSelf && bookUi.RightDetailTitleText.text == "Air" && !string.IsNullOrEmpty(bookUi.RightDetailBodyText.text),
                "Case 15e failed: Right page must display Air explanation when H2O is selected.");

            // Independent left & right ScrollRects
            Assert(bookUi.LeftScrollRect != null && bookUi.RightScrollRect != null && bookUi.LeftScrollRect != bookUi.RightScrollRect,
                "Case 15f failed: Book must have two distinct ScrollRects for Left and Right pages.");

            RectTransform rootCanvasRt = ui.GetComponent<RectTransform>();
            Vector3 prevCanvasScale = rootCanvasRt.localScale;
            if (rootCanvasRt.localScale.x < 0.001f)
                rootCanvasRt.localScale = Vector3.one;

            string originalBody = bookUi.RightDetailBodyText.text;
            bookUi.RightDetailBodyText.text = originalBody + new string('\n', 24) + "Catatan lanjutan untuk uji gulir kanan.";
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bookUi.LeftScrollRect.content);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bookUi.RightDetailRoot.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(bookUi.RightScrollRect.content);

            var scrollUpdateBounds = typeof(ScrollRect).GetMethod("UpdateBounds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            scrollUpdateBounds?.Invoke(bookUi.LeftScrollRect, null);
            scrollUpdateBounds?.Invoke(bookUi.RightScrollRect, null);

            bookUi.LeftScrollRect.verticalNormalizedPosition = 0.25f;
            bookUi.RightScrollRect.verticalNormalizedPosition = 0.40f;
            scrollUpdateBounds?.Invoke(bookUi.LeftScrollRect, null);
            scrollUpdateBounds?.Invoke(bookUi.RightScrollRect, null);

            Assert(Mathf.Abs(bookUi.LeftScrollRect.verticalNormalizedPosition - 0.25f) < 0.02f &&
                   Mathf.Abs(bookUi.RightScrollRect.verticalNormalizedPosition - 0.40f) < 0.02f,
                $"Case 15f failed: Left and Right page ScrollRect positions must move independently (L={bookUi.LeftScrollRect.verticalNormalizedPosition}, R={bookUi.RightScrollRect.verticalNormalizedPosition}).");
            bookUi.RightDetailBodyText.text = originalBody;

            // Selecting another unlocked item resets only right scroll without resetting left scroll
            EncyclopediaBookSlotUI slotH = bookUi.SpawnedSlots[0];
            slotH.SlotButton.onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            scrollUpdateBounds?.Invoke(bookUi.LeftScrollRect, null);
            scrollUpdateBounds?.Invoke(bookUi.RightScrollRect, null);
            Assert(Mathf.Abs(bookUi.LeftScrollRect.verticalNormalizedPosition - 0.25f) < 0.02f &&
                   Mathf.Abs(bookUi.RightScrollRect.content.anchoredPosition.y) < 1f,
                $"Case 15f failed: Selecting an entry must keep left scroll position intact and reset right scroll to top (L_norm={bookUi.LeftScrollRect.verticalNormalizedPosition:F3}, R_posY={bookUi.RightScrollRect.content.anchoredPosition.y:F2}).");

            rootCanvasRt.localScale = prevCanvasScale;

            // Bookmark tab switching Index <-> Glosarium
            float idxActiveY = bookUi.BookmarkIndexRect.anchoredPosition.y;
            float glosInactiveY = bookUi.BookmarkGlosariumRect.anchoredPosition.y;
            Assert(idxActiveY > glosInactiveY, "Case 15g failed: Index bookmark must sit higher than Glosarium when active.");

            bookUi.BookmarkGlosariumButton.onClick.Invoke();
            Assert(bookUi.CurrentTab == EncyclopediaBookUI.BookTab.Glosarium && bookUi.CornerRibbonText.text == "Glosarium",
                "Case 15g failed: Glosarium bookmark must switch tab & corner banner to 'Glosarium'.");
            Assert(bookUi.BookmarkGlosariumRect.anchoredPosition.y > bookUi.BookmarkIndexRect.anchoredPosition.y,
                "Case 15g failed: Glosarium bookmark must raise above Index bookmark when active.");
            Assert(bookUi.SpawnedGlossaryItems.Count >= 10 && bookUi.RightPromptRoot.activeSelf,
                "Case 15h failed: Glosarium tab must list alphabetical terms and reset right page prompt.");
            Assert(bookUi.GlossaryItemsParent.Find("Section_A") != null,
                "Case 15h failed: Glosarium must show alphabetical section headers (e.g. Section_A).");

            bookUi.SpawnedGlossaryItems[0].ItemButton.onClick.Invoke();
            Assert(bookUi.RightDetailRoot.activeSelf && !string.IsNullOrEmpty(bookUi.RightDetailBodyText.text) &&
                   bookUi.SpawnedGlossaryItems[0].SelectionArrow.gameObject.activeSelf,
                "Case 15h failed: Clicking a glossary term must activate arrow indicator and show explanation on the right page.");

            bookUi.BookmarkIndexButton.onClick.Invoke();
            Assert(bookUi.CurrentTab == EncyclopediaBookUI.BookTab.Index && bookUi.CornerRibbonText.text == "Index",
                "Case 15i failed: Index bookmark must switch tab back to 'Index'.");
            bookUi.Close();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"[MixingCartVerification] {message}");
            }
        }
    }
}
