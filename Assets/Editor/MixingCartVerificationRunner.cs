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
            Assert(gabungRoot.activeSelf && !combineBtn.interactable, "Case 2 failed: Gabungkan should be visible but disabled for O=1.");

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

            // 7. Valid recipe H=2, O=1 -> Gabungkan active & glowing
            manager.SetQuantity(elemH, 2);
            manager.SetQuantity(elemO, 1);
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
            Assert(manager.EvaluateCurrentCombination() == null && !combineBtn.interactable,
                "Case 10d failed: H=2, O=1, Na=1 must not match H2O while Na=1.");

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
