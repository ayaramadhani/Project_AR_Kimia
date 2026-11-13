using System.Collections.Generic;
using System.IO;
using ARKimia.Data;
using ARKimia.Gameplay;
using ARKimia.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace ARKimia.EditorTools
{
    public static class EncyclopediaAndLeftUpBuilder
    {
        private const string EncyclopediaSpritesFolder = "Assets/Sprites/UI/Encyclopedia";
        private const string LeftUpSpritesFolder = "Assets/Sprites/UI/LifeUPSidebar";
        private const string ScreenshotSpritesFolder = "Assets/Sprites/UI/CameraScreenshot";
        private const string PopUpSpritesFolder = "Assets/Sprites/UI/PopUp";
        private const string SlotMaterialPath = "Assets/Materials/EncyclopediaSlotTextSDF.mat";
        private const string GlossaryDefaultMaterialPath = "Assets/Materials/EncyclopediaGlossaryDefaultSDF.mat";
        private const string GlossarySelectedMaterialPath = "Assets/Materials/EncyclopediaGlossarySelectedSDF.mat";

        public static void IntegrateIntoCanvas(
            RectTransform safeArea,
            Sprite roundedRect,
            Sprite circleSprite,
            Sprite closeX,
            MixingCartUI cartUi,
            GameObject[] hudRootsBeforePanels)
        {
            ConfigureEncyclopediaSpriteImporters();

            TMP_FontAsset jotiSdf = LoadTmpFont(
                "Assets/_Project/Art/Fonts/Joti_One/JotiOne-Regular SDF.asset",
                "Assets/Fonts/Joti_One/JotiOne-Regular SDF.asset");
            TMP_FontAsset markoSdf = LoadTmpFont(
                "Assets/Fonts/MarkoOne/MarkoOne-Regular SDF.asset",
                "Assets/_Project/Art/Fonts/Marko_One/MarkoOne-Regular SDF.asset");
            TMP_FontAsset juaSdf = LoadTmpFont(
                "Assets/Fonts/Jua/Jua-Regular SDF.asset",
                "Assets/_Project/Art/Fonts/Jua/Jua-Regular SDF.asset") ?? jotiSdf;

            Material slotTextMat = EnsureSlotTextMaterial(markoSdf);
            Material glossaryDefaultMat = EnsureTmpOutlineMaterial(jotiSdf, GlossaryDefaultMaterialPath, Hex("#2D2C5B"), Color.clear, 0f);
            Material glossarySelectedMat = EnsureTmpOutlineMaterial(jotiSdf, GlossarySelectedMaterialPath, Hex("#2D2C5B"), Hex("#FFEFDD"), 0.22f);

            LeftUpRefs leftUp = BuildLeftUpSidebar(safeArea, circleSprite);
            EncyclopediaBookUI bookUi = BuildEncyclopediaBook(
                safeArea,
                roundedRect,
                closeX != null ? closeX : circleSprite,
                jotiSdf,
                markoSdf,
                juaSdf,
                slotTextMat,
                glossaryDefaultMat,
                glossarySelectedMat);
            CameraPanelRefs camRefs = BuildCameraScreenshotPanel(safeArea, circleSprite, roundedRect, jotiSdf);
            ExitPopupRefs exitRefs = BuildExitConfirmationPopup(safeArea, juaSdf);

            WireIntegrations(
                safeArea,
                leftUp,
                bookUi,
                camRefs,
                exitRefs,
                hudRootsBeforePanels);
        }

        private static LeftUpRefs BuildLeftUpSidebar(RectTransform safeArea, Sprite circleSprite)
        {
            Sprite iconLogout = AssetDatabase.LoadAssetAtPath<Sprite>($"{LeftUpSpritesFolder}/icon_logout.png");
            Sprite iconCamera = AssetDatabase.LoadAssetAtPath<Sprite>($"{LeftUpSpritesFolder}/icon_camera.png");
            Sprite iconEncyclopedia = AssetDatabase.LoadAssetAtPath<Sprite>($"{LeftUpSpritesFolder}/icon_encyclopedia.png");

            RectTransform barRt = CreateRect("LeftUpSidebar", safeArea);
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 1f);
            barRt.anchoredPosition = new Vector2(36f, -24f);
            barRt.sizeDelta = new Vector2(84f, 288f);

            var vlg = barRt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            Button logoutBtn = CreateSidebarCircleButton("ButtonLogout", barRt, circleSprite, iconLogout);
            Button cameraBtn = CreateSidebarCircleButton("ButtonCamera", barRt, circleSprite, iconCamera);
            Button encyclopediaBtn = CreateSidebarCircleButton("ButtonEncyclopedia", barRt, circleSprite, iconEncyclopedia);

            return new LeftUpRefs
            {
                root = barRt.gameObject,
                logoutButton = logoutBtn,
                cameraButton = cameraBtn,
                encyclopediaButton = encyclopediaBtn
            };
        }

        private static Button CreateSidebarCircleButton(
            string name,
            Transform parent,
            Sprite circleSprite,
            Sprite iconSprite)
        {
            RectTransform btnRt = CreateRect(name, parent);
            btnRt.sizeDelta = new Vector2(84f, 84f);

            var outerImg = btnRt.gameObject.AddComponent<Image>();
            outerImg.sprite = circleSprite;
            outerImg.color = Hex("#4B498B");
            outerImg.preserveAspect = true;

            var btn = btnRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = outerImg;

            RectTransform innerRt = CreateRect("BackgroundOrange", btnRt);
            StretchFull(innerRt, 4f);
            var innerImg = innerRt.gameObject.AddComponent<Image>();
            innerImg.sprite = circleSprite;
            innerImg.color = Hex("#F9AD0A");
            innerImg.preserveAspect = true;
            innerImg.raycastTarget = false;

            RectTransform iconRt = CreateRect("Icon", innerRt);
            StretchFull(iconRt, 14f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.color = Color.black;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            return btn;
        }

        private static EncyclopediaBookUI BuildEncyclopediaBook(
            RectTransform safeArea,
            Sprite roundedRect,
            Sprite closeX,
            TMP_FontAsset jotiSdf,
            TMP_FontAsset markoSdf,
            TMP_FontAsset juaSdf,
            Material slotTextMat,
            Material glossaryDefaultMat,
            Material glossarySelectedMat)
        {
            Sprite bookCover = LoadEncSprite("book_cover.png");
            Sprite pageBack3 = LoadEncSprite("book_page_back3.png");
            Sprite pageBack2 = LoadEncSprite("book_page_back2.png");
            Sprite pageBack1 = LoadEncSprite("book_page_back1.png");
            Sprite topSpread = LoadEncSprite("book_top_spread.png");
            Sprite bmIndexSprite = LoadEncSprite("bookmark_index.png");
            Sprite bmGlosariumSprite = LoadEncSprite("bookmark_glosarium.png");
            Sprite ribbonSprite = LoadEncSprite("ribbon_banner.png");
            Sprite ribbonGlosariumSprite = LoadEncSprite("ribbon_banner_glosarium.png");
            Sprite glosariumArrowSprite = LoadEncSprite("glosarium_left_arrow.png");
            Sprite rightPanelSprite = LoadEncSprite("right_page_panel.png");
            Sprite slotDefaultSprite = LoadEncSprite("slot_default.png");
            Sprite slotSelectedSprite = LoadEncSprite("slot_selected.png");

            RectTransform systemRt = CreateRect("EncyclopediaBookSystem", safeArea);
            StretchFull(systemRt, 0f);

            RectTransform overlayRt = CreateRect("BookOverlay", systemRt);
            StretchFull(overlayRt, 0f);
            var overlayImg = overlayRt.gameObject.AddComponent<Image>();
            overlayImg.color = new Color(0.02f, 0.03f, 0.08f, 0.92f);
            var backdropBtn = overlayRt.gameObject.AddComponent<Button>();
            backdropBtn.targetGraphic = overlayImg;
            backdropBtn.transition = Selectable.Transition.None;

            RectTransform bookRoot = CreateRect("BookRoot", overlayRt);
            bookRoot.anchorMin = new Vector2(0.5f, 0.5f);
            bookRoot.anchorMax = new Vector2(0.5f, 0.5f);
            bookRoot.pivot = new Vector2(0.5f, 0.5f);
            bookRoot.anchoredPosition = new Vector2(18f, -22f);
            bookRoot.sizeDelta = new Vector2(1812.61f, 903f);
            bookRoot.localScale = new Vector3(0.84f, 0.84f, 1f);

            CreateFigmaLayer("BookCover", bookRoot, bookCover, 0f, 55.06f, 1812.61f, 847.94f, true);
            CreateFigmaLayer("PageBack3", bookRoot, pageBack3, 13.92f, 23.24f, 1779.35f, 849.12f, false);
            CreateFigmaLayer("PageBack2", bookRoot, pageBack2, 13.95f, 13.26f, 1780.31f, 851.15f, false);

            (RectTransform bmIdxRt, Button bmIdxBtn) = CreateBookmark("BookmarkIndex", bookRoot, bmIndexSprite, 351f, -72f);
            (RectTransform bmGlosRt, Button bmGlosBtn) = CreateBookmark("BookmarkGlosarium", bookRoot, bmGlosariumSprite, 517f, -32f);

            CreateFigmaLayer("PageBack1", bookRoot, pageBack1, 13.95f, 5.55f, 1780.31f, 851.15f, false);
            CreateFigmaLayer("TopSpread", bookRoot, topSpread, 12.11f, 0f, 1775.17f, 840.23f, false);

            // Independent ScrollView on Left Page
            RectTransform leftScrollRt = CreateRectTopLeft("LeftPageScrollView", bookRoot, 50f, 132f, 826f, 676f);
            var leftScroll = leftScrollRt.gameObject.AddComponent<ScrollRect>();
            leftScroll.horizontal = false;
            leftScroll.vertical = true;
            leftScroll.movementType = ScrollRect.MovementType.Clamped;
            leftScroll.scrollSensitivity = 28f;

            RectTransform leftViewport = CreateRect("Viewport", leftScrollRt);
            StretchFull(leftViewport, 0f);
            leftViewport.gameObject.AddComponent<RectMask2D>();
            var leftVpImg = leftViewport.gameObject.AddComponent<Image>();
            leftVpImg.color = new Color(0f, 0f, 0f, 0.001f);

            // Tab Index Grid Container
            RectTransform indexContent = CreateRect("IndexContent", leftViewport);
            indexContent.anchorMin = new Vector2(0f, 1f);
            indexContent.anchorMax = new Vector2(1f, 1f);
            indexContent.pivot = new Vector2(0.5f, 1f);
            indexContent.anchoredPosition = Vector2.zero;
            indexContent.sizeDelta = Vector2.zero;

            var grid = indexContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(148.66f, 151.5f);
            grid.spacing = new Vector2(50f, 74f);
            grid.padding = new RectOffset(37, 37, 34, 36);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperLeft;

            var idxFitter = indexContent.gameObject.AddComponent<ContentSizeFitter>();
            idxFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            idxFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            EncyclopediaBookSlotUI slotTemplate = CreateSlotTemplate(
                indexContent,
                slotDefaultSprite,
                slotSelectedSprite,
                markoSdf,
                slotTextMat);

            // Tab Glosarium List Container
            RectTransform glossaryContent = CreateRect("GlossaryContent", leftViewport);
            glossaryContent.anchorMin = new Vector2(0f, 1f);
            glossaryContent.anchorMax = new Vector2(1f, 1f);
            glossaryContent.pivot = new Vector2(0.5f, 1f);
            glossaryContent.anchoredPosition = Vector2.zero;
            glossaryContent.sizeDelta = Vector2.zero;

            var glosVlg = glossaryContent.gameObject.AddComponent<VerticalLayoutGroup>();
            glosVlg.padding = new RectOffset(64, 52, 22, 36);
            glosVlg.spacing = 12f;
            glosVlg.childAlignment = TextAnchor.UpperLeft;
            glosVlg.childControlWidth = true;
            glosVlg.childControlHeight = true;
            glosVlg.childForceExpandWidth = true;
            glosVlg.childForceExpandHeight = false;

            var glosFitter = glossaryContent.gameObject.AddComponent<ContentSizeFitter>();
            glosFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            glosFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject glossarySectionTemplate = CreateGlossarySectionTemplate(glossaryContent, jotiSdf);
            EncyclopediaGlossaryItemUI glossaryTemplate = CreateGlossaryTemplate(
                glossaryContent,
                jotiSdf,
                glosariumArrowSprite,
                glossaryDefaultMat,
                glossarySelectedMat);
            glossaryContent.gameObject.SetActive(false);

            leftScroll.viewport = leftViewport;
            leftScroll.content = indexContent;

            // Corner tilted banner drawn above LeftPageScrollView
            RectTransform ribbonRt = CreateFigmaLayer("CornerRibbon", bookRoot, ribbonSprite, -57.26f, -20.92f, 293.9f, 188.98f, false);
            Image ribbonImg = ribbonRt.GetComponent<Image>();
            TextMeshProUGUI ribbonTxt = CreateTmpText("RibbonLabel", ribbonRt, jotiSdf, "Index", 56f, Hex("#FFEFDD"), TextAlignmentOptions.Center);
            StretchFull(ribbonTxt.rectTransform, 14f);
            ribbonTxt.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 20.5f);

            // Right Page jagged purple panel + independent ScrollView
            RectTransform rightPanelRt = CreateFigmaLayer("RightPagePanel", bookRoot, rightPanelSprite, 942.33f, 60.35f, 801.14f, 728.58f, false);

            RectTransform rightPromptRt = CreateRect("RightPromptRoot", rightPanelRt);
            StretchFull(rightPromptRt, 56f);
            TextMeshProUGUI promptTxt = CreateTmpText(
                "PromptText",
                rightPromptRt,
                juaSdf,
                "Pilihlah Unsur/Senyawa yang ingin dilihat informasinya!",
                44f,
                Hex("#F9AD0A"),
                TextAlignmentOptions.Center);
            promptTxt.textWrappingMode = TextWrappingModes.Normal;
            StretchFull(promptTxt.rectTransform, 0f);

            RectTransform rightScrollRt = CreateRect("RightPageScrollView", rightPanelRt);
            StretchFull(rightScrollRt, 36f);
            var rightScroll = rightScrollRt.gameObject.AddComponent<ScrollRect>();
            rightScroll.horizontal = false;
            rightScroll.vertical = true;
            rightScroll.movementType = ScrollRect.MovementType.Clamped;
            rightScroll.scrollSensitivity = 28f;

            RectTransform rightViewport = CreateRect("Viewport", rightScrollRt);
            StretchFull(rightViewport, 0f);
            rightViewport.gameObject.AddComponent<RectMask2D>();
            var rightVpImg = rightViewport.gameObject.AddComponent<Image>();
            rightVpImg.color = new Color(0f, 0f, 0f, 0.001f);

            RectTransform rightContent = CreateRect("RightContent", rightViewport);
            rightContent.anchorMin = new Vector2(0f, 1f);
            rightContent.anchorMax = new Vector2(1f, 1f);
            rightContent.pivot = new Vector2(0.5f, 1f);
            rightContent.anchoredPosition = Vector2.zero;
            rightContent.sizeDelta = Vector2.zero;

            var rcVlg = rightContent.gameObject.AddComponent<VerticalLayoutGroup>();
            rcVlg.padding = new RectOffset(14, 14, 14, 24);
            rcVlg.spacing = 18f;
            rcVlg.childAlignment = TextAnchor.UpperCenter;
            rcVlg.childControlWidth = true;
            rcVlg.childControlHeight = true;
            rcVlg.childForceExpandWidth = true;
            rcVlg.childForceExpandHeight = false;

            var rcFitter = rightContent.gameObject.AddComponent<ContentSizeFitter>();
            rcFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rcFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            rightScroll.viewport = rightViewport;
            rightScroll.content = rightContent;

            RightDetailRefs detailRefs = BuildRightDetailContent(
                rightContent,
                slotDefaultSprite,
                roundedRect,
                jotiSdf,
                markoSdf,
                juaSdf);

            // Top-right close X button
            RectTransform closeBtnRt = CreateRect("CloseBookButton", bookRoot);
            closeBtnRt.anchorMin = new Vector2(1f, 1f);
            closeBtnRt.anchorMax = new Vector2(1f, 1f);
            closeBtnRt.pivot = new Vector2(0.5f, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-14f, -44f);
            closeBtnRt.sizeDelta = new Vector2(72f, 72f);
            var closeImg = closeBtnRt.gameObject.AddComponent<Image>();
            closeImg.sprite = closeX;
            closeImg.color = Color.white;
            closeImg.preserveAspect = true;
            var closeBtn = closeBtnRt.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;

            overlayRt.gameObject.SetActive(false);

            List<EncyclopediaEntry> entries = EncyclopediaDataGenerator.Generate();
            ChemistryDatabase db = AssetDatabase.LoadAssetAtPath<ChemistryDatabase>("Assets/Resources/ChemistryData/ChemistryDatabase.asset");

            var bookUi = systemRt.gameObject.AddComponent<EncyclopediaBookUI>();
            bookUi.ConfigureReferences(
                overlayRt.gameObject,
                backdropBtn,
                closeBtn,
                bmIdxRt,
                bmIdxBtn,
                bmGlosRt,
                bmGlosBtn,
                ribbonRt,
                ribbonImg,
                ribbonTxt,
                ribbonSprite,
                ribbonGlosariumSprite,
                leftScroll,
                rightScroll,
                indexContent.gameObject,
                indexContent,
                slotTemplate,
                glossaryContent.gameObject,
                glossaryContent,
                glossarySectionTemplate,
                glossaryTemplate,
                rightPromptRt.gameObject,
                promptTxt,
                detailRefs.root,
                detailRefs.badgeText,
                detailRefs.titleText,
                detailRefs.subtitleText,
                detailRefs.bodyText,
                detailRefs.funFactBox,
                detailRefs.funFactText,
                db,
                entries);

            return bookUi;
        }

        private static EncyclopediaBookSlotUI CreateSlotTemplate(
            RectTransform parent,
            Sprite defaultFrame,
            Sprite selectedFrame,
            TMP_FontAsset markoSdf,
            Material slotTextMat)
        {
            RectTransform slotRt = CreateRect("SlotTemplate", parent);
            slotRt.sizeDelta = new Vector2(148.66f, 151.5f);

            var frameImg = slotRt.gameObject.AddComponent<Image>();
            frameImg.sprite = defaultFrame;
            frameImg.color = Color.white;
            frameImg.preserveAspect = false;

            var btn = slotRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = frameImg;

            TextMeshProUGUI labelTxt = CreateTmpText("Label", slotRt, markoSdf, "???", 68f, Color.white, TextAlignmentOptions.Center);
            StretchFull(labelTxt.rectTransform, 8f);
            if (slotTextMat != null)
                labelTxt.fontSharedMaterial = slotTextMat;

            var slotUi = slotRt.gameObject.AddComponent<EncyclopediaBookSlotUI>();
            slotUi.ConfigureReferences(frameImg, labelTxt, btn, defaultFrame, selectedFrame);
            slotRt.gameObject.SetActive(false);
            return slotUi;
        }

        private static GameObject CreateGlossarySectionTemplate(RectTransform parent, TMP_FontAsset jotiSdf)
        {
            RectTransform secRt = CreateRect("GlossarySectionTemplate", parent);
            var secVlg = secRt.gameObject.AddComponent<VerticalLayoutGroup>();
            secVlg.padding = new RectOffset(0, 0, 12, 4);
            secVlg.spacing = 4f;
            secVlg.childAlignment = TextAnchor.UpperLeft;
            secVlg.childControlWidth = true;
            secVlg.childControlHeight = true;
            secVlg.childForceExpandWidth = true;
            secVlg.childForceExpandHeight = false;

            TextMeshProUGUI letterTxt = CreateTmpText("LetterText", secRt, jotiSdf, "A", 76f, Hex("#2D2C5B"), TextAlignmentOptions.BottomLeft);
            var letterLe = letterTxt.gameObject.AddComponent<LayoutElement>();
            letterLe.preferredHeight = 84f;

            RectTransform lineRt = CreateRect("DividerLine", secRt);
            var lineLe = lineRt.gameObject.AddComponent<LayoutElement>();
            lineLe.preferredHeight = 6f;
            var lineImg = lineRt.gameObject.AddComponent<Image>();
            lineImg.color = Hex("#2D2C5B");
            lineImg.raycastTarget = false;

            secRt.gameObject.SetActive(false);
            return secRt.gameObject;
        }

        private static EncyclopediaGlossaryItemUI CreateGlossaryTemplate(
            RectTransform parent,
            TMP_FontAsset jotiSdf,
            Sprite arrowSprite,
            Material defaultMat,
            Material selectedMat)
        {
            RectTransform itemRt = CreateRect("GlossaryItemTemplate", parent);
            var le = itemRt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 76f;

            var hitImg = itemRt.gameObject.AddComponent<Image>();
            hitImg.color = new Color(1f, 1f, 1f, 0.001f);

            var btn = itemRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = hitImg;
            btn.transition = Selectable.Transition.None;

            var hlg = itemRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 18f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            TextMeshProUGUI termTxt = CreateTmpText("TermLabel", itemRt, jotiSdf, "Atom", 52f, Hex("#2D2C5B"), TextAlignmentOptions.MidlineLeft);
            var termLe = termTxt.gameObject.AddComponent<LayoutElement>();
            termLe.flexibleWidth = 0f;
            termLe.preferredHeight = 72f;
            if (defaultMat != null)
                termTxt.fontSharedMaterial = defaultMat;

            RectTransform arrowRt = CreateRect("SelectionArrow", itemRt);
            var arrowLe = arrowRt.gameObject.AddComponent<LayoutElement>();
            arrowLe.preferredWidth = 56f;
            arrowLe.preferredHeight = 56f;
            arrowLe.minWidth = 56f;
            var arrowImg = arrowRt.gameObject.AddComponent<Image>();
            arrowImg.sprite = arrowSprite;
            arrowImg.color = Color.white;
            arrowImg.preserveAspect = true;
            arrowImg.raycastTarget = false;
            arrowRt.gameObject.SetActive(false);

            var itemUi = itemRt.gameObject.AddComponent<EncyclopediaGlossaryItemUI>();
            itemUi.ConfigureReferences(termTxt, arrowImg, btn, defaultMat, selectedMat);
            itemRt.gameObject.SetActive(false);
            return itemUi;
        }

        private static RightDetailRefs BuildRightDetailContent(
            RectTransform parent,
            Sprite slotDefault,
            Sprite roundedRect,
            TMP_FontAsset jotiSdf,
            TMP_FontAsset markoSdf,
            TMP_FontAsset juaSdf)
        {
            RectTransform detailRt = CreateRect("RightDetailRoot", parent);
            var vlg = detailRt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = detailRt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform headerRt = CreateRect("HeaderRow", detailRt);
            var hlg = headerRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            RectTransform badgeRt = CreateRect("SymbolBadge", headerRt);
            var badgeLe = badgeRt.gameObject.AddComponent<LayoutElement>();
            badgeLe.preferredWidth = 104f;
            badgeLe.preferredHeight = 104f;
            badgeLe.minWidth = 104f;
            var badgeImg = badgeRt.gameObject.AddComponent<Image>();
            badgeImg.sprite = roundedRect != null ? roundedRect : slotDefault;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = Hex("#F9AD0A");

            TextMeshProUGUI badgeTxt = CreateTmpText("BadgeText", badgeRt, markoSdf, "H", 42f, Hex("#2D2C5B"), TextAlignmentOptions.Center);
            StretchFull(badgeTxt.rectTransform, 6f);

            RectTransform titleColRt = CreateRect("TitleColumn", headerRt);
            var titleColLe = titleColRt.gameObject.AddComponent<LayoutElement>();
            titleColLe.flexibleWidth = 1f;
            var tcVlg = titleColRt.gameObject.AddComponent<VerticalLayoutGroup>();
            tcVlg.spacing = 6f;
            tcVlg.childControlWidth = true;
            tcVlg.childControlHeight = true;
            tcVlg.childForceExpandWidth = true;
            tcVlg.childForceExpandHeight = false;

            TextMeshProUGUI titleTxt = CreateTmpText("TitleText", titleColRt, jotiSdf, "Hidrogen", 38f, Hex("#FFEFDD"), TextAlignmentOptions.Left);
            titleTxt.textWrappingMode = TextWrappingModes.Normal;
            TextMeshProUGUI subTxt = CreateTmpText("SubtitleText", titleColRt, juaSdf, "Nomor atom 1", 22f, Hex("#F9AD0A"), TextAlignmentOptions.Left);
            subTxt.textWrappingMode = TextWrappingModes.Normal;

            RectTransform dividerRt = CreateRect("Divider", detailRt);
            var divLe = dividerRt.gameObject.AddComponent<LayoutElement>();
            divLe.preferredHeight = 4f;
            var divImg = dividerRt.gameObject.AddComponent<Image>();
            divImg.color = new Color32(249, 173, 10, 190);

            TextMeshProUGUI bodyTxt = CreateTmpText("BodyText", detailRt, juaSdf, "Deskripsi", 26f, Hex("#FFEFDD"), TextAlignmentOptions.TopLeft);
            bodyTxt.textWrappingMode = TextWrappingModes.Normal;

            RectTransform funBoxRt = CreateRect("FunFactBox", detailRt);
            var funImg = funBoxRt.gameObject.AddComponent<Image>();
            funImg.sprite = roundedRect;
            funImg.type = Image.Type.Sliced;
            funImg.color = new Color32(45, 44, 91, 235);
            var funVlg = funBoxRt.gameObject.AddComponent<VerticalLayoutGroup>();
            funVlg.padding = new RectOffset(20, 20, 16, 16);
            funVlg.childControlWidth = true;
            funVlg.childControlHeight = true;
            funVlg.childForceExpandWidth = true;
            funVlg.childForceExpandHeight = false;

            TextMeshProUGUI funTxt = CreateTmpText("FunFactText", funBoxRt, juaSdf, "Tahukah Kamu?", 23f, Hex("#FBC690"), TextAlignmentOptions.TopLeft);
            funTxt.textWrappingMode = TextWrappingModes.Normal;

            detailRt.gameObject.SetActive(false);

            return new RightDetailRefs
            {
                root = detailRt.gameObject,
                badgeText = badgeTxt,
                titleText = titleTxt,
                subtitleText = subTxt,
                bodyText = bodyTxt,
                funFactBox = funBoxRt.gameObject,
                funFactText = funTxt
            };
        }

        private static CameraPanelRefs BuildCameraScreenshotPanel(
            RectTransform safeArea,
            Sprite circleSprite,
            Sprite roundedRect,
            TMP_FontAsset jotiSdf)
        {
            Sprite bgCamera = AssetDatabase.LoadAssetAtPath<Sprite>($"{ScreenshotSpritesFolder}/BG_Camera.png");
            Sprite bgBack = AssetDatabase.LoadAssetAtPath<Sprite>($"{ScreenshotSpritesFolder}/Bg_back.png");
            Sprite iconDownload = AssetDatabase.LoadAssetAtPath<Sprite>($"{ScreenshotSpritesFolder}/download.png");
            Sprite iconShare = AssetDatabase.LoadAssetAtPath<Sprite>($"{ScreenshotSpritesFolder}/share.png");

            RectTransform panelRt = CreateRect("CameraScreenshotPanel", safeArea);
            StretchFull(panelRt, 0f);

            var bgImg = panelRt.gameObject.AddComponent<Image>();
            bgImg.sprite = bgCamera;
            bgImg.color = Color.white;
            bgImg.raycastTarget = true;

            RectTransform frameRt = CreateRect("ScreenshotFrame", panelRt);
            frameRt.anchorMin = new Vector2(0.5f, 0.5f);
            frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.anchoredPosition = new Vector2(0f, 52f);
            frameRt.sizeDelta = new Vector2(1040f, 585f);
            var frameBg = frameRt.gameObject.AddComponent<Image>();
            frameBg.color = new Color(0f, 0f, 0f, 0.25f);

            RectTransform previewRt = CreateRect("ScreenshotPreview", frameRt);
            StretchFull(previewRt, 0f);
            var rawImg = previewRt.gameObject.AddComponent<RawImage>();
            var aspectFitter = previewRt.gameObject.AddComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            Button shareBtn = CreateActionCircleButton("ButtonShare", panelRt, circleSprite, iconShare, new Vector2(-175f, 96f));
            Button downloadBtn = CreateActionCircleButton("ButtonDownload", panelRt, circleSprite, iconDownload, new Vector2(-40f, 96f));

            RectTransform backBtnRt = CreateRect("ButtonBack", panelRt);
            backBtnRt.anchorMin = new Vector2(1f, 0f);
            backBtnRt.anchorMax = new Vector2(1f, 0f);
            backBtnRt.pivot = new Vector2(1f, 0.5f);
            backBtnRt.anchoredPosition = new Vector2(-64f, 96f);
            backBtnRt.sizeDelta = new Vector2(220f, 68f);
            var backImg = backBtnRt.gameObject.AddComponent<Image>();
            backImg.sprite = bgBack != null ? bgBack : roundedRect;
            backImg.color = Color.white;
            var backBtn = backBtnRt.gameObject.AddComponent<Button>();
            backBtn.targetGraphic = backImg;

            TextMeshProUGUI backTxt = CreateTmpText("Label", backBtnRt, jotiSdf, "Kembali", 30f, Color.white, TextAlignmentOptions.Center);
            StretchFull(backTxt.rectTransform, 6f);

            var controller = panelRt.gameObject.AddComponent<ARScreenshotController>();
            panelRt.gameObject.SetActive(false);

            return new CameraPanelRefs
            {
                panelRoot = panelRt.gameObject,
                preview = rawImg,
                aspectFitter = aspectFitter,
                shareButton = shareBtn,
                downloadButton = downloadBtn,
                backButton = backBtn,
                controller = controller
            };
        }

        private static Button CreateActionCircleButton(
            string name,
            RectTransform parent,
            Sprite circleSprite,
            Sprite iconSprite,
            Vector2 bottomCenterOffset)
        {
            RectTransform rt = CreateRect(name, parent);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = bottomCenterOffset;
            rt.sizeDelta = new Vector2(110f, 110f);

            var outerImg = rt.gameObject.AddComponent<Image>();
            outerImg.sprite = circleSprite;
            outerImg.color = Hex("#494B8A");
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = outerImg;

            RectTransform innerRt = CreateRect("Background_Orange", rt);
            StretchFull(innerRt, 6f);
            var innerImg = innerRt.gameObject.AddComponent<Image>();
            innerImg.sprite = circleSprite;
            innerImg.color = Hex("#F9AD0A");
            innerImg.raycastTarget = false;

            RectTransform iconRt = CreateRect("Icon", innerRt);
            StretchFull(iconRt, 18f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            return btn;
        }

        private static ExitPopupRefs BuildExitConfirmationPopup(RectTransform safeArea, TMP_FontAsset juaSdf)
        {
            Sprite bgPopUp = AssetDatabase.LoadAssetAtPath<Sprite>($"{PopUpSpritesFolder}/Bg_PopUp.png");
            Sprite bgBack = AssetDatabase.LoadAssetAtPath<Sprite>($"{PopUpSpritesFolder}/Bg_back.png");
            Sprite bgOut = AssetDatabase.LoadAssetAtPath<Sprite>($"{PopUpSpritesFolder}/Bg_out.png");

            RectTransform rootRt = CreateRect("ExitConfirmationPopup", safeArea);
            StretchFull(rootRt, 0f);

            RectTransform overlayRt = CreateRect("Overlay", rootRt);
            StretchFull(overlayRt, 0f);
            var overlayImg = overlayRt.gameObject.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.85f);
            overlayImg.raycastTarget = true;

            RectTransform boxRt = CreateRect("PopupPanel", rootRt);
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(560f, 280f);
            var boxImg = boxRt.gameObject.AddComponent<Image>();
            boxImg.sprite = bgPopUp;
            boxImg.color = Color.white;

            TextMeshProUGUI promptTxt = CreateTmpText(
                "TextConfirmation",
                boxRt,
                juaSdf,
                "Apakah Anda yakin ingin keluar ke Main Menu?",
                28f,
                Color.white,
                TextAlignmentOptions.Center);
            promptTxt.textWrappingMode = TextWrappingModes.Normal;
            promptTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
            promptTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            promptTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
            promptTxt.rectTransform.offsetMin = new Vector2(44f, -152f);
            promptTxt.rectTransform.offsetMax = new Vector2(-44f, -40f);

            RectTransform cancelRt = CreateRect("ButtonBack", boxRt);
            cancelRt.anchorMin = new Vector2(0.5f, 0.5f);
            cancelRt.anchorMax = new Vector2(0.5f, 0.5f);
            cancelRt.pivot = new Vector2(0.5f, 0.5f);
            cancelRt.anchoredPosition = new Vector2(-120f, -68f);
            cancelRt.sizeDelta = new Vector2(168f, 48f);
            var cancelImg = cancelRt.gameObject.AddComponent<Image>();
            cancelImg.sprite = bgBack;
            var cancelBtn = cancelRt.gameObject.AddComponent<Button>();
            cancelBtn.targetGraphic = cancelImg;
            TextMeshProUGUI cancelTxt = CreateTmpText("Label", cancelRt, juaSdf, "Batal", 24f, Color.white, TextAlignmentOptions.Center);
            StretchFull(cancelTxt.rectTransform, 4f);

            RectTransform exitRt = CreateRect("ButtonExit", boxRt);
            exitRt.anchorMin = new Vector2(0.5f, 0.5f);
            exitRt.anchorMax = new Vector2(0.5f, 0.5f);
            exitRt.pivot = new Vector2(0.5f, 0.5f);
            exitRt.anchoredPosition = new Vector2(120f, -68f);
            exitRt.sizeDelta = new Vector2(168f, 48f);
            var exitImg = exitRt.gameObject.AddComponent<Image>();
            exitImg.sprite = bgOut;
            var exitBtn = exitRt.gameObject.AddComponent<Button>();
            exitBtn.targetGraphic = exitImg;
            TextMeshProUGUI exitTxt = CreateTmpText("Label", exitRt, juaSdf, "Iya", 24f, Hex("#2D2C5B"), TextAlignmentOptions.Center);
            StretchFull(exitTxt.rectTransform, 4f);

            var exitCtrl = safeArea.GetComponentInParent<Canvas>().gameObject.AddComponent<ExitConfirmation>();
            exitCtrl.ConfigureReferences(rootRt.gameObject);
            rootRt.gameObject.SetActive(false);

            return new ExitPopupRefs
            {
                popupRoot = rootRt.gameObject,
                cancelButton = cancelBtn,
                confirmButton = exitBtn,
                controller = exitCtrl
            };
        }

        private static void WireIntegrations(
            RectTransform safeArea,
            LeftUpRefs leftUp,
            EncyclopediaBookUI bookUi,
            CameraPanelRefs camRefs,
            ExitPopupRefs exitRefs,
            GameObject[] hudRootsBeforePanels)
        {
            var extraHuds = new List<GameObject>();
            if (hudRootsBeforePanels != null)
            {
                for (int i = 0; i < hudRootsBeforePanels.Length; i++)
                {
                    if (hudRootsBeforePanels[i] != null)
                        extraHuds.Add(hudRootsBeforePanels[i]);
                }
            }
            if (bookUi != null && bookUi.PanelRoot != null)
                extraHuds.Add(bookUi.PanelRoot);

            camRefs.controller.ConfigureReferences(
                leftUp.root,
                extraHuds.ToArray(),
                camRefs.panelRoot,
                camRefs.preview,
                camRefs.aspectFitter);

            UnityEventTools.AddPersistentListener(leftUp.cameraButton.onClick, camRefs.controller.TakeScreenshot);
            UnityEventTools.AddPersistentListener(camRefs.shareButton.onClick, camRefs.controller.ShareScreenshot);
            UnityEventTools.AddPersistentListener(camRefs.downloadButton.onClick, camRefs.controller.DownloadScreenshot);
            UnityEventTools.AddPersistentListener(camRefs.backButton.onClick, camRefs.controller.CloseScreenshotPanel);

            UnityEventTools.AddPersistentListener(leftUp.encyclopediaButton.onClick, bookUi.OpenIndex);

            UnityEventTools.AddPersistentListener(leftUp.logoutButton.onClick, exitRefs.controller.ShowPopup);
            UnityEventTools.AddPersistentListener(exitRefs.cancelButton.onClick, exitRefs.controller.HidePopup);
            UnityEventTools.AddPersistentListener(exitRefs.confirmButton.onClick, exitRefs.controller.ConfirmExit);
        }

        private static (RectTransform, Button) CreateBookmark(
            string name,
            Transform parent,
            Sprite sprite,
            float figmaX,
            float figmaY)
        {
            RectTransform rt = CreateFigmaLayer(name, parent, sprite, figmaX, figmaY, 90f, 150f, true);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = rt.GetComponent<Image>();
            return (rt, btn);
        }

        private static RectTransform CreateFigmaLayer(
            string name,
            Transform parent,
            Sprite sprite,
            float figmaX,
            float figmaYFromTop,
            float width,
            float height,
            bool raycastTarget)
        {
            RectTransform rt = CreateRectTopLeft(name, parent, figmaX, figmaYFromTop, width, height);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = raycastTarget;
            return rt;
        }

        private static RectTransform CreateRectTopLeft(
            string name,
            Transform parent,
            float x,
            float yFromTop,
            float w,
            float h)
        {
            RectTransform rt = CreateRect(name, parent);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yFromTop);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        private static TextMeshProUGUI CreateTmpText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string content,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            RectTransform rt = CreateRect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static TMP_FontAsset LoadTmpFont(string primaryPath, string fallbackPath)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(primaryPath)
                ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath);
        }

        private static Sprite LoadEncSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{EncyclopediaSpritesFolder}/{fileName}");
        }

        private static Material EnsureSlotTextMaterial(TMP_FontAsset markoSdf)
        {
            return EnsureTmpOutlineMaterial(markoSdf, SlotMaterialPath, Color.white, Hex("#F9AD0A"), 0.16f);
        }

        private static Material EnsureTmpOutlineMaterial(
            TMP_FontAsset fontAsset,
            string matPath,
            Color faceColor,
            Color outlineColor,
            float outlineWidth)
        {
            if (fontAsset == null || fontAsset.material == null)
                return null;

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing == null)
            {
                existing = new Material(fontAsset.material);
                AssetDatabase.CreateAsset(existing, matPath);
            }

            existing.SetColor(ShaderUtilities.ID_FaceColor, faceColor);
            existing.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            existing.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            if (outlineWidth > 0f)
                existing.EnableKeyword("OUTLINE_ON");
            else
                existing.DisableKeyword("OUTLINE_ON");
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        private static void ConfigureEncyclopediaSpriteImporters()
        {
            if (!AssetDatabase.IsValidFolder(EncyclopediaSpritesFolder))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { EncyclopediaSpritesFolder });
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
                if (dirty)
                    importer.SaveAndReimport();
            }
        }

        private static Color Hex(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out Color c) ? c : Color.white;
        }

        private struct LeftUpRefs
        {
            public GameObject root;
            public Button logoutButton;
            public Button cameraButton;
            public Button encyclopediaButton;
        }

        private struct RightDetailRefs
        {
            public GameObject root;
            public TMP_Text badgeText;
            public TMP_Text titleText;
            public TMP_Text subtitleText;
            public TMP_Text bodyText;
            public GameObject funFactBox;
            public TMP_Text funFactText;
        }

        private struct CameraPanelRefs
        {
            public GameObject panelRoot;
            public RawImage preview;
            public AspectRatioFitter aspectFitter;
            public Button shareButton;
            public Button downloadButton;
            public Button backButton;
            public ARScreenshotController controller;
        }

        private struct ExitPopupRefs
        {
            public GameObject popupRoot;
            public Button cancelButton;
            public Button confirmButton;
            public ExitConfirmation controller;
        }
    }
}

