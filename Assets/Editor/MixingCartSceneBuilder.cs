using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using ARKimia.Data;
using ARKimia.Tracking;
using ARKimia.UI;

namespace ARKimia.EditorTools
{
    public static class MixingCartSceneBuilder
    {
        private const string UiSpritesFolder = "Assets/Sprites/UI/LeftSidebar";
        private const string FontsFolder = "Assets/Fonts";
        private const string MaterialsFolder = "Assets/Materials";
        private const string PrefabsFolder = "Assets/Prefabs/UI";
        private const string PrefabAssetPath = "Assets/Prefabs/UI/MixingCartCanvas.prefab";
        private const string ArSceneAssetPath = "Assets/Scenes/ARScene.unity";
        private const string ChemistryDatabasePath = "Assets/Resources/ChemistryData/ChemistryDatabase.asset";

        [MenuItem("AR Kimia/Setup Mixing Cart UI (Si A)")]
        public static void SetupInActiveScene()
        {
            EnsureGeneratedSprites();
            ConfigureSpriteImporters();

            Sprite iconCart = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSpritesFolder}/icon_mixing_cart.png");
            Sprite btnGabungActive = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSpritesFolder}/btn_gabung_active.png");
            Sprite btnGabungDisabled = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSpritesFolder}/btn_gabung_disabled.png");
            Sprite btnCloseX = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSpritesFolder}/btn_close_x.png");
            Sprite btnMinus = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSpritesFolder}/btn_minus.png");
            Sprite btnPlus = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSpritesFolder}/btn_plus.png");
            Sprite roundedRect = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSpritesFolder}/rounded_rect_white.png");
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSpritesFolder}/circle_white.png");

            Font fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Font fontLogoUnsur = AssetDatabase.LoadAssetAtPath<Font>($"{FontsFolder}/MarkoOne-Regular.ttf") ?? fallbackFont;
            Font fontTitle = AssetDatabase.LoadAssetAtPath<Font>($"{FontsFolder}/JotiOne-Regular.ttf") ?? fallbackFont;
            Font fontText = AssetDatabase.LoadAssetAtPath<Font>($"{FontsFolder}/Jua-Regular.ttf") ?? fallbackFont;
            Material placeholderMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/PlaceholderAtomMaterial.mat");

            WireSharedDatabaseAssets(placeholderMat, fontLogoUnsur, fontTitle, fontText);

            EnsureEventSystem();

            GameObject existingCanvas = GameObject.Find("MixingCartCanvas");
            if (existingCanvas != null)
            {
                Object.DestroyImmediate(existingCanvas);
            }

            GameObject canvasRoot = BuildMixingCartHierarchy(
                fontLogoUnsur,
                fontTitle,
                fontText,
                roundedRect,
                circleSprite,
                iconCart,
                btnGabungActive,
                btnGabungDisabled,
                btnCloseX,
                btnMinus,
                btnPlus);

            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabsFolder);

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(
                canvasRoot,
                PrefabAssetPath,
                InteractionMode.AutomatedAction);

            // Hide debug IMGUI overlay so it does not overlap HUD in Play Mode.
            MultiMarkerDebugOverlay debugOverlay = Object.FindAnyObjectByType<MultiMarkerDebugOverlay>();
            if (debugOverlay != null)
            {
                var so = new SerializedObject(debugOverlay);
                SerializedProperty prop = so.FindProperty("showOverlay");
                if (prop != null)
                {
                    prop.boolValue = false;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(debugOverlay);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log($"[MixingCartSceneBuilder] Built MixingCartCanvas and saved prefab at {PrefabAssetPath} (Prefab={prefabAsset != null}).");
        }

        public static void SetupBatchEntry()
        {
            EditorSceneManager.OpenScene(ArSceneAssetPath, OpenSceneMode.Single);
            SetupInActiveScene();
        }

        private static void WireSharedDatabaseAssets(Material placeholderMat, Font logoFont, Font titleFont, Font textFont)
        {
            ChemistryDatabase db = AssetDatabase.LoadAssetAtPath<ChemistryDatabase>(ChemistryDatabasePath);
            if (db == null)
                return;

            var so = new SerializedObject(db);
            if (placeholderMat != null)
                so.FindProperty("placeholderAtomMaterial").objectReferenceValue = placeholderMat;
            if (logoFont != null)
                so.FindProperty("logoUnsurFont").objectReferenceValue = logoFont;
            if (titleFont != null)
                so.FindProperty("titleFont").objectReferenceValue = titleFont;
            if (textFont != null)
                so.FindProperty("textFont").objectReferenceValue = textFont;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
            }

            StandaloneInputModule legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy);
            }

            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static GameObject BuildMixingCartHierarchy(
            Font fontLogoUnsur,
            Font fontTitle,
            Font fontText,
            Sprite roundedRect,
            Sprite circleSprite,
            Sprite iconCart,
            Sprite gabungActive,
            Sprite gabungDisabled,
            Sprite closeX,
            Sprite minusSprite,
            Sprite plusSprite)
        {
            Color colOuterBorder = Hex("#2D2C5B");
            Color colPanelFill = Hex("#3D3C66");
            Color colCounterBar = Hex("#4B498B");
            Color colGold = Hex("#F9AD0A");
            Color colCream = Hex("#FFEFDD");
            Color colDarkInk = Hex("#000610");
            Color colBadgeRed = Hex("#C93636");

            var canvasGo = new GameObject("MixingCartCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform safeArea = CreateRect("SafeArea", canvasGo.transform);
            StretchFull(safeArea, 0f);

            // Frame 49: Minimized icon + red count badge (Bottom-Left)
            RectTransform frame49Root = CreateRect("Frame49_Minimized", safeArea);
            frame49Root.anchorMin = Vector2.zero;
            frame49Root.anchorMax = Vector2.zero;
            frame49Root.pivot = Vector2.zero;
            frame49Root.anchoredPosition = new Vector2(80f, 92f);
            frame49Root.sizeDelta = new Vector2(120f, 120f);

            var expandImg = frame49Root.gameObject.AddComponent<Image>();
            expandImg.sprite = iconCart != null ? iconCart : roundedRect;
            expandImg.color = Color.white;
            expandImg.preserveAspect = true;

            var expandBtn = frame49Root.gameObject.AddComponent<Button>();
            expandBtn.targetGraphic = expandImg;

            RectTransform badgeRect = CreateRect("ActiveCountBadge", frame49Root);
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-6f, -6f);
            badgeRect.sizeDelta = new Vector2(40f, 40f);

            var badgeBg = badgeRect.gameObject.AddComponent<Image>();
            badgeBg.sprite = circleSprite;
            badgeBg.color = colBadgeRed;

            Text badgeText = CreateText("CountText", badgeRect, fontText, "1", 22, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
            StretchFull(badgeText.rectTransform, 0f);

            // Frame 52: Expanded vertical counter panel (Bottom-Left)
            RectTransform frame52Root = CreateRect("Frame52_Expanded", safeArea);
            frame52Root.anchorMin = Vector2.zero;
            frame52Root.anchorMax = Vector2.zero;
            frame52Root.pivot = Vector2.zero;
            frame52Root.anchoredPosition = new Vector2(88f, 92f);
            frame52Root.sizeDelta = new Vector2(144f, 548f);

            var outerImg = frame52Root.gameObject.AddComponent<Image>();
            outerImg.sprite = roundedRect;
            outerImg.type = Image.Type.Sliced;
            outerImg.color = colOuterBorder;

            RectTransform innerPanel = CreateRect("InnerPanel", frame52Root);
            StretchFull(innerPanel, 10f);
            var innerImg = innerPanel.gameObject.AddComponent<Image>();
            innerImg.sprite = roundedRect;
            innerImg.type = Image.Type.Sliced;
            innerImg.color = colPanelFill;

            // Scrollable viewport inside Frame 52 so 4+ elements scroll smoothly without overflowing
            RectTransform scrollRectRt = CreateRect("ScrollViewport", innerPanel);
            scrollRectRt.anchorMin = Vector2.zero;
            scrollRectRt.anchorMax = Vector2.one;
            scrollRectRt.offsetMin = new Vector2(6f, 14f);
            scrollRectRt.offsetMax = new Vector2(-6f, -18f);
            scrollRectRt.gameObject.AddComponent<RectMask2D>();
            var scrollBg = scrollRectRt.gameObject.AddComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.001f);

            RectTransform contentRt = CreateRect("RowsContainer", scrollRectRt);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentRt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 6, 8);
            vlg.spacing = 22f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            var fitter = contentRt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = innerPanel.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = scrollRectRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            // Row Template (elem-count)
            MixingCartRowUI rowTemplate = BuildRowTemplate(contentRt, fontLogoUnsur, fontText, roundedRect, minusSprite, plusSprite, colGold, colDarkInk, colCounterBar);

            // Red X Minimize Button at top-right of Frame 52
            RectTransform closeBtnRt = CreateRect("MinimizeButton_X", frame52Root);
            closeBtnRt.anchorMin = new Vector2(1f, 1f);
            closeBtnRt.anchorMax = new Vector2(1f, 1f);
            closeBtnRt.pivot = new Vector2(0.5f, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-4f, -4f);
            closeBtnRt.sizeDelta = new Vector2(42f, 42f);

            var closeImg = closeBtnRt.gameObject.AddComponent<Image>();
            closeImg.sprite = closeX != null ? closeX : circleSprite;
            closeImg.color = Color.white;
            closeImg.preserveAspect = true;

            var closeBtn = closeBtnRt.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;

            // Gabungkan Button (Bottom-Center of AR viewport)
            RectTransform gabungRoot = CreateRect("GabungkanRoot", safeArea);
            gabungRoot.anchorMin = new Vector2(0.5f, 0f);
            gabungRoot.anchorMax = new Vector2(0.5f, 0f);
            gabungRoot.pivot = new Vector2(0.5f, 0f);
            gabungRoot.anchoredPosition = new Vector2(-120f, 88f);
            gabungRoot.sizeDelta = new Vector2(330f, 110f);

            RectTransform glowRt = CreateRect("GlowBackplate", gabungRoot);
            glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.pivot = new Vector2(0.5f, 0.5f);
            glowRt.anchoredPosition = Vector2.zero;
            glowRt.sizeDelta = new Vector2(358f, 134f);

            var glowImg = glowRt.gameObject.AddComponent<Image>();
            glowImg.sprite = roundedRect;
            glowImg.type = Image.Type.Sliced;
            glowImg.color = new Color(1f, 0.82f, 0.25f, 0.65f);
            glowImg.raycastTarget = false;
            glowRt.gameObject.SetActive(false);

            RectTransform combineBtnRt = CreateRect("GabungkanButton", gabungRoot);
            StretchFull(combineBtnRt, 0f);

            var combineImg = combineBtnRt.gameObject.AddComponent<Image>();
            combineImg.sprite = gabungDisabled;
            combineImg.color = Color.white;
            combineImg.preserveAspect = true;

            var combineBtn = combineBtnRt.gameObject.AddComponent<Button>();
            combineBtn.targetGraphic = combineImg;
            combineBtn.transition = Selectable.Transition.None;

            // Discovery Victory Popup (Top-Center so 3D molecule on card stays visible)
            RectTransform popupRoot = CreateRect("DiscoveryPopup", safeArea);
            popupRoot.anchorMin = new Vector2(0.5f, 1f);
            popupRoot.anchorMax = new Vector2(0.5f, 1f);
            popupRoot.pivot = new Vector2(0.5f, 1f);
            popupRoot.anchoredPosition = new Vector2(-120f, -36f);
            popupRoot.sizeDelta = new Vector2(560f, 256f);

            var popupBorder = popupRoot.gameObject.AddComponent<Image>();
            popupBorder.sprite = roundedRect;
            popupBorder.type = Image.Type.Sliced;
            popupBorder.color = colOuterBorder;

            RectTransform popupInner = CreateRect("PopupInner", popupRoot);
            StretchFull(popupInner, 8f);
            var popupFill = popupInner.gameObject.AddComponent<Image>();
            popupFill.sprite = roundedRect;
            popupFill.type = Image.Type.Sliced;
            popupFill.color = colPanelFill;

            RectTransform headerBanner = CreateRect("HeaderBanner", popupInner);
            headerBanner.anchorMin = new Vector2(0f, 1f);
            headerBanner.anchorMax = new Vector2(1f, 1f);
            headerBanner.pivot = new Vector2(0.5f, 1f);
            headerBanner.anchoredPosition = new Vector2(0f, -12f);
            headerBanner.sizeDelta = new Vector2(-28f, 48f);

            var bannerImg = headerBanner.gameObject.AddComponent<Image>();
            bannerImg.sprite = roundedRect;
            bannerImg.type = Image.Type.Sliced;
            bannerImg.color = colGold;

            Text popupTitle = CreateText("PopupTitle", headerBanner, fontTitle, "Senyawa Baru Ditemukan!", 25, FontStyle.Normal, colOuterBorder, TextAnchor.MiddleCenter);
            StretchFull(popupTitle.rectTransform, 4f);

            Text popupCompound = CreateText("PopupCompound", popupInner, fontTitle, "H<size=30>2</size>O  —  Air", 34, FontStyle.Normal, colCream, TextAnchor.MiddleCenter);
            popupCompound.supportRichText = true;
            popupCompound.rectTransform.anchorMin = new Vector2(0f, 1f);
            popupCompound.rectTransform.anchorMax = new Vector2(1f, 1f);
            popupCompound.rectTransform.pivot = new Vector2(0.5f, 1f);
            popupCompound.rectTransform.anchoredPosition = new Vector2(0f, -68f);
            popupCompound.rectTransform.sizeDelta = new Vector2(-32f, 48f);

            Text popupDesc = CreateText("PopupDescription", popupInner, fontText, "Deskripsi senyawa.", 20, FontStyle.Normal, Color.white, TextAnchor.UpperCenter);
            popupDesc.rectTransform.anchorMin = new Vector2(0f, 0f);
            popupDesc.rectTransform.anchorMax = new Vector2(1f, 1f);
            popupDesc.rectTransform.offsetMin = new Vector2(24f, 64f);
            popupDesc.rectTransform.offsetMax = new Vector2(-24f, -120f);

            RectTransform continueBtnRt = CreateRect("ContinueButton", popupInner);
            continueBtnRt.anchorMin = new Vector2(0.5f, 0f);
            continueBtnRt.anchorMax = new Vector2(0.5f, 0f);
            continueBtnRt.pivot = new Vector2(0.5f, 0f);
            continueBtnRt.anchoredPosition = new Vector2(0f, 12f);
            continueBtnRt.sizeDelta = new Vector2(180f, 42f);

            var continueImg = continueBtnRt.gameObject.AddComponent<Image>();
            continueImg.sprite = roundedRect;
            continueImg.type = Image.Type.Sliced;
            continueImg.color = colGold;

            var continueBtn = continueBtnRt.gameObject.AddComponent<Button>();
            continueBtn.targetGraphic = continueImg;

            Text continueTxt = CreateText("Label", continueBtnRt, fontTitle, "Lanjut", 23, FontStyle.Normal, colOuterBorder, TextAnchor.MiddleCenter);
            StretchFull(continueTxt.rectTransform, 0f);

            popupRoot.gameObject.SetActive(false);
            frame49Root.gameObject.SetActive(false);
            frame52Root.gameObject.SetActive(false);
            gabungRoot.gameObject.SetActive(false);

            var cartUi = canvasGo.AddComponent<MixingCartUI>();
            cartUi.ConfigureReferences(
                safeArea,
                frame49Root.gameObject,
                expandBtn,
                badgeRect.gameObject,
                badgeText,
                frame52Root.gameObject,
                closeBtn,
                contentRt,
                rowTemplate,
                gabungRoot.gameObject,
                combineBtn,
                combineImg,
                glowImg,
                gabungActive,
                gabungDisabled,
                popupRoot.gameObject,
                popupTitle,
                popupCompound,
                popupDesc,
                continueBtn);

            return canvasGo;
        }

        private static MixingCartRowUI BuildRowTemplate(
            RectTransform parent,
            Font fontLogoUnsur,
            Font fontText,
            Sprite roundedRect,
            Sprite minusSprite,
            Sprite plusSprite,
            Color colGold,
            Color colDarkInk,
            Color colCounterBar)
        {
            RectTransform rowRt = CreateRect("RowTemplate", parent);
            rowRt.sizeDelta = new Vector2(100f, 148f);

            var layoutElem = rowRt.gameObject.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = 100f;
            layoutElem.preferredHeight = 148f;

            // Symbol Box (100x100)
            RectTransform symbolBox = CreateRect("SymbolBox", rowRt);
            symbolBox.anchorMin = new Vector2(0.5f, 1f);
            symbolBox.anchorMax = new Vector2(0.5f, 1f);
            symbolBox.pivot = new Vector2(0.5f, 1f);
            symbolBox.anchoredPosition = Vector2.zero;
            symbolBox.sizeDelta = new Vector2(100f, 100f);

            var symbolBg = symbolBox.gameObject.AddComponent<Image>();
            symbolBg.sprite = roundedRect;
            symbolBg.type = Image.Type.Sliced;
            symbolBg.pixelsPerUnitMultiplier = 2.2f;
            symbolBg.color = colGold;

            Text symbolText = CreateText("SymbolText", symbolBox, fontLogoUnsur, "O", 56, FontStyle.Normal, colDarkInk, TextAnchor.MiddleCenter);
            symbolText.resizeTextForBestFit = true;
            symbolText.resizeTextMinSize = 32;
            symbolText.resizeTextMaxSize = 56;
            StretchFull(symbolText.rectTransform, 4f);

            // Counter Bar (100x36)
            RectTransform counterBar = CreateRect("CounterBar", rowRt);
            counterBar.anchorMin = new Vector2(0.5f, 0f);
            counterBar.anchorMax = new Vector2(0.5f, 0f);
            counterBar.pivot = new Vector2(0.5f, 0f);
            counterBar.anchoredPosition = Vector2.zero;
            counterBar.sizeDelta = new Vector2(100f, 36f);

            var barBg = counterBar.gameObject.AddComponent<Image>();
            barBg.sprite = roundedRect;
            barBg.type = Image.Type.Sliced;
            barBg.pixelsPerUnitMultiplier = 2.5f;
            barBg.color = colCounterBar;

            // Minus Button
            RectTransform minusRt = CreateRect("MinusButton", counterBar);
            minusRt.anchorMin = new Vector2(0f, 0.5f);
            minusRt.anchorMax = new Vector2(0f, 0.5f);
            minusRt.pivot = new Vector2(0f, 0.5f);
            minusRt.anchoredPosition = new Vector2(4f, 0f);
            minusRt.sizeDelta = new Vector2(26f, 26f);

            var minusImg = minusRt.gameObject.AddComponent<Image>();
            minusImg.sprite = minusSprite != null ? minusSprite : roundedRect;
            minusImg.color = Color.white;
            minusImg.preserveAspect = true;

            var minusBtn = minusRt.gameObject.AddComponent<Button>();
            minusBtn.targetGraphic = minusImg;

            // Plus Button
            RectTransform plusRt = CreateRect("PlusButton", counterBar);
            plusRt.anchorMin = new Vector2(1f, 0.5f);
            plusRt.anchorMax = new Vector2(1f, 0.5f);
            plusRt.pivot = new Vector2(1f, 0.5f);
            plusRt.anchoredPosition = new Vector2(-4f, 0f);
            plusRt.sizeDelta = new Vector2(26f, 26f);

            var plusImg = plusRt.gameObject.AddComponent<Image>();
            plusImg.sprite = plusSprite != null ? plusSprite : roundedRect;
            plusImg.color = Color.white;
            plusImg.preserveAspect = true;

            var plusBtn = plusRt.gameObject.AddComponent<Button>();
            plusBtn.targetGraphic = plusImg;

            // Quantity Text
            Text qtyText = CreateText("QuantityText", counterBar, fontText, "1", 22, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
            qtyText.rectTransform.anchorMin = Vector2.zero;
            qtyText.rectTransform.anchorMax = Vector2.one;
            qtyText.rectTransform.offsetMin = new Vector2(30f, 0f);
            qtyText.rectTransform.offsetMax = new Vector2(-30f, 0f);

            var rowUi = rowRt.gameObject.AddComponent<MixingCartRowUI>();
            rowUi.ConfigureReferences(symbolText, qtyText, minusBtn, minusImg, plusBtn, plusImg);
            rowRt.gameObject.SetActive(false);

            return rowUi;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            string content,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor alignment)
        {
            RectTransform rt = CreateRect(name, parent);
            var txt = rt.gameObject.AddComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        private static void StretchFull(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static Color Hex(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out Color c) ? c : Color.white;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void EnsureGeneratedSprites()
        {
            EnsureFolder("Assets/Sprites");
            EnsureFolder("Assets/Sprites/UI");
            EnsureFolder(UiSpritesFolder);

            string roundedFullPath = Path.Combine(Application.dataPath, "Sprites/UI/LeftSidebar/rounded_rect_white.png");
            if (!File.Exists(roundedFullPath))
            {
                Texture2D roundedTex = BuildRoundedRectTexture(64, 16f);
                File.WriteAllBytes(roundedFullPath, roundedTex.EncodeToPNG());
                Object.DestroyImmediate(roundedTex);
            }

            string circleFullPath = Path.Combine(Application.dataPath, "Sprites/UI/LeftSidebar/circle_white.png");
            if (!File.Exists(circleFullPath))
            {
                Texture2D circleTex = BuildRoundedRectTexture(64, 31f);
                File.WriteAllBytes(circleFullPath, circleTex.EncodeToPNG());
                Object.DestroyImmediate(circleTex);
            }

            AssetDatabase.Refresh();
        }

        private static Texture2D BuildRoundedRectTexture(int size, float radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            Vector2 innerHalf = new Vector2(half - radius - 1f, half - radius - 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(Mathf.Abs(x + 0.5f - half), Mathf.Abs(y + 0.5f - half));
                    Vector2 q = p - innerHalf;
                    float dist = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        private static void ConfigureSpriteImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiSpritesFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (path.EndsWith("rounded_rect_white.png"))
                {
                    Vector4 targetBorder = new Vector4(18f, 18f, 18f, 18f);
                    if (importer.spriteBorder != targetBorder)
                    {
                        importer.spriteBorder = targetBorder;
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }
}

