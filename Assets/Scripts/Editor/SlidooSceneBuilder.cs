// ============================================================
//  SlidooSceneBuilder.cs  –  EDITOR ONLY
//  Menu: Slidoo ▶ Build Scene  (Ctrl+Shift+B)
//
//  Generates the COMPLETE scene hierarchy from scratch.
//  All RectTransforms use clean explicit parameters – no NaN.
// ============================================================
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Slidoo;

public static class SlidooSceneBuilder
{
    // ── Colours ──────────────────────────────────────────────────────────
    static readonly Color C_DARK   = new Color(0.10f, 0.07f, 0.05f, 0.95f);
    static readonly Color C_POPUP  = new Color(0.15f, 0.10f, 0.06f, 0.97f);
    static readonly Color C_GREEN  = new Color(0.22f, 0.70f, 0.30f, 1f);
    static readonly Color C_RED    = new Color(0.80f, 0.22f, 0.22f, 1f);
    static readonly Color C_YELLOW = new Color(0.95f, 0.78f, 0.10f, 1f);
    static readonly Color C_BLUE   = new Color(0.18f, 0.46f, 0.80f, 1f);
    static readonly Color C_GREY   = new Color(0.45f, 0.45f, 0.45f, 1f);
    static readonly Color C_GOLD   = new Color(1.00f, 0.84f, 0.00f, 1f);
    static readonly Color C_EMPTY  = new Color(0.30f, 0.30f, 0.30f, 1f);
    static readonly Color C_BORDER = new Color(0.90f, 0.75f, 0.40f, 1f);
    static readonly Color C_CLEAR  = new Color(0, 0, 0, 0);
    static readonly Color C_DIMMER = new Color(0, 0, 0, 0.60f);
    static readonly Color C_HUD    = new Color(0, 0, 0, 0.35f);

    // ════════════════════════════════════════════════════════════════════
    [MenuItem("Slidoo/Build Scene %#b")]
    public static void BuildScene()
    {
        // ── Auto-fix sprite import types first ───────────────────────────
        FixSpriteImportTypes();

        // ── Clear previous objects ───────────────────────────────────────
        foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.transform.parent == null && go.name != "Main Camera")
                Object.DestroyImmediate(go);

        // ── MANAGERS (each on its own child GO) ──────────────────────────
        var managers = Create("[ MANAGERS ]");
        Child("SaveManager",       managers).AddComponent<SaveManager>();
        BuildAudioManager(managers);
        Child("ShuffleBag",        managers).AddComponent<ShuffleBag>();
        Child("GameManager",       managers).AddComponent<GameManager>();

        var uiMgr   = Child("UIManager",        managers).AddComponent<UIManager>();
        var lvlCtrl = Child("LevelController",   managers).AddComponent<LevelController>();
        var boardMgr= Child("BoardManager",      managers).AddComponent<BoardManager>();
        var backBtn = Child("BackButtonHandler",  managers).AddComponent<BackButtonHandler>();

        // ── CANVAS ──────────────────────────────────────────────────────
        var canvas = BuildCanvas();

        // Always-visible background
        ImgStretch("[ Background ]", canvas, Sp("Common/bg"), C_DARK);

        // ── SCREENS ─────────────────────────────────────────────────────
        var screenRoot = PanelStretch("[ SCREENS ]", canvas);
        var splashGO   = BuildSplashScreen  (screenRoot);
        var menuGO     = BuildMainMenuScreen (screenRoot);
        var modeGO     = BuildModeSelectScreen(screenRoot);

        LevelSelectUI lvlUI;  LevelButtonUI[] lvlBtns;
        var lvlSelGO   = BuildLevelSelectScreen(screenRoot, out lvlUI, out lvlBtns);

        GameplayUI gpUI;  RectTransform boardContainer;
        var gpGO       = BuildGameplayScreen(screenRoot, out gpUI, out boardContainer);

        // ── POPUPS ──────────────────────────────────────────────────────
        var popRoot = PanelStretch("[ POPUPS ]", canvas);
        WinPopupUI winUI;
        var winGO   = BuildWinPopup           (popRoot, out winUI);
        var pauseGO = BuildPausePopup         (popRoot);
        var settGO  = BuildSettingsPopup      (popRoot);
        var ctrlGO  = BuildControlSettingsPopup(popRoot);
        var quitGO  = BuildQuitPopup          (popRoot);

        // Hide all except splash
        menuGO.SetActive(false); modeGO.SetActive(false);
        lvlSelGO.SetActive(false); gpGO.SetActive(false);
        winGO.SetActive(false); pauseGO.SetActive(false);
        settGO.SetActive(false); ctrlGO.SetActive(false);
        quitGO.SetActive(false);

        // ── Wire UIManager ───────────────────────────────────────────────
        Set(uiMgr, "splashScreen",           splashGO);
        Set(uiMgr, "mainMenuScreen",         menuGO);
        Set(uiMgr, "modeSelectScreen",       modeGO);
        Set(uiMgr, "levelSelectScreen",      lvlSelGO);
        Set(uiMgr, "gameplayScreen",         gpGO);
        Set(uiMgr, "winPopup",               winGO);
        Set(uiMgr, "pausePopup",             pauseGO);
        Set(uiMgr, "settingsPopup",          settGO);
        Set(uiMgr, "controlSettingsPopup",   ctrlGO);
        Set(uiMgr, "quitPopup",              quitGO);

        // ── Wire LevelController ─────────────────────────────────────────
        Set(lvlCtrl, "gameplayUI", gpUI);

        // ── Wire BoardManager ────────────────────────────────────────────
        var tilePrefab = GetOrCreateTilePrefab();
        Set(boardMgr, "boardContainer", boardContainer);
        Set(boardMgr, "tilePrefab",     tilePrefab);

        // ── Wire BackButtonHandler ───────────────────────────────────────
        var quitUI = quitGO.GetComponent<QuitPopupUI>();
        Set(backBtn, "quitPopup", quitUI);

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("<color=lime>[SlidooSceneBuilder] ✅ Done! Ctrl+S to save. Assign audio clips in AudioManager.</color>");
    }

    // ════════════════════════════════════════════════════════════════════
    //  SCREEN BUILDERS
    // ════════════════════════════════════════════════════════════════════

    static GameObject BuildSplashScreen(GameObject parent)
    {
        // Full-screen background + logo
        var root = PanelStretch("[ Screen_Splash ]", parent, Sp("Splash/splash_bg"), C_DARK);
        root.AddComponent<SplashScreenUI>().EnsureLoadingBar();
        ImgRect("Logo", root, 0.5f, 0.6f, 700, 240, 0, 0, Sp("Splash/logo"), Color.white);
        return root;
    }

    static GameObject BuildMainMenuScreen(GameObject parent)
    {
        var root = PanelStretch("[ Screen_MainMenu ]", parent, Sp("Home/home_bg"), C_DARK);

        //  ax    ay    w    h    ox   oy
        var settBtn  = Btn("Btn_Settings",    root, 1f,   1f,  110, 110,  -70, -70, Sp("Home/btn_settings", "btn_settings_1"), Color.white, "");
        var playBtn  = Btn("Btn_Play",        root, 0.5f, 0.35f,520,170,    0,   0, Sp("Home/btn_play", "btn_play_1"),     Color.white, "");
        var soundBtn = Btn("Btn_Sound",       root, 0f,   0f,  110, 110,   70,  70, Sp("Home/icon_sound", "icon_sound_1"),   Color.white, "");
        var debugBtn = Btn("Btn_DEBUG_Unlock",root, 1f,   0f,  160,  50,  -90,  35, null, C_RED, "UNLOCK ALL");
        debugBtn.SetActive(false);   // remove to test; re-hide before shipping

        var ui = root.AddComponent<MainMenuUI>();
        Set(ui, "playButton",        playBtn .GetComponent<Button>());
        Set(ui, "settingsButton",    settBtn .GetComponent<Button>());
        Set(ui, "debugUnlockButton", debugBtn.GetComponent<Button>());
        return root;
    }

    static GameObject BuildModeSelectScreen(GameObject parent)
    {
        var root = PanelStretch("[ Screen_ModeSelect ]", parent);
        ImgStretch("Bg", root, Sp("Common/bg"), C_DARK);

        ImgRect("Header", root, 0.5f, 1f, 700, 130, 0, -80, Sp("ModeSelect/mode_header"), Color.white);
        var backBtn = Btn("Btn_Back", root, 0f, 1f, 110, 110, 70, -70, Sp("ModeSelect/btn_back"), Color.white, "");

        // 4 mode cards stacked vertically
        string[] unlSp  = { "ModeSelect/mode_easy_unlocked","ModeSelect/mode_medium_unlocked",
                             "ModeSelect/mode_hard_unlocked","ModeSelect/mode_master_unlocked" };
        string[] lckSp  = { null,"ModeSelect/mode_medium_locked",
                             "ModeSelect/mode_hard_locked","ModeSelect/mode_master_locked" };
        string[] labels = { "Easy  3×3","Medium  4×4","Hard  5×5","Master  6×6" };
        float[]  cardY  = { -260f,-450f,-640f,-830f };

        var modeUIs = new ModeButtonUI[4];
        for (int i = 0; i < 4; i++)
        {
            // Card panel – fixed size
            var card = PanelRect($"ModeCard_{i}", root, 0.5f, 1f, 940, 170, 0, cardY[i]);

            // ── Unlocked group ───────────────────────────────────────────
            var uGrp = PanelStretch("Unlocked", card);
            ImgStretch("Bg", uGrp, Sp(unlSp[i]), Color.white);

            var nameT = TMPRect("Text_Name",  uGrp, 0f,   0.5f, 400, 55, 60, 20, labels[i], 26, TextAlignmentOptions.Left);
            var progT = TMPRect("Text_Prog",  uGrp, 1f,   0.5f, 180, 46,-55, 20, "0 / 30",  22, TextAlignmentOptions.Right);
            var starT = TMPRect("Text_Stars", uGrp, 1f,   0.5f, 180, 40,-55,-22, "★ 0/90",  18, TextAlignmentOptions.Right);
            nameT.color = new Color(0.1f,0.05f,0,1); progT.color = nameT.color; starT.color = C_GOLD;

            // ── Locked group ─────────────────────────────────────────────
            var lGrp = PanelStretch("Locked", card);
            if (lckSp[i] != null) ImgStretch("Bg", lGrp, Sp(lckSp[i]), Color.white);
            else                   ImgStretch("Bg", lGrp, null, C_GREY);
            ImgRect("Icon_Lock", lGrp, 0.5f, 0.5f, 60, 60, -160, 0, Sp("LevelSelect/icon_lock"), Color.white);
            var hintT = TMPRect("Text_Hint", lGrp, 0.5f, 0.5f, 500, 55, 40, 0,
                i == 0 ? "" : $"Complete 15 {ModeConfig.ModeNames[i-1]} levels to unlock",
                16, TextAlignmentOptions.Left);
            hintT.color = Color.white;

            // Invisible button covering whole card
            var btn = card.AddComponent<Button>();
            StyleBtn(btn);

            card.AddComponent<ButtonPressFeedback>();
            var mUI = card.AddComponent<ModeButtonUI>();
            Set(mUI,"unlockedGroup",uGrp); Set(mUI,"lockedGroup",lGrp);
            Set(mUI,"modeNameText", nameT); Set(mUI,"progressText",progT);
            Set(mUI,"starsText",    starT); Set(mUI,"lockHintText", hintT);
            Set(mUI,"button",       btn);
            modeUIs[i] = mUI;

            uGrp.SetActive(i == 0);
            lGrp.SetActive(i != 0);
        }

        var ui = root.AddComponent<ModeSelectUI>();
        Set(ui,"modeButtons", modeUIs);
        Set(ui,"backButton",  backBtn.GetComponent<Button>());
        return root;
    }

    static GameObject BuildLevelSelectScreen(GameObject parent,
        out LevelSelectUI lvlUI, out LevelButtonUI[] lvlBtns)
    {
        var root = PanelStretch("[ Screen_LevelSelect ]", parent);
        ImgStretch("Bg", root, Sp("Common/bg"), C_DARK);

        var backBtn = Btn("Btn_Back",     root, 0f,1f, 110,110,  70,-70, Sp("ModeSelect/btn_back"), Color.white, "");
        var ctrlBtn = Btn("Btn_Controls", root, 1f,1f, 200, 60, -115,-70, null, C_BLUE, "Controls");

        // ── Scroll view — starts below banner ───────────────────────────
        // top padding = 170 to clear HUD + count text
        var scrollGO = PanelStretchPad("LevelGrid_Scroll", root, 18,18,175,18);
        scrollGO.AddComponent<RectMask2D>();

        var grid = PanelStretch("LevelGrid_Content", scrollGO);
        var glg  = grid.AddComponent<GridLayoutGroup>();
        // 3 columns, bigger cells so they breathe on 1080px canvas
        glg.cellSize        = new Vector2(285, 285);
        glg.spacing         = new Vector2(22, 22);
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.UpperCenter;
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 3;   // ← 3 per row
        glg.padding         = new RectOffset(10,10,10,10);
        var csf = grid.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollGO.AddComponent<ScrollRect>();
        sr.content      = grid.GetComponent<RectTransform>();
        sr.viewport     = scrollGO.GetComponent<RectTransform>();
        sr.horizontal   = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;

        // ── 30 level buttons ─────────────────────────────────────────────
        // Cell is 285×285. Layout (top→bottom):
        //   Number text: center-ish at y=+30
        //   3 stars:     bottom strip at y=+30 from bottom edge  → anchorY=0, oy=+45
        lvlBtns = new LevelButtonUI[30];
        for (int i = 0; i < 30; i++)
        {
            var cell = new GameObject($"LevelBtn_{i+1:D2}");
            cell.transform.SetParent(grid.transform, false);
            cell.AddComponent<RectTransform>();

            var bgU = ImgStretch("Bg_Unlocked", cell, Sp("LevelSelect/tile_unlocked"), Color.white);
            var bgL = ImgStretch("Bg_Locked",   cell, Sp("LevelSelect/tile_locked"),   Color.white);
            bgL.SetActive(false);

            // Level number — slightly above center
            var numT = TMPRect("Text_Num", cell, 0.5f,0.5f, 220,80, 0,25, (i+1).ToString(), 48, TextAlignmentOptions.Center);
            numT.color = new Color(0.1f,0.05f,0,1);
            numT.fontStyle = FontStyles.Bold;

            // 3 stars — anchored to bottom, raised 45px above bottom edge
            var stars = new Image[3];
            float starSize = 44f;
            float starGap  = 50f;
            for (int s = 0; s < 3; s++)
            {
                float xOff = (s - 1) * starGap;   // -50, 0, +50
                var sg = ImgRect($"Star_{s}", cell, 0.5f, 0f, starSize, starSize, xOff, 45f,
                                 Sp("LevelSelect/icon_star"), C_EMPTY);
                stars[s] = sg.GetComponent<Image>();
            }

            // Lock icon — centered
            var lockIcon = ImgRect("Icon_Lock", cell, 0.5f,0.5f, 80,80, 0,0,
                                   Sp("LevelSelect/icon_lock"), Color.white);
            lockIcon.SetActive(false);

            var btn = cell.AddComponent<Button>();
            StyleBtn(btn);

            cell.AddComponent<ButtonPressFeedback>();
            var lb = cell.AddComponent<LevelButtonUI>();
            Set(lb,"button",          btn);
            Set(lb,"levelNumberText", numT);
            Set(lb,"starImages",      stars);
            Set(lb,"lockIcon",        lockIcon.GetComponent<Image>());
            Set(lb,"buttonBg",        bgU.GetComponent<Image>());
            lvlBtns[i] = lb;
        }

        // ── Wire LevelSelectUI ───────────────────────────────────────────
        lvlUI = root.AddComponent<LevelSelectUI>();
        ConfigureLevelSelect(lvlUI);
        Set(lvlUI,"levelButtons",   lvlBtns);
        Set(lvlUI,"backButton",     backBtn.GetComponent<Button>());
        Set(lvlUI,"controlsButton", ctrlBtn.GetComponent<Button>());
        return root;
    }

    [InitializeOnLoadMethod]
    static void UpdateOpenLegacyLevelSelect()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var ui = Object.FindFirstObjectByType<LevelSelectUI>(FindObjectsInactive.Include);
            if (ui != null && ui.gameObject.scene.path == "Assets/Scenes/SampleScene.unity"
                && ui.transform.Find("LevelCountBadge") == null)
                UpdateLevelSelect();
        };
    }

    [MenuItem("Slidoo/Update Level Select")]
    static void UpdateLevelSelect()
    {
        var ui = Object.FindFirstObjectByType<LevelSelectUI>(FindObjectsInactive.Include);
        if (ui == null) return;
        Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Update Level Select");
        ConfigureLevelSelect(ui);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
    }

    static void ConfigureLevelSelect(LevelSelectUI ui)
    {
        var images = new Image[4];
        var root = ui.gameObject;
        for (int i = 0; i < images.Length; i++)
        {
            string mode = ModeConfig.ModeNames[i];
            string tab = "tab_" + mode.ToLowerInvariant();
            var sprite = Sp("LevelSelect/" + tab, tab + "_0");
            var image = System.Array.Find(ui.GetComponentsInChildren<Image>(true),
                candidate => candidate.sprite != null && sprite != null && candidate.sprite.texture == sprite.texture);
            if (image == null)
            {
                var obj = ImgRect("Hud_" + mode, root, .5f,1f, 940,305, 0,-420, sprite, Color.white);
                Undo.RegisterCreatedObjectUndo(obj, "Add mode HUD");
                image = obj.GetComponent<Image>();
                if (i > 0)
                {
                    var source = images[0].rectTransform;
                    var rect = image.rectTransform;
                    rect.SetParent(source.parent, false);
                    rect.anchorMin = source.anchorMin;
                    rect.anchorMax = source.anchorMax;
                    rect.pivot = source.pivot;
                    rect.sizeDelta = source.sizeDelta;
                    rect.anchoredPosition3D = source.anchoredPosition3D;
                    rect.localScale = source.localScale;
                    rect.localRotation = source.localRotation;
                }
            }
            images[i] = image;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.gameObject.SetActive(i == 0);
        }
        foreach (string oldName in new[] { "Text_Title", "Banner" })
        {
            var old = ui.transform.Find(oldName);
            if (old != null && old != images[0].transform && !images[0].transform.IsChildOf(old))
                old.gameObject.SetActive(false);
        }
        var viewport = (RectTransform)ui.transform.Find("LevelGrid_Scroll");
        var grid = viewport.GetComponentInChildren<GridLayoutGroup>(true);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(285, 285);
        grid.spacing = new Vector2(22, 22);
        var content = (RectTransform)grid.transform;
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1);
        content.anchoredPosition = Vector2.zero;

        var corners = new Vector3[4];
        images[0].rectTransform.GetWorldCorners(corners);
        var parent = (RectTransform)ui.transform;
        float bottom = parent.InverseTransformPoint(corners[0]).y - parent.rect.yMax;
        var badge = ui.transform.Find("LevelCountBadge");
        if (badge == null)
        {
            var obj = ImgRect("LevelCountBadge", root, .5f,1f, 300,85, 0,0,
                Sp("LevelSelect/level_banner", "level_banner_29"), Color.white);
            Undo.RegisterCreatedObjectUndo(obj, "Add level counter badge");
            badge = obj.transform;
        }
        ((RectTransform)badge).anchoredPosition = new Vector2(0, bottom - 17);
        var count = badge.Find("Text_LevelCount")?.GetComponent<TextMeshProUGUI>();
        if (count == null)
        {
            count = ui.transform.Find("Text_LevelCount")?.GetComponent<TextMeshProUGUI>();
            if (count != null) count.transform.SetParent(badge, false);
            else count = TMPRect("Text_LevelCount", badge.gameObject, .5f,.5f, 245,54, 0,0,
                "Level 1 / 30", 34, TextAlignmentOptions.Center);
        }
        RTRect(count.rectTransform, .5f,.5f, 245,54);
        count.text = "Level 1 / 30";
        count.fontSize = 34;
        count.fontStyle = FontStyles.Bold;
        viewport.offsetMax = new Vector2(viewport.offsetMax.x, bottom - 68);
        foreach (var tile in content.GetComponentsInChildren<LevelButtonUI>(true))
            tile.ApplyTileStyle();

        Set(ui, "modeHudImages", images);
        Set(ui, "modeTitleText", null);
        Set(ui, "levelCountText", count);
    }

    static GameObject BuildGameplayScreen(GameObject parent,
        out GameplayUI gpUI, out RectTransform boardContainer)
    {
        const float HUD = 130f, BOT = 130f;

        var root = PanelStretch("[ Screen_Gameplay ]", parent);
        ImgStretch("Bg", root, Sp("Common/bg"), C_DARK);

        // ── Top HUD bar ──────────────────────────────────────────────────
        var hud = TopBar("HUD", root, HUD, C_HUD);
        var pauseBtn = Btn("Btn_Pause", hud, 0f,0.5f, 90,90, 55,0, null, C_CLEAR, "⏸");
        var moveT = TMPRect("Text_Moves",hud,0.5f,0.5f,240,55,-100,0,"Moves: 0",24,TextAlignmentOptions.Center);
        var tmrT  = TMPRect("Text_Timer",hud,0.5f,0.5f,180,55, 110,0,"00:00",   24,TextAlignmentOptions.Center);
        moveT.color = Color.white; moveT.fontStyle = FontStyles.Bold;
        tmrT .color = Color.white; tmrT .fontStyle = FontStyles.Bold;

        // ── Board area (between HUD and bottom bar) ───────────────────────
        var boardArea = PanelStretchPad("BoardArea", root, 0,0,(int)HUD,(int)BOT);
        ImgStretch("BoardFrame", boardArea, Sp("Gameplay/board_frame"), Color.white);
        var boardGO = PanelRect("BoardContainer", boardArea, 0.5f,0.5f, 940,940, 0,0);
        boardContainer = boardGO.GetComponent<RectTransform>();
        boardArea.AddComponent<PuzzleBoardLayout>().Refresh();

        // ── Bottom bar ───────────────────────────────────────────────────
        var botBar = BotBar("BottomBar", root, BOT, C_HUD);

        // Preview hold-button
        var pvGO = new GameObject("Btn_Preview");
        pvGO.transform.SetParent(botBar.transform, false);
        var pvRT = pvGO.AddComponent<RectTransform>();
        pvRT.anchorMin = pvRT.anchorMax = new Vector2(0.5f,0.5f);
        pvRT.pivot = new Vector2(0.5f,0.5f);
        pvRT.sizeDelta = new Vector2(300,90);
        pvRT.anchoredPosition = Vector2.zero;
        pvGO.AddComponent<Image>().color = C_YELLOW;
        var pvHold = pvGO.AddComponent<PreviewHoldButton>();
        var pvLbl  = TMPRect("Label",pvGO,0.5f,0.5f,260,70,0,0,"👁  Preview",24,TextAlignmentOptions.Center);
        pvLbl.color = new Color(0.1f,0.05f,0,1);

        // ── Preview overlay ──────────────────────────────────────────────
        var pvPanel = PanelStretch("PreviewPanel", root, null, new Color(0,0,0,0.78f));
        var pvImgGO = new GameObject("PreviewImage");
        pvImgGO.transform.SetParent(pvPanel.transform,false);
        var pvImgRT = pvImgGO.AddComponent<RectTransform>();
        pvImgRT.anchorMin = pvImgRT.anchorMax = new Vector2(0.5f,0.5f);
        pvImgRT.pivot = new Vector2(0.5f,0.5f);
        pvImgRT.sizeDelta = new Vector2(940,940);
        pvImgRT.anchoredPosition = Vector2.zero;
        var rawImg = pvImgGO.AddComponent<RawImage>();
        rawImg.color = Color.white;
        pvPanel.SetActive(false);

        gpUI = root.AddComponent<GameplayUI>();
        Set(gpUI,"moveCountText", moveT);
        Set(gpUI,"timerText",     tmrT);
        Set(gpUI,"pauseButton",   pauseBtn.GetComponent<Button>());
        Set(gpUI,"previewButton", pvHold);
        Set(gpUI,"previewImage",  rawImg);
        Set(gpUI,"previewPanel",  pvPanel);
        return root;
    }

    // ════════════════════════════════════════════════════════════════════
    //  POPUP BUILDERS
    // ════════════════════════════════════════════════════════════════════

    static GameObject BuildWinPopup(GameObject parent, out WinPopupUI winUI)
    {
        var dim   = Dimmer("[ Popup_Win ]", parent);
        var panel = PanelRect("Panel",dim,0.5f,0.5f,720,870,0,0,Sp("Win/win_panel"),C_POPUP);

        // 3 Stars at top of panel
        var stars = new Image[3];
        float[] sx = { -105f, 0f, 105f };
        for (int i=0; i<3; i++)
        {
            var sg = ImgRect($"Star_{i}",panel,0.5f,1f,100,100,sx[i],-120,Sp("LevelSelect/icon_star"),C_GOLD);
            stars[i] = sg.GetComponent<Image>();
        }

        var movesT    = TMPRect("Text_Moves",    panel,0.5f,0.5f,450,60, 0, 180,"0 Moves",    30,TextAlignmentOptions.Center);
        var timeT     = TMPRect("Text_Time",     panel,0.5f,0.5f,450,50, 0, 110,"00:00",      24,TextAlignmentOptions.Center);
        var bestMoveT = TMPRect("Text_BestMoves",panel,0.5f,0.5f,350,42, 0,  50,"Best: -",    20,TextAlignmentOptions.Center);
        var bestTimeT = TMPRect("Text_BestTime", panel,0.5f,0.5f,350,42, 0,   5,"Best: --:--",20,TextAlignmentOptions.Center);
        movesT.color = Color.white;
        timeT .color = new Color(0.9f,0.9f,0.9f,1);
        bestMoveT.color = bestTimeT.color = new Color(0.75f,0.75f,0.75f,1);

        var unlockBanner = PanelRect("UnlockBanner",panel,0.5f,0.5f,560,55,0,-55,null,new Color(0.1f,0.6f,0.1f,0.9f));
        var unlockTxt    = TMPStretch("Text_Unlock",unlockBanner,"🎉 New Mode Unlocked!",20,TextAlignmentOptions.Center);
        unlockTxt.color  = Color.white;
        unlockBanner.SetActive(false);

        var nextBtn   = Btn("Btn_Next",  panel,0.5f,0f,220,80, 115,110,Sp("Win/btn_next"),  Color.white,"");
        var replayBtn = Btn("Btn_Replay",panel,0.5f,0f,220,80,-115,110,Sp("Win/btn_replay"),Color.white,"");
        var menuBtn   = Btn("Btn_Menu",  panel,0.5f,0f,160,60,   0, 20,null,C_GREY,"Menu");

        winUI = dim.AddComponent<WinPopupUI>();
        Set(winUI,"starImages",     stars);
        Set(winUI,"movesText",      movesT);    Set(winUI,"timeText",      timeT);
        Set(winUI,"bestMovesText",  bestMoveT); Set(winUI,"bestTimeText",  bestTimeT);
        Set(winUI,"unlockBanner",   unlockBanner); Set(winUI,"unlockBannerText",unlockTxt);
        Set(winUI,"nextButton",     nextBtn  .GetComponent<Button>());
        Set(winUI,"replayButton",   replayBtn.GetComponent<Button>());
        Set(winUI,"menuButton",     menuBtn  .GetComponent<Button>());
        return dim;
    }

    static GameObject BuildPausePopup(GameObject parent)
    {
        var dim   = Dimmer("[ Popup_Pause ]", parent);
        var panel = PanelRect("Panel",dim,0.5f,0.5f,620,650,0,0,Sp("Pause/pause_panel"),C_POPUP);

        var titleT = TMPRect("Text_Title",panel,0.5f,1f,460,70,0,-65,"PAUSED",38,TextAlignmentOptions.Center);
        titleT.color = Color.white;

        var r = Btn("Btn_Resume", panel,0.5f,0.5f,460,100,0, 110,Sp("Pause/btn_resume"), Color.white,"");
        var s = Btn("Btn_Restart",panel,0.5f,0.5f,460,100,0,   0,Sp("Pause/btn_restart"),Color.white,"");
        var m = Btn("Btn_Menu",   panel,0.5f,0.5f,460,100,0,-110,Sp("Pause/btn_menu"),   Color.white,"");

        var ui = dim.AddComponent<PausePopupUI>();
        Set(ui,"resumeButton", r.GetComponent<Button>());
        Set(ui,"restartButton",s.GetComponent<Button>());
        Set(ui,"menuButton",   m.GetComponent<Button>());
        return dim;
    }

    static GameObject BuildSettingsPopup(GameObject parent)
    {
        var dim   = Dimmer("[ Popup_Settings ]", parent);
        var panel = PanelRect("Panel",dim,0.5f,0.5f,620,540,0,0,Sp("Settings/settings_panel"),C_POPUP);

        var titleT = TMPRect("Text_Title",panel,0.5f,1f,480,65,0,-60,"SETTINGS",34,TextAlignmentOptions.Center);
        titleT.color = Color.white;

        TMPRect("Lbl_Sound",panel,0.5f,0.5f,220,50,-120, 70,"Sound",26,TextAlignmentOptions.Left).color = Color.white;
        var sndToggle = BuildToggle("Toggle_Sound",panel,0.5f,0.5f,110,55, 100, 70);

        TMPRect("Lbl_Music",panel,0.5f,0.5f,220,50,-120,-10,"Music",26,TextAlignmentOptions.Left).color = Color.white;
        var musToggle = BuildToggle("Toggle_Music",panel,0.5f,0.5f,110,55, 100,-10);

        var closeBtn = Btn("Btn_Close",panel,1f,1f,65,65,-40,-40,Sp("Settings/btn_close"),Color.white,"");

        var ui = dim.AddComponent<SettingsPopupUI>();
        Set(ui,"soundToggle",sndToggle); Set(ui,"musicToggle",musToggle);
        ui.ConfigureInputs();
        Set(ui,"closeButton",closeBtn.GetComponent<Button>());
        return dim;
    }

    static GameObject BuildControlSettingsPopup(GameObject parent)
    {
        var dim   = Dimmer("[ Popup_ControlSettings ]", parent);
        var panel = PanelRect("Panel",dim,0.5f,0.5f,660,600,0,0,Sp("ControlSettings/controls_panel"),C_POPUP);

        TMPRect("Text_Title",panel,0.5f,1f,560,65,0,-60,"CONTROL SETTINGS",28,TextAlignmentOptions.Center).color=Color.white;

        var sw = Btn("Btn_Swipe",panel,0.5f,0.5f,520,90,0, 120,null,C_GREEN,"👆  Swipe  (default)");
        var tp = Btn("Btn_Tap",  panel,0.5f,0.5f,520,90,0,  10,null,C_BLUE, "👇  Tap");
        var dr = Btn("Btn_Drag", panel,0.5f,0.5f,520,90,0,-100,null,C_GREY, "✋  Drag");
        var cl = Btn("Btn_Close",panel,1f,1f,65,65,-40,-40,Sp("Settings/btn_close"),Color.white,"");

        var ui = dim.AddComponent<ControlSettingsPopupUI>();
        Set(ui,"swipeButton",sw.GetComponent<Button>()); Set(ui,"tapButton",tp.GetComponent<Button>());
        Set(ui,"dragButton", dr.GetComponent<Button>()); Set(ui,"closeButton",cl.GetComponent<Button>());
        ui.EnsureIndicators();
        return dim;
    }

    static GameObject BuildQuitPopup(GameObject parent)
    {
        var dim   = Dimmer("[ Popup_Quit ]", parent);
        var panel = PanelRect("Panel",dim,0.5f,0.5f,640,480,0,0,Sp("Quit/quit_panel"),C_POPUP);

        TMPRect("Text_Title",panel,0.5f,1f,540,65,0,-70,"QUIT GAME?",34,TextAlignmentOptions.Center).color=Color.white;
        TMPRect("Text_Sub",  panel,0.5f,0.5f,480,50,0,50,"Are you sure you want to quit?",20,TextAlignmentOptions.Center)
            .color = new Color(0.85f,0.85f,0.85f,1);

        var q = Btn("Btn_Quit",  panel,0.5f,0f,230,90, 125,80,Sp("Quit/btn_quit"),  Color.white,"");
        var c = Btn("Btn_Cancel",panel,0.5f,0f,230,90,-125,80,Sp("Quit/btn_cancel"),Color.white,"");

        var ui = dim.AddComponent<QuitPopupUI>();
        Set(ui,"quitButton",  q.GetComponent<Button>());
        Set(ui,"cancelButton",c.GetComponent<Button>());
        return dim;
    }

    // ════════════════════════════════════════════════════════════════════
    //  AUDIO MANAGER  (child GOs for SFX + Music sources)
    // ════════════════════════════════════════════════════════════════════

    static void BuildAudioManager(GameObject parent)
    {
        var go  = Child("AudioManager", parent);
        var am  = go.AddComponent<AudioManager>();
        var sfx = Child("SFX_Source",   go).AddComponent<AudioSource>();
        var mus = Child("Music_Source", go).AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        mus.playOnAwake = false; mus.loop = true;
        Set(am,"sfxSource",  sfx);
        Set(am,"musicSource",mus);
    }

    // ════════════════════════════════════════════════════════════════════
    //  CANVAS
    // ════════════════════════════════════════════════════════════════════

    static GameObject BuildCanvas()
    {
        var go = new GameObject("[ CANVAS ]");
        var cv = go.AddComponent<Canvas>();
        cv.renderMode  = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 0;

        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1080,1920);
        sc.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return go;
    }

    // ════════════════════════════════════════════════════════════════════
    //  TILE PREFAB
    // ════════════════════════════════════════════════════════════════════

    static GameObject GetOrCreateTilePrefab()
    {
        const string PATH = "Assets/Prefabs/Tile.prefab";
        var ex = AssetDatabase.LoadAssetAtPath<GameObject>(PATH);
        if (ex != null) return ex;

        var go = new GameObject("Tile");
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0,1);
        var ri = go.AddComponent<RawImage>();
        ri.color = Color.white; ri.raycastTarget = true;
        var ol = go.AddComponent<Outline>();
        ol.effectColor = C_BORDER; ol.effectDistance = new Vector2(2,2);
        go.AddComponent<Tile>();

        Directory.CreateDirectory("Assets/Prefabs");
        var pf = PrefabUtility.SaveAsPrefabAsset(go, PATH);
        Object.DestroyImmediate(go);
        AssetDatabase.Refresh();
        return pf;
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI PRIMITIVE HELPERS  –  NO Vector4 encoding, NO NaN positions
    // ════════════════════════════════════════════════════════════════════

    // ── RectTransform setup ───────────────────────────────────────────────

    /// Fill parent (optional padding).
    static void RTStretch(RectTransform rt, float l=0,float r=0,float t=0,float b=0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l,b); rt.offsetMax = new Vector2(-r,-t);
    }

    /// Fixed size, pivot centered, anchor at (ax,ay), offset by (ox,oy).
    static void RTRect(RectTransform rt, float ax,float ay,float w,float h,float ox=0,float oy=0)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(ax,ay);
        rt.pivot     = new Vector2(0.5f,0.5f);
        rt.sizeDelta        = new Vector2(w,h);
        rt.anchoredPosition = new Vector2(ox,oy);
    }

    // ── Factories ─────────────────────────────────────────────────────────

    static GameObject Create(string name)                   { return new GameObject(name); }
    static GameObject Child(string name, GameObject parent) { var g=Create(name); g.transform.SetParent(parent.transform,false); return g; }

    static GameObject PanelStretch(string name, GameObject parent, Sprite sp=null, Color? col=null)
    {
        var g = Child(name,parent); RTStretch(g.AddComponent<RectTransform>());
        if (sp!=null||col.HasValue){ var i=g.AddComponent<Image>(); if(sp!=null){i.sprite=sp;i.type=Image.Type.Simple;} i.color=col??Color.white; i.raycastTarget=false; }
        return g;
    }

    static GameObject PanelStretchPad(string name,GameObject parent,int l,int r,int t,int b)
    {
        var g=Child(name,parent); RTStretch(g.AddComponent<RectTransform>(),l,r,t,b); return g;
    }

    static GameObject PanelRect(string name,GameObject parent,float ax,float ay,float w,float h,float ox=0,float oy=0,Sprite sp=null,Color? col=null)
    {
        var g=Child(name,parent); RTRect(g.AddComponent<RectTransform>(),ax,ay,w,h,ox,oy);
        if(sp!=null||col.HasValue){ var i=g.AddComponent<Image>(); if(sp!=null){i.sprite=sp;i.type=Image.Type.Simple;} i.color=col??Color.white; }
        return g;
    }

    static GameObject ImgStretch(string name,GameObject parent,Sprite sp,Color col)
    {
        var g=Child(name,parent); RTStretch(g.AddComponent<RectTransform>());
        var i=g.AddComponent<Image>(); i.sprite=sp; i.color=col; i.raycastTarget=false;
        if(sp!=null) i.type=Image.Type.Simple; return g;
    }

    static GameObject ImgRect(string name,GameObject parent,float ax,float ay,float w,float h,float ox,float oy,Sprite sp,Color col)
    {
        var g=Child(name,parent); RTRect(g.AddComponent<RectTransform>(),ax,ay,w,h,ox,oy);
        var i=g.AddComponent<Image>(); i.sprite=sp; i.color=col; i.raycastTarget=false;
        if(sp!=null) i.type=Image.Type.Simple; return g;
    }

    static GameObject Btn(string name,GameObject parent,float ax,float ay,float w,float h,float ox,float oy,Sprite sp,Color bg,string label)
    {
        var g=Child(name,parent); RTRect(g.AddComponent<RectTransform>(),ax,ay,w,h,ox,oy);
        var i=g.AddComponent<Image>();
        i.sprite=sp;
        // If sprite loaded, use Color.white so sprite shows at full brightness.
        // If sprite is null, use bg color so button is still visible as a colored rect.
        i.color = (sp!=null) ? Color.white : bg;
        if(sp!=null) i.type=Image.Type.Simple;
        var b=g.AddComponent<Button>(); StyleBtn(b);
        // Label — white text on sprite buttons, dark text on colored buttons
        if(!string.IsNullOrEmpty(label))
        {
            var lt=TMPStretch("Label",g,label,22,TextAlignmentOptions.Center);
            lt.color = (sp!=null) ? Color.white : new Color(0.1f,0.05f,0,1f);
        }
        return g;
    }

    static TextMeshProUGUI TMPStretch(string name,GameObject parent,string text,float size,TextAlignmentOptions align)
    {
        var g=Child(name,parent); RTStretch(g.AddComponent<RectTransform>());
        var t=g.AddComponent<TextMeshProUGUI>(); t.text=text; t.fontSize=size; t.alignment=align; t.enableWordWrapping=false; return t;
    }

    static TextMeshProUGUI TMPRect(string name,GameObject parent,float ax,float ay,float w,float h,float ox,float oy,string text,float size,TextAlignmentOptions align)
    {
        var g=Child(name,parent); RTRect(g.AddComponent<RectTransform>(),ax,ay,w,h,ox,oy);
        var t=g.AddComponent<TextMeshProUGUI>(); t.text=text; t.fontSize=size; t.alignment=align; t.enableWordWrapping=false; t.overflowMode=TextOverflowModes.Ellipsis; return t;
    }

    static GameObject Dimmer(string name,GameObject parent)
    {
        var g=Child(name,parent); RTStretch(g.AddComponent<RectTransform>());
        var i=g.AddComponent<Image>(); i.color=C_DIMMER; i.raycastTarget=true; return g;
    }

    static GameObject TopBar(string name,GameObject parent,float h,Color col)
    {
        var g=Child(name,parent); var rt=g.AddComponent<RectTransform>();
        rt.anchorMin=new Vector2(0,1); rt.anchorMax=new Vector2(1,1);
        rt.pivot=new Vector2(0.5f,1f); rt.sizeDelta=new Vector2(0,h); rt.anchoredPosition=Vector2.zero;
        g.AddComponent<Image>().color=col; return g;
    }

    static GameObject BotBar(string name,GameObject parent,float h,Color col)
    {
        var g=Child(name,parent); var rt=g.AddComponent<RectTransform>();
        rt.anchorMin=new Vector2(0,0); rt.anchorMax=new Vector2(1,0);
        rt.pivot=new Vector2(0.5f,0f); rt.sizeDelta=new Vector2(0,h); rt.anchoredPosition=Vector2.zero;
        g.AddComponent<Image>().color=col; return g;
    }

    static Toggle BuildToggle(string name,GameObject parent,float ax,float ay,float w,float h,float ox,float oy)
    {
        var g=Child(name,parent); RTRect(g.AddComponent<RectTransform>(),ax,ay,w,h,ox,oy);
        var bg    = ImgStretch("Background",g, null, new Color(0.2f,0.2f,0.2f,1f));
        var check = ImgStretch("Checkmark", bg,Sp("Settings/toggle_on"),Color.white);
        var t=g.AddComponent<Toggle>();
        t.targetGraphic=bg.GetComponent<Image>(); t.graphic=check.GetComponent<Image>(); t.isOn=true; return t;
    }

    static void StyleBtn(Button b)
    {
        var c = ColorBlock.defaultColorBlock;
        c.normalColor      = Color.white;
        c.highlightedColor = new Color(0.85f,0.85f,0.85f,1f);
        c.pressedColor     = new Color(0.70f,0.70f,0.70f,1f);
        c.selectedColor    = Color.white;
        c.disabledColor    = new Color(0.5f,0.5f,0.5f,0.5f);
        c.colorMultiplier  = 1f;
        c.fadeDuration     = 0.1f;
        b.colors    = c;
        b.transition= Selectable.Transition.ColorTint;
    }

    // ── Force all UI sprites to Sprite (2D and UI) import type ───────────
    [MenuItem("Slidoo/Fix Sprite Import Types")]
    public static void FixSpriteImportTypes()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[]{"Assets/UI/Sprites"});
        int fixed_ = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var imp  = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            if (imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType  = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.SaveAndReimport();
                fixed_++;
            }
        }
        AssetDatabase.Refresh();
        Debug.Log($"<color=cyan>[SlidooSceneBuilder] Fixed {fixed_} sprite import types.</color>");
    }

    static Sprite Sp(string sub, string sliceName = null)
    {
        var path = $"Assets/UI/Sprites/{sub}.png";
        // Auto-fix import type if needed
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sliceName != null && imp != null && imp.spriteImportMode == SpriteImportMode.Multiple)
        {
            sp = System.Array.Find(AssetDatabase.LoadAllAssetsAtPath(path),
                asset => asset is Sprite && asset.name == sliceName) as Sprite;
        }
        if (sp == null) Debug.LogWarning($"[Builder] Missing sprite: {path}");
        return sp;
    }

    // Reflection field setter
    static void Set(object obj, string field, object value)
    {
        var f=obj.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        if(f!=null) f.SetValue(obj,value);
        else Debug.LogWarning($"[Builder] Field not found: {obj.GetType().Name}.{field}");
    }
}
#endif
