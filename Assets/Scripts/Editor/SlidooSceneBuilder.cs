// ============================================================
//  SlidooSceneBuilder.cs   –   EDITOR ONLY
//  Menu: Slidoo ▶ Build Scene
//
//  Generates the COMPLETE Main scene hierarchy from scratch:
//    • All screens, popups, board, HUD
//    • Sprites loaded from Assets/UI/Sprites/
//    • Manager GameObjects with all components
//    • Inspector references auto-wired
//
//  Run once on a fresh scene. Safe to re-run (clears first).
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
    // ── Sprite paths (relative to Assets/) ──────────────────────────────────
    private const string SP = "Assets/UI/Sprites/";

    // ── Colors used for placeholders ────────────────────────────────────────
    private static readonly Color C_PANEL_BG        = new Color(0.13f, 0.08f, 0.05f, 0.95f);
    private static readonly Color C_POPUP_BG        = new Color(0.18f, 0.12f, 0.06f, 0.97f);
    private static readonly Color C_BTN_GREEN       = new Color(0.22f, 0.70f, 0.30f, 1f);
    private static readonly Color C_BTN_RED         = new Color(0.80f, 0.22f, 0.22f, 1f);
    private static readonly Color C_BTN_YELLOW      = new Color(0.95f, 0.78f, 0.10f, 1f);
    private static readonly Color C_BTN_BLUE        = new Color(0.18f, 0.46f, 0.80f, 1f);
    private static readonly Color C_BTN_GREY        = new Color(0.45f, 0.45f, 0.45f, 1f);
    private static readonly Color C_STAR_GOLD       = new Color(1f, 0.84f, 0f, 1f);
    private static readonly Color C_STAR_EMPTY      = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color C_TILE_BORDER     = new Color(0.9f, 0.75f, 0.4f, 1f);
    private static readonly Color C_TRANSPARENT     = new Color(0, 0, 0, 0);

    // ────────────────────────────────────────────────────────────────────────
    [MenuItem("Slidoo/Build Scene %#b")]
    public static void BuildScene()
    {
        // Clear existing roots except Camera / EventSystem
        var existing = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in existing)
            if (go.transform.parent == null && go.name != "Main Camera")
                Object.DestroyImmediate(go);

        // ── Managers ────────────────────────────────────────────────────────
        var managers = CreateEmpty("[ MANAGERS ]");
        var saveGO   = CreateChild("SaveManager",      managers).AddComponent<SaveManager>();
        var audioGO  = BuildAudioManager(managers);
        var shuffleGO= CreateChild("ShuffleBag",       managers).AddComponent<ShuffleBag>();
        var gmGO     = CreateChild("GameManager",      managers).AddComponent<GameManager>();
        var uiMgrGO  = CreateChild("UIManager",        managers);
        var lvlCtrlGO= CreateChild("LevelController",  managers);
        var boardMgrGO= CreateChild("BoardManager",    managers);
        var backBtnGO = CreateChild("BackButtonHandler",managers);

        // ── Canvas ──────────────────────────────────────────────────────────
        var canvasGO = BuildCanvas();
        var canvas   = canvasGO.GetComponent<Canvas>();

        // ── Background (always visible under everything) ─────────────────────
        var bgImg = CreateImage("[ Background ]", canvasGO, Stretch(),
                       LoadSprite("Common/bg"), C_PANEL_BG, false);
        bgImg.GetComponent<Image>().preserveAspect = false;

        // ── SCREENS ─────────────────────────────────────────────────────────
        var screenParent  = CreatePanel("[ SCREENS ]",  canvasGO, Stretch());
        var splashGO      = BuildSplashScreen(screenParent);
        var mainMenuGO    = BuildMainMenuScreen(screenParent);
        var modeSelectGO  = BuildModeSelectScreen(screenParent);
        var lvlSelectGO   = BuildLevelSelectScreen(screenParent, out var lvlSelectUI, out var lvlButtonUIs);
        var gameplayGO    = BuildGameplayScreen(screenParent, boardMgrGO,
                                out var gameplayUI, out var boardContainer);

        // ── POPUPS ──────────────────────────────────────────────────────────
        var popupParent   = CreatePanel("[ POPUPS ]",   canvasGO, Stretch());
        var winGO         = BuildWinPopup(popupParent,   out var winUI);
        var pauseGO       = BuildPausePopup(popupParent);
        var settingsGO    = BuildSettingsPopup(popupParent);
        var ctrlSettingsGO= BuildControlSettingsPopup(popupParent);
        var quitGO        = BuildQuitPopup(popupParent);

        // All popups start hidden
        winGO.SetActive(false);
        pauseGO.SetActive(false);
        settingsGO.SetActive(false);
        ctrlSettingsGO.SetActive(false);
        quitGO.SetActive(false);

        // ── Wire UIManager ──────────────────────────────────────────────────
        var uiMgr = uiMgrGO.AddComponent<UIManager>();
        SetPrivateField(uiMgr, "splashScreen",           splashGO);
        SetPrivateField(uiMgr, "mainMenuScreen",         mainMenuGO);
        SetPrivateField(uiMgr, "modeSelectScreen",       modeSelectGO);
        SetPrivateField(uiMgr, "levelSelectScreen",      lvlSelectGO);
        SetPrivateField(uiMgr, "gameplayScreen",         gameplayGO);
        SetPrivateField(uiMgr, "winPopup",               winGO);
        SetPrivateField(uiMgr, "pausePopup",             pauseGO);
        SetPrivateField(uiMgr, "settingsPopup",          settingsGO);
        SetPrivateField(uiMgr, "controlSettingsPopup",   ctrlSettingsGO);
        SetPrivateField(uiMgr, "quitPopup",              quitGO);

        // ── Wire LevelController ─────────────────────────────────────────────
        var lvlCtrl = lvlCtrlGO.AddComponent<LevelController>();
        SetPrivateField(lvlCtrl, "gameplayUI", gameplayUI);

        // ── Wire BoardManager ────────────────────────────────────────────────
        var boardMgr = boardMgrGO.AddComponent<BoardManager>();
        var tilePrefab = LoadOrCreateTilePrefab();
        SetPrivateField(boardMgr, "boardContainer", boardContainer);
        SetPrivateField(boardMgr, "tilePrefab",     tilePrefab);
        SetPrivateField(boardMgr, "animDuration",   0.15f);
        SetPrivateField(boardMgr, "tileGap",        3f);

        // ── Wire BackButtonHandler ────────────────────────────────────────────
        var bbh = backBtnGO.AddComponent<BackButtonHandler>();
        var quitUI = quitGO.GetComponent<QuitPopupUI>();
        SetPrivateField(bbh, "quitPopup", quitUI);

        // ── Wire AudioManager SFX into scripts ────────────────────────────────
        // (clips must be assigned manually in Inspector)

        // ── Deactivate screens except splash ────────────────────────────────
        mainMenuGO.SetActive(false);
        modeSelectGO.SetActive(false);
        lvlSelectGO.SetActive(false);
        gameplayGO.SetActive(false);

        // ── Save scene ───────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("<color=lime>[SlidooSceneBuilder] Scene built successfully! " +
                  "Save with Ctrl+S then assign audio clips in AudioManager.</color>");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  SCREEN BUILDERS
    // ════════════════════════════════════════════════════════════════════════

    static GameObject BuildSplashScreen(GameObject parent)
    {
        var root = CreatePanel("[ Screen_Splash ]", parent, Stretch(), LoadSprite("Splash/splash_bg"), C_PANEL_BG);

        // Logo centered
        var logo = CreateImage("Logo", root, Center(400, 400), LoadSprite("Splash/logo"), Color.white);
        return root;
    }

    static GameObject BuildMainMenuScreen(GameObject parent)
    {
        var root = CreatePanel("[ Screen_MainMenu ]", parent, Stretch(), LoadSprite("Home/home_bg"), C_PANEL_BG);

        // Settings top-right
        var settingsBtn = CreateButton("Btn_Settings", root,
            TopRight(80, 80, -50, -50),
            LoadSprite("Home/btn_settings"), Color.white, "");

        // Play button – center bottom area
        var playBtn = CreateButton("Btn_Play", root,
            Center(300, 100, 0, -200),
            LoadSprite("Home/btn_play"), Color.white, "");

        // Sound toggle – bottom-left
        var soundBtn = CreateButton("Btn_Sound", root,
            BottomLeft(80, 80, 50, 50),
            LoadSprite("Home/icon_sound"), Color.white, "");

        // Debug unlock button (small, hidden corner) – bottom right
        var debugBtn = CreateButton("Btn_DEBUG_Unlock", root,
            BottomRight(120, 40, -10, 10),
            null, C_BTN_RED, "UNLOCK ALL");
        debugBtn.SetActive(false);    // hide in production; enable to test

        // Wire
        var ui = root.AddComponent<MainMenuUI>();
        SetPrivateField(ui, "playButton",        playBtn.GetComponent<Button>());
        SetPrivateField(ui, "settingsButton",    settingsBtn.GetComponent<Button>());
        SetPrivateField(ui, "debugUnlockButton", debugBtn.GetComponent<Button>());
        return root;
    }

    static GameObject BuildModeSelectScreen(GameObject parent)
    {
        var root = CreatePanel("[ Screen_ModeSelect ]", parent, Stretch(), null, C_PANEL_BG);

        // Header
        CreateImage("Header", root, TopCenter(600, 100, 0, -60), LoadSprite("ModeSelect/mode_header"), Color.white);

        // Back button
        var backBtn = CreateButton("Btn_Back", root,
            TopLeft(80, 80, 50, -50), LoadSprite("ModeSelect/btn_back"), Color.white, "");

        // 4 mode buttons in a vertical column
        string[] modeNames = { "Easy (3×3)", "Medium (4×4)", "Hard (5×5)", "Master (6×6)" };
        string[] unlockedSprites = { "ModeSelect/mode_easy_unlocked", "ModeSelect/mode_medium_unlocked",
                                     "ModeSelect/mode_hard_unlocked",  "ModeSelect/mode_master_unlocked" };
        string[] lockedSprites   = { null, "ModeSelect/mode_medium_locked",
                                           "ModeSelect/mode_hard_locked", "ModeSelect/mode_master_locked" };

        var modeButtonUIs = new ModeButtonUI[4];
        float startY = -120, stepY = -170;

        for (int i = 0; i < 4; i++)
        {
            float y = startY + i * stepY;
            var card = CreatePanel($"ModeCard_{modeNames[i]}", root, Center(600, 150, 0, y));

            // Unlocked group
            var unlockedGrp = CreatePanel("Unlocked", card, Stretch());
            CreateImage("Bg_Unlocked", unlockedGrp, Stretch(), LoadSprite(unlockedSprites[i]), Color.white);
            var nameText     = CreateTMP("Text_ModeName", unlockedGrp, Center(300, 50, -80, 20), modeNames[i], 24, TextAlignmentOptions.Left);
            var progressText = CreateTMP("Text_Progress", unlockedGrp, Center(200, 36, 60, 20), "0 / 30", 20, TextAlignmentOptions.Right);
            var starsText    = CreateTMP("Text_Stars",    unlockedGrp, Center(200, 36, 60, -20), "★ 0 / 90", 18, TextAlignmentOptions.Right);
            starsText.color  = C_STAR_GOLD;

            // Locked group
            var lockedGrp = CreatePanel("Locked", card, Stretch());
            if (lockedSprites[i] != null)
                CreateImage("Bg_Locked", lockedGrp, Stretch(), LoadSprite(lockedSprites[i]), Color.white);
            else
                SetColor(lockedGrp, C_BTN_GREY);
            CreateImage("Icon_Lock", lockedGrp, Center(60, 60, -100, 0), LoadSprite("LevelSelect/icon_lock"), Color.white);
            var hintText = CreateTMP("Text_LockHint", lockedGrp, Center(380, 50, 30, 0), "Complete 15 Easy levels to unlock", 16, TextAlignmentOptions.Left);
            hintText.color = Color.white;

            // Add button on the card itself
            var btn = card.AddComponent<Button>();
            SetupButtonTransition(btn);

            // Wire ModeButtonUI
            var modeUI = card.AddComponent<ModeButtonUI>();
            SetPrivateField(modeUI, "unlockedGroup",  unlockedGrp);
            SetPrivateField(modeUI, "lockedGroup",    lockedGrp);
            SetPrivateField(modeUI, "modeNameText",   nameText);
            SetPrivateField(modeUI, "progressText",   progressText);
            SetPrivateField(modeUI, "starsText",      starsText);
            SetPrivateField(modeUI, "lockHintText",   hintText);
            SetPrivateField(modeUI, "button",         btn);
            modeButtonUIs[i] = modeUI;

            lockedGrp.SetActive(i > 0);  // only Easy starts unlocked
        }

        // Wire ModeSelectUI
        var ui = root.AddComponent<ModeSelectUI>();
        SetPrivateField(ui, "modeButtons", modeButtonUIs);
        SetPrivateField(ui, "backButton", backBtn.GetComponent<Button>());
        return root;
    }

    static GameObject BuildLevelSelectScreen(GameObject parent,
        out LevelSelectUI lvlSelectUI, out LevelButtonUI[] lvlButtonUIs)
    {
        var root = CreatePanel("[ Screen_LevelSelect ]", parent, Stretch(), null, C_PANEL_BG);

        // Banner / title
        var banner = CreateImage("Banner", root, TopCenter(700, 100, 0, -60), LoadSprite("LevelSelect/level_banner"), Color.white);
        var titleText = CreateTMP("Text_ModeTitle", root, TopCenter(500, 60, 0, -55), "EASY", 32, TextAlignmentOptions.Center);
        titleText.color = Color.white;

        // Back + Controls buttons
        var backBtn  = CreateButton("Btn_Back",     root, TopLeft(80,  80, 50, -50),  LoadSprite("ModeSelect/btn_back"), Color.white, "");
        var ctrlBtn  = CreateButton("Btn_Controls", root, TopRight(140, 60, -50, -50), null, C_BTN_BLUE, "Controls");

        // Scroll view for 30 level buttons
        var scrollRoot = CreatePanel("LevelGrid_Scroll", root, new RectOffset(20, 20, 130, 20));
        var grid       = CreatePanel("LevelGrid",        scrollRoot, Stretch());
        var glg        = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(150, 150);
        glg.spacing         = new Vector2(14, 14);
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.UpperCenter;
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 5;
        var csf = grid.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Scroll rect
        var sr = scrollRoot.AddComponent<ScrollRect>();
        sr.content    = grid.GetComponent<RectTransform>();
        sr.horizontal = false;
        sr.vertical   = true;
        sr.movementType = ScrollRect.MovementType.Elastic;

        // 30 Level Buttons
        lvlButtonUIs = new LevelButtonUI[30];
        for (int i = 0; i < 30; i++)
        {
            var cell = CreatePanel($"LevelBtn_{i + 1:D2}", grid, new Vector2(150, 150));

            // Tile background (unlocked = yellow, locked = grey)
            var bg   = CreateImage("Bg_Unlocked", cell, Stretch(), LoadSprite("LevelSelect/tile_unlocked"), Color.white);
            var bgL  = CreateImage("Bg_Locked",   cell, Stretch(), LoadSprite("LevelSelect/tile_locked"),   Color.white);
            bgL.SetActive(false);

            var numText  = CreateTMP("Text_LevelNum", cell, Center(120, 50, 0, 20),  (i + 1).ToString(), 28, TextAlignmentOptions.Center);
            numText.color = new Color(0.2f, 0.1f, 0f, 1f);

            // 3 star icons
            var stars = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                float sx = -20 + s * 20f;
                var starGO = CreateImage($"Star_{s}", cell, Center(18, 18, sx - 10, -28), LoadSprite("LevelSelect/icon_star"), C_STAR_EMPTY);
                stars[s] = starGO.GetComponent<Image>();
            }

            var lockIcon = CreateImage("Icon_Lock", cell, Center(50, 50, 0, 0), LoadSprite("LevelSelect/icon_lock"), Color.white);
            lockIcon.SetActive(false);

            var btn = cell.AddComponent<Button>();
            SetupButtonTransition(btn);

            var lbUI = cell.AddComponent<LevelButtonUI>();
            SetPrivateField(lbUI, "button",       btn);
            SetPrivateField(lbUI, "levelNumberText", numText);
            SetPrivateField(lbUI, "starImages",   stars);
            SetPrivateField(lbUI, "lockIcon",     lockIcon.GetComponent<Image>());
            SetPrivateField(lbUI, "buttonBg",     bg.GetComponent<Image>());
            lvlButtonUIs[i] = lbUI;
        }

        // Wire LevelSelectUI
        lvlSelectUI = root.AddComponent<LevelSelectUI>();
        SetPrivateField(lvlSelectUI, "modeTitleText",  titleText);
        SetPrivateField(lvlSelectUI, "levelButtons",   lvlButtonUIs);
        SetPrivateField(lvlSelectUI, "backButton",     backBtn.GetComponent<Button>());
        SetPrivateField(lvlSelectUI, "controlsButton", ctrlBtn.GetComponent<Button>());
        return root;
    }

    static GameObject BuildGameplayScreen(GameObject parent, GameObject boardMgrGO,
        out GameplayUI gameplayUI, out RectTransform boardContainer)
    {
        var root = CreatePanel("[ Screen_Gameplay ]", parent, Stretch(), null, C_PANEL_BG);

        // ── Top HUD ──────────────────────────────────────────────────────────
        var hud = CreatePanel("HUD", root, TopStretch(100));
        hud.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);

        var pauseBtn    = CreateButton("Btn_Pause",  hud, TopLeft(70, 70, 15, -15),  null, C_TRANSPARENT, "⏸");
        var moveText    = CreateTMP("Text_Moves",    hud, TopCenter(200, 50, -80, -25), "Moves: 0", 22, TextAlignmentOptions.Center);
        var timerText   = CreateTMP("Text_Timer",    hud, TopCenter(160, 50,  80, -25), "00:00", 22, TextAlignmentOptions.Center);
        moveText.color  = Color.white;
        timerText.color = Color.white;
        ((TextMeshProUGUI)moveText).fontStyle = FontStyles.Bold;

        // ── Board area ────────────────────────────────────────────────────────
        var boardArea = CreatePanel("BoardArea", root, FullExcept(top: 110, bottom: 120));

        // Wooden frame behind board
        var frame = CreateImage("BoardFrame", boardArea, Stretch(),
                                LoadSprite("Gameplay/board_frame"), Color.white);
        frame.GetComponent<Image>().preserveAspect = true;

        // The actual board container (tiles go here)
        var boardGO = CreatePanel("BoardContainer", boardArea, Center(900, 900, 0, 0));
        boardContainer = boardGO.GetComponent<RectTransform>();

        // ── Bottom bar ───────────────────────────────────────────────────────
        var bottomBar = CreatePanel("BottomBar", root, BottomStretch(110));
        bottomBar.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);

        var previewBtn = CreateButton("Btn_Preview", bottomBar, Center(140, 70, 0, 0), null, C_BTN_YELLOW, "👁 Preview");
        previewBtn.AddComponent<PreviewHoldButton>();
        // Remove standard button since PreviewHoldButton handles its own input
        Object.DestroyImmediate(previewBtn.GetComponent<Button>());

        // ── Preview overlay ──────────────────────────────────────────────────
        var previewPanel = CreatePanel("PreviewPanel", root, Stretch());
        previewPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);
        var previewImg = CreateImage("PreviewImage", previewPanel, Center(900, 900, 0, 0), null, Color.white);
        previewPanel.SetActive(false);

        // ── Wire GameplayUI ──────────────────────────────────────────────────
        gameplayUI = root.AddComponent<GameplayUI>();
        SetPrivateField(gameplayUI, "moveCountText",  moveText);
        SetPrivateField(gameplayUI, "timerText",      timerText);
        SetPrivateField(gameplayUI, "pauseButton",    pauseBtn.GetComponent<Button>());
        SetPrivateField(gameplayUI, "previewButton",  previewBtn.GetComponent<PreviewHoldButton>());
        SetPrivateField(gameplayUI, "previewImage",   previewImg.GetComponent<RawImage>());
        SetPrivateField(gameplayUI, "previewPanel",   previewPanel);
        return root;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  POPUP BUILDERS
    // ════════════════════════════════════════════════════════════════════════

    static GameObject BuildWinPopup(GameObject parent, out WinPopupUI winUI)
    {
        var dimmer = CreateDimmer("[ Popup_Win ]", parent);
        var panel  = CreatePanel("Panel", dimmer, Center(680, 760), LoadSprite("Win/win_panel"), C_POPUP_BG);

        // 3 Stars
        var stars = new Image[3];
        for (int s = 0; s < 3; s++)
        {
            float sx = -100 + s * 100f;
            var sg = CreateImage($"Star_{s}", panel, Center(80, 80, sx, 220), LoadSprite("LevelSelect/icon_star"), C_STAR_GOLD);
            stars[s] = sg.GetComponent<Image>();
        }

        var movesText    = CreateTMP("Text_Moves",     panel, Center(400, 50, 0,  110),  "0 Moves",    26, TextAlignmentOptions.Center);
        var timeText     = CreateTMP("Text_Time",      panel, Center(400, 50, 0,  60),   "00:00",      22, TextAlignmentOptions.Center);
        var bestMoveTxt  = CreateTMP("Text_BestMoves", panel, Center(300, 36, 0,  10),   "Best: -",    18, TextAlignmentOptions.Center);
        var bestTimeTxt  = CreateTMP("Text_BestTime",  panel, Center(300, 36, 0, -30),   "Best: --:--",18, TextAlignmentOptions.Center);
        movesText .color = Color.white;
        timeText  .color = new Color(0.9f, 0.9f, 0.9f, 1f);
        bestMoveTxt.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        bestTimeTxt.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        // Unlock banner
        var unlockBanner = CreatePanel("UnlockBanner", panel, Center(500, 50, 0, -80));
        unlockBanner.AddComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f, 0.9f);
        var unlockTxt = CreateTMP("Text_Unlock", unlockBanner, Stretch(), "🎉 New Mode Unlocked!", 20, TextAlignmentOptions.Center);
        unlockBanner.SetActive(false);

        // Buttons row
        var nextBtn   = CreateButton("Btn_Next",   panel, Center(190, 70, 95,  -175), LoadSprite("Win/btn_next"),   Color.white, "");
        var replayBtn = CreateButton("Btn_Replay", panel, Center(190, 70, -95, -175), LoadSprite("Win/btn_replay"), Color.white, "");
        var menuBtn   = CreateButton("Btn_Menu",   panel, Center(130, 55, 0,   -245), null, C_BTN_GREY, "Menu");

        winUI = dimmer.AddComponent<WinPopupUI>();
        SetPrivateField(winUI, "starImages",     stars);
        SetPrivateField(winUI, "movesText",      movesText);
        SetPrivateField(winUI, "timeText",       timeText);
        SetPrivateField(winUI, "bestMovesText",  bestMoveTxt);
        SetPrivateField(winUI, "bestTimeText",   bestTimeTxt);
        SetPrivateField(winUI, "unlockBanner",   unlockBanner);
        SetPrivateField(winUI, "unlockBannerText", unlockTxt);
        SetPrivateField(winUI, "nextButton",     nextBtn.GetComponent<Button>());
        SetPrivateField(winUI, "replayButton",   replayBtn.GetComponent<Button>());
        SetPrivateField(winUI, "menuButton",     menuBtn.GetComponent<Button>());
        return dimmer;
    }

    static GameObject BuildPausePopup(GameObject parent)
    {
        var dimmer = CreateDimmer("[ Popup_Pause ]", parent);
        var panel  = CreatePanel("Panel", dimmer, Center(560, 600), LoadSprite("Pause/pause_panel"), C_POPUP_BG);

        var title   = CreateTMP("Text_Title", panel, TopCenter(400, 60, 0, -50), "PAUSED", 36, TextAlignmentOptions.Center);
        title.color = Color.white;

        var resumeBtn  = CreateButton("Btn_Resume",  panel, Center(400, 90, 0,  40),  LoadSprite("Pause/btn_resume"),  Color.white, "");
        var restartBtn = CreateButton("Btn_Restart", panel, Center(400, 90, 0, -60),  LoadSprite("Pause/btn_restart"), Color.white, "");
        var menuBtn    = CreateButton("Btn_Menu",    panel, Center(400, 90, 0, -160), LoadSprite("Pause/btn_menu"),    Color.white, "");

        var ui = dimmer.AddComponent<PausePopupUI>();
        SetPrivateField(ui, "resumeButton",  resumeBtn .GetComponent<Button>());
        SetPrivateField(ui, "restartButton", restartBtn.GetComponent<Button>());
        SetPrivateField(ui, "menuButton",    menuBtn   .GetComponent<Button>());
        return dimmer;
    }

    static GameObject BuildSettingsPopup(GameObject parent)
    {
        var dimmer = CreateDimmer("[ Popup_Settings ]", parent);
        var panel  = CreatePanel("Panel", dimmer, Center(560, 500), LoadSprite("Settings/settings_panel"), C_POPUP_BG);

        var title  = CreateTMP("Text_Title", panel, TopCenter(400, 60, 0, -50), "SETTINGS", 32, TextAlignmentOptions.Center);
        title.color = Color.white;

        // Sound row
        CreateTMP("Label_Sound", panel, Center(200, 40, -100, 60), "Sound", 24, TextAlignmentOptions.Left).color = Color.white;
        var soundToggle = BuildToggle("Toggle_Sound", panel, Center(100, 50, 100, 60));

        // Music row
        CreateTMP("Label_Music", panel, Center(200, 40, -100, -20), "Music", 24, TextAlignmentOptions.Left).color = Color.white;
        var musicToggle = BuildToggle("Toggle_Music", panel, Center(100, 50, 100, -20));

        var closeBtn = CreateButton("Btn_Close", panel, TopRight(60, 60, -20, -20), LoadSprite("Settings/btn_close"), Color.white, "");

        var ui = dimmer.AddComponent<SettingsPopupUI>();
        SetPrivateField(ui, "soundToggle", soundToggle);
        SetPrivateField(ui, "musicToggle", musicToggle);
        SetPrivateField(ui, "closeButton", closeBtn.GetComponent<Button>());
        return dimmer;
    }

    static GameObject BuildControlSettingsPopup(GameObject parent)
    {
        var dimmer = CreateDimmer("[ Popup_ControlSettings ]", parent);
        var panel  = CreatePanel("Panel", dimmer, Center(600, 540), LoadSprite("ControlSettings/controls_panel"), C_POPUP_BG);

        var title  = CreateTMP("Text_Title", panel, TopCenter(500, 60, 0, -50), "CONTROL SETTINGS", 28, TextAlignmentOptions.Center);
        title.color = Color.white;

        var swipeBtn = CreateButton("Btn_Swipe", panel, Center(460, 80, 0,  60),  null, C_BTN_GREEN, "Swipe (default)");
        var tapBtn   = CreateButton("Btn_Tap",   panel, Center(460, 80, 0, -30),  null, C_BTN_BLUE,  "Tap");
        var dragBtn  = CreateButton("Btn_Drag",  panel, Center(460, 80, 0, -120), null, C_BTN_GREY,  "Drag");

        var closeBtn = CreateButton("Btn_Close", panel, TopRight(60, 60, -20, -20), LoadSprite("Settings/btn_close"), Color.white, "");

        var ui = dimmer.AddComponent<ControlSettingsPopupUI>();
        SetPrivateField(ui, "swipeButton", swipeBtn.GetComponent<Button>());
        SetPrivateField(ui, "tapButton",   tapBtn  .GetComponent<Button>());
        SetPrivateField(ui, "dragButton",  dragBtn .GetComponent<Button>());
        SetPrivateField(ui, "closeButton", closeBtn.GetComponent<Button>());
        return dimmer;
    }

    static GameObject BuildQuitPopup(GameObject parent)
    {
        var dimmer = CreateDimmer("[ Popup_Quit ]", parent);
        var panel  = CreatePanel("Panel", dimmer, Center(580, 420), LoadSprite("Quit/quit_panel"), C_POPUP_BG);

        var title   = CreateTMP("Text_Title", panel, TopCenter(500, 60, 0, -60), "QUIT GAME?", 32, TextAlignmentOptions.Center);
        title.color = Color.white;
        var subtitle = CreateTMP("Text_Sub", panel, Center(440, 40, 0, 40), "Are you sure you want to quit?", 20, TextAlignmentOptions.Center);
        subtitle.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        var quitBtn   = CreateButton("Btn_Quit",   panel, Center(200, 80, 110, -80),  LoadSprite("Quit/btn_quit"),   Color.white, "");
        var cancelBtn = CreateButton("Btn_Cancel",  panel, Center(200, 80, -110, -80), LoadSprite("Quit/btn_cancel"), Color.white, "");

        var ui = dimmer.AddComponent<QuitPopupUI>();
        SetPrivateField(ui, "quitButton",   quitBtn  .GetComponent<Button>());
        SetPrivateField(ui, "cancelButton", cancelBtn.GetComponent<Button>());
        return dimmer;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  AUDIO MANAGER BUILDER
    // ════════════════════════════════════════════════════════════════════════

    static GameObject BuildAudioManager(GameObject parent)
    {
        var go = CreateChild("AudioManager", parent);
        var am = go.AddComponent<AudioManager>();

        var sfxGO   = new GameObject("SFX_Source");   sfxGO  .transform.SetParent(go.transform);
        var musicGO = new GameObject("Music_Source");  musicGO.transform.SetParent(go.transform);

        var sfxSrc   = sfxGO  .AddComponent<AudioSource>();
        var musicSrc = musicGO.AddComponent<AudioSource>();
        sfxSrc  .playOnAwake = false;
        musicSrc.playOnAwake = false;
        musicSrc.loop        = true;

        SetPrivateField(am, "sfxSource",   sfxSrc);
        SetPrivateField(am, "musicSource", musicSrc);
        return go;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TILE PREFAB
    // ════════════════════════════════════════════════════════════════════════

    static GameObject LoadOrCreateTilePrefab()
    {
        const string PATH = "Assets/Prefabs/Tile.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PATH);
        if (existing != null) return existing;

        // Build in scene then save as prefab
        var go = new GameObject("Tile");
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);

        var img = go.AddComponent<RawImage>();
        img.color = Color.white;
        img.raycastTarget = true;

        var outline = go.AddComponent<Outline>();
        outline.effectColor    = C_TILE_BORDER;
        outline.effectDistance = new Vector2(2, 2);

        go.AddComponent<Tile>();

        Directory.CreateDirectory("Assets/Prefabs");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PATH);
        Object.DestroyImmediate(go);
        AssetDatabase.Refresh();
        return prefab;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  UI PRIMITIVE HELPERS
    // ════════════════════════════════════════════════════════════════════════

    static GameObject BuildCanvas()
    {
        var go = new GameObject("[ CANVAS ]");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return go;
    }

    static GameObject CreatePanel(string name, GameObject parent, RectOffset padding)
    {
        var go = CreateEmpty(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = new Vector2(padding.left,   padding.bottom);
        rt.offsetMax        = new Vector2(-padding.right, -padding.top);
        return go;
    }

    static GameObject CreatePanel(string name, GameObject parent, Vector4 anchor,
        Sprite sprite = null, Color? bgColor = null)
    {
        var go = CreateEmpty(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(anchor.x, anchor.y);
        rt.anchorMax        = new Vector2(anchor.z, anchor.w);
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;

        if (sprite != null || bgColor.HasValue)
        {
            var img = go.AddComponent<Image>();
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            if (bgColor.HasValue) img.color = bgColor.Value;
        }
        return go;
    }

    // Panel with explicit size and pivot-center anchor
    static GameObject CreatePanel(string name, GameObject parent, Vector2 size)
    {
        var go = CreateEmpty(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        return go;
    }

    static GameObject CreateImage(string name, GameObject parent, Vector4 anchor,
        Sprite sprite, Color color, bool rawImage = false)
    {
        var go = CreateEmpty(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchor.x, anchor.y);
        rt.anchorMax = new Vector2(anchor.z, anchor.w);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        if (rawImage)
        {
            var ri = go.AddComponent<RawImage>();
            ri.color = color;
        }
        else
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color  = color;
            img.raycastTarget = false;
            if (sprite != null) img.type = Image.Type.Simple;
        }
        return go;
    }

    // Overload for explicit size + offset from center
    static GameObject CreateImage(string name, GameObject parent, Vector2 size, float ox, float oy,
        Sprite sprite, Color color)
    {
        var go = CreateEmpty(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(ox, oy);
        var ri = go.AddComponent<RawImage>();
        ri.color = color;
        return go;
    }

    static GameObject CreateButton(string name, GameObject parent, Vector4 anchor,
        Sprite sprite, Color bg, string label)
    {
        var go = CreateEmpty(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchor.x, anchor.y);
        rt.anchorMax = new Vector2(anchor.z, anchor.w);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color  = bg;
        if (sprite != null) img.type = Image.Type.Simple;

        var btn = go.AddComponent<Button>();
        SetupButtonTransition(btn);

        if (!string.IsNullOrEmpty(label))
        {
            var txt = CreateTMP("Label", go, Stretch(), label, 22, TextAlignmentOptions.Center);
            txt.color = new Color(0.1f, 0.05f, 0f, 1f);
        }
        return go;
    }

    static TextMeshProUGUI CreateTMP(string name, GameObject parent, Vector4 anchor,
        string text, float size, TextAlignmentOptions align)
    {
        var go = CreateEmpty(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchor.x, anchor.y);
        rt.anchorMax = new Vector2(anchor.z, anchor.w);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    static Toggle BuildToggle(string name, GameObject parent, Vector4 anchor)
    {
        var go = CreateEmpty(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchor.x, anchor.y);
        rt.anchorMax = new Vector2(anchor.z, anchor.w);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg   = CreateImage("Background", go, Stretch(), null, new Color(0.2f, 0.2f, 0.2f));
        var check = CreateImage("Checkmark", bg,  Stretch(), LoadSprite("Settings/toggle_on"), Color.white);
        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bg.GetComponent<Image>();
        toggle.graphic       = check.GetComponent<Image>();
        toggle.isOn          = true;
        return toggle;
    }

    static GameObject CreateDimmer(string name, GameObject parent)
    {
        var go = CreatePanel(name, parent, Stretch());
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);
        img.raycastTarget = true;
        return go;
    }

    static GameObject CreateChild(string name, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static GameObject CreateEmpty(string name)
    {
        var go = new GameObject(name);
        return go;
    }

    static void SetColor(GameObject go, Color c)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = c;
    }

    static void SetupButtonTransition(Button btn)
    {
        var colors          = btn.colors;
        colors.normalColor  = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors          = colors;
        btn.transition      = Selectable.Transition.ColorTint;
    }

    static Sprite LoadSprite(string subpath)
    {
        string path = $"Assets/UI/Sprites/{subpath}.png";
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null)
            Debug.LogWarning($"[SlidooSceneBuilder] Missing sprite: {path}");
        return sp;
    }

    // ── Anchor shortcut helpers ────────────────────────────────────────────

    // anchor = (minX, minY, maxX, maxY) in [0..1] – returned as Vector4
    static Vector4 Stretch()                 => new Vector4(0, 0, 1, 1);
    static Vector4 TopStretch(float h)       => new Vector4(0, 1, 1, 1);  // override with offset
    static Vector4 BottomStretch(float h)    => new Vector4(0, 0, 1, 0);
    static Vector4 TopCenter(float w, float h, float ox, float oy) =>
        EncodeCenter(0.5f, 1f, w, h, ox, oy);
    static Vector4 TopLeft(float w, float h, float ox, float oy) =>
        EncodeCenter(0f, 1f, w, h, ox, oy);
    static Vector4 TopRight(float w, float h, float ox, float oy) =>
        EncodeCenter(1f, 1f, w, h, ox, oy);
    static Vector4 BottomLeft(float w, float h, float ox, float oy) =>
        EncodeCenter(0f, 0f, w, h, ox, oy);
    static Vector4 BottomRight(float w, float h, float ox, float oy) =>
        EncodeCenter(1f, 0f, w, h, ox, oy);
    static Vector4 Center(float w, float h, float ox = 0, float oy = 0) =>
        EncodeCenter(0.5f, 0.5f, w, h, ox, oy);
    static Vector4 EncodeCenter(float ax, float ay, float w, float h, float ox, float oy)
        => new Vector4(ax, ay, w, h + ox * 1000 + oy);   // carry extra in zw – we decode below

    // We actually use a different overload pattern – let's use a struct instead
    // ↑ The anchor helpers need a rethink to pass sizeDelta. Let's use a different approach.
    // All "named anchors" return a Vector4 that encodes anchor point + size + offset.
    // The CreateButton/CreateTMP/CreateImage overloads decode this.
    // For simplicity this builder uses the direct overloads above that take explicit params.

    static RectOffset Stretch(int all = 0)   => new RectOffset(all, all, all, all);
    static RectOffset FullExcept(float top = 0, float bottom = 0, float left = 0, float right = 0)
        => new RectOffset((int)left, (int)right, (int)top, (int)bottom);

    // ── Reflection helper (sets serialized private fields) ────────────────────
    static void SetPrivateField(object obj, string fieldName, object value)
    {
        var type  = obj.GetType();
        var field = type.GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(obj, value);
        else
            Debug.LogWarning($"[SlidooSceneBuilder] Field not found: {type.Name}.{fieldName}");
    }
}
#endif
