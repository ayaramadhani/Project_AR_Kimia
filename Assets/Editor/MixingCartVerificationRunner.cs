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

            // 13. Right Sidebar (ElementSidebarUI, SidebarCollapse, Quickdrop, EncyclopediaPopup) Integration
            ElementSidebarUI rightSidebar = safeArea.GetComponentInChildren<ElementSidebarUI>(true);
            SidebarCollapse sidebarCollapse = safeArea.GetComponentInChildren<SidebarCollapse>(true);
            EncyclopediaPopup encyclopedia = UnityEngine.Object.FindAnyObjectByType<EncyclopediaPopup>();
            DropVisualSpawner dropSpawner = UnityEngine.Object.FindAnyObjectByType<DropVisualSpawner>();

            Assert(rightSidebar != null && sidebarCollapse != null, "Case 13a failed: Right Sidebar (ElementSidebarUI + SidebarCollapse) missing in SafeArea.");
            Assert(encyclopedia != null, "Case 13b failed: EncyclopediaPopup missing in scene.");
            Assert(dropSpawner != null, "Case 13c failed: DropVisualSpawner missing in scene.");

            var sidebarStart = typeof(ElementSidebarUI).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var collapseAwake = typeof(SidebarCollapse).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var collapseStart = typeof(SidebarCollapse).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var encAwake = typeof(EncyclopediaPopup).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var encOnEnable = typeof(EncyclopediaPopup).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var spawnerOnEnable = typeof(DropVisualSpawner).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            collapseAwake?.Invoke(sidebarCollapse, null);
            collapseStart?.Invoke(sidebarCollapse, null);
            sidebarStart?.Invoke(rightSidebar, null);
            encAwake?.Invoke(encyclopedia, null);
            encOnEnable?.Invoke(encyclopedia, null);
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

            // Long-press info request -> opens EncyclopediaPopup
            QuickdropEvents.RaiseElementInfoRequested(elemO);
            Assert(encyclopedia.panel.activeSelf && encyclopedia.categoryText.text == "UNSUR" && encyclopedia.symbolText.text == "O",
                "Case 13h failed: Long-press on element O must open EncyclopediaPopup with Oksigen info.");
            encyclopedia.Close();
            Assert(!encyclopedia.panel.activeSelf, "Case 13i failed: Closing EncyclopediaPopup must hide overlay panel.");

            // SidebarCollapse toggle test
            Assert(!sidebarCollapse.IsCollapsed, "Case 13j failed: Right Sidebar should start expanded.");
            sidebarCollapse.SetCollapsed(true, instant: true);
            Assert(sidebarCollapse.IsCollapsed && Mathf.Abs(sidebarCollapse.arrowIcon.localEulerAngles.z - 180f) < 1f,
                "Case 13k failed: Collapsing Right Sidebar must slide sidebar and rotate arrow icon 180 degrees.");
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
