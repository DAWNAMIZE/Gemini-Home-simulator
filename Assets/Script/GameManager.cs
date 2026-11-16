using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Owns pause / game-over state and builds the death screen at runtime so the
/// scene does not need a hand-wired panel. Drop an existing panel into
/// gameOverPanel if you would rather author the screen yourself.
/// </summary>
public class GameManager : MonoBehaviour
{
    // The Singleton: Allows any script to call GameManager.instance safely
    public static GameManager instance;

    [Header("Game States")]
    public bool isGamePaused = false;
    public bool isGameOver = false;

    [Header("UI Connections")]
    [Tooltip("Optional hand-authored death screen. Leave empty to use the built-in runtime game over screen.")]
    public GameObject gameOverPanel;

    [Header("Game Over Screen")]
    public string gameOverTitle = "GAME OVER";
    public string gameOverMessage = "The night claimed you.";
    public string mainMenuSceneName = "Main Menu";

    private GameObject runtimeGameOverPanel;
    private CanvasGroup gameOverGroup;
    private TMP_Text messageLabel;
    private TMP_Text statsLabel;

    private float sessionStartTime;
    private float fadeAlpha;
    private bool showingGameOver;

    void Awake()
    {
        // Setup the Singleton
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instance != this) return;

        instance = null;
        Time.timeScale = 1f;
    }

    void Start()
    {
        sessionStartTime = Time.unscaledTime;

        BuildGameOverScreen();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Ensure the game starts cleanly
        if (!isGameOver)
        {
            isGamePaused = false;
            Time.timeScale = 1f;
        }
    }

    void Update()
    {
        if (showingGameOver && gameOverGroup != null)
        {
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, 1f, Time.unscaledDeltaTime * 2.5f);
            gameOverGroup.alpha = fadeAlpha;
        }

        if (isGameOver) return;

        CheckPlayerVitals();
    }

    private void CheckPlayerVitals()
    {
        // Find the player's vitals in the scene
        PlayerVitals vitals = PlayerVitals.instance;
        if (vitals == null) vitals = FindFirstObjectByType<PlayerVitals>();

        if (vitals != null && vitals.currentHealth <= 0f)
            TriggerGameOver();
    }

    // ------------------------------------------------------------------
    // GAME STATE CONTROLS
    // ------------------------------------------------------------------

    public void PauseGame()
    {
        if (isGameOver) return;

        isGamePaused = true;
        Time.timeScale = 0f; // This freezes everything (movement, solar power, day/night)

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (isGameOver) return;

        isGamePaused = false;
        Time.timeScale = 1f; // Resumes time

        Debug.Log("Game Resumed");
    }

    // ------------------------------------------------------------------
    // GAME OVER
    // ------------------------------------------------------------------

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        isGamePaused = true;

        // Freeze the world immediately (do not call PauseGame: it early-outs
        // when isGameOver is already true).
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        HideGameplayHud();

        if (gameOverPanel == null) BuildGameOverScreen();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else if (runtimeGameOverPanel != null)
        {
            runtimeGameOverPanel.SetActive(true);

            showingGameOver = true;
            fadeAlpha = 0f;
            if (gameOverGroup != null) gameOverGroup.alpha = 0f;
            if (messageLabel != null) messageLabel.text = gameOverMessage;
            if (statsLabel != null) statsLabel.text = BuildRunStats();
        }

        Debug.Log("GAME OVER!");
    }

    private void BuildGameOverScreen()
    {
        if (runtimeGameOverPanel != null || gameOverPanel != null) return;

        Canvas canvas = RuntimeUI.CreateOverlayCanvas("GameOverCanvas", 5000);
        canvas.transform.SetParent(transform, false);

        runtimeGameOverPanel = canvas.gameObject;
        gameOverGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        gameOverGroup.alpha = 0f;
        gameOverGroup.interactable = true;
        gameOverGroup.blocksRaycasts = true;

        Image background = RuntimeUI.CreateImage(canvas.transform, "Background",
            new Color(0.016f, 0.012f, 0.012f, 0.97f));
        Stretch(background.rectTransform);

        Image topBar = RuntimeUI.CreateImage(canvas.transform, "TopAccent",
            new Color(0.60f, 0.08f, 0.07f, 1f));
        SetEdgeStrip(topBar.rectTransform, true);

        Image bottomBar = RuntimeUI.CreateImage(canvas.transform, "BottomAccent",
            new Color(0.60f, 0.08f, 0.07f, 1f));
        SetEdgeStrip(bottomBar.rectTransform, false);

        TMP_Text title = RuntimeUI.CreateText(canvas.transform, "GameOverTitle",
            gameOverTitle.ToUpperInvariant(), 128f, new Color(0.84f, 0.14f, 0.11f));
        title.fontStyle = FontStyles.Bold;
        SetCentered(title.rectTransform, new Vector2(0.5f, 0.68f),
            new Vector2(1500f, 180f), Vector2.zero);

        messageLabel = RuntimeUI.CreateText(canvas.transform, "GameOverMessage",
            gameOverMessage, 34f, new Color(0.85f, 0.81f, 0.75f, 0.9f));
        SetCentered(messageLabel.rectTransform, new Vector2(0.5f, 0.54f),
            new Vector2(1100f, 60f), Vector2.zero);

        statsLabel = RuntimeUI.CreateText(canvas.transform, "GameOverStats",
            "SURVIVED 00:00", 26f, new Color(0.95f, 0.75f, 0.32f, 0.95f));
        SetCentered(statsLabel.rectTransform, new Vector2(0.5f, 0.45f),
            new Vector2(900f, 48f), Vector2.zero);

        Image divider = RuntimeUI.CreateImage(canvas.transform, "Divider",
            new Color(1f, 1f, 1f, 0.12f));
        SetCentered(divider.rectTransform, new Vector2(0.5f, 0.39f),
            new Vector2(460f, 2f), Vector2.zero);

        RuntimeUI.CreateButton(canvas.transform, "RestartButton", "RESTART",
            new Vector2(330f, 84f), new Vector2(-190f, -260f), new Vector2(0.5f, 0.5f),
            new Color(0.63f, 0.10f, 0.08f, 1f), RestartCurrentLevel);

        RuntimeUI.CreateButton(canvas.transform, "MainMenuButton", "MAIN MENU",
            new Vector2(330f, 84f), new Vector2(190f, -260f), new Vector2(0.5f, 0.5f),
            new Color(0.14f, 0.14f, 0.16f, 1f), ReturnToMainMenu);

        TMP_Text hint = RuntimeUI.CreateText(canvas.transform, "GameOverHint",
            "Tip: save at the bed before the house gets dangerous.", 20f,
            new Color(1f, 1f, 1f, 0.35f));
        SetCentered(hint.rectTransform, new Vector2(0.5f, 0.10f),
            new Vector2(1000f, 34f), Vector2.zero);

        runtimeGameOverPanel.SetActive(false);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetEdgeStrip(RectTransform rt, bool top)
    {
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.sizeDelta = new Vector2(0f, 14f);
        rt.anchoredPosition = Vector2.zero;
    }

    private static void SetCentered(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
    }

    private string BuildRunStats()
    {
        float elapsed = Mathf.Max(0f, Time.unscaledTime - sessionStartTime);

        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        string survived = minutes > 0
            ? minutes + "m " + seconds.ToString("00") + "s"
            : seconds + "s";

        DayNightCycle clock = DayNightCycle.instance;
        if (clock == null) clock = FindFirstObjectByType<DayNightCycle>();

        if (clock != null && clock.dayLengthInSeconds > 0f)
        {
            int day = 1 + Mathf.FloorToInt(elapsed / clock.dayLengthInSeconds);
            return "DAY " + day + "   |   SURVIVED " + survived;
        }

        return "SURVIVED " + survived;
    }

    private void HideGameplayHud()
    {
        PhoneUIManager ui = PhoneUIManager.instance;
        if (ui == null) ui = FindFirstObjectByType<PhoneUIManager>(FindObjectsInactive.Include);

        if (ui != null)
        {
            ui.CloseAllMenus();
            if (ui.tutorialPanel != null) ui.tutorialPanel.SetActive(false);
            if (ui.mobileHUD != null) ui.mobileHUD.SetActive(false);
            if (ui.crosshair != null) ui.crosshair.SetActive(false);
        }

        if (WeaponHUD.instance != null)
            WeaponHUD.instance.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // SCENE MANAGEMENT
    // ------------------------------------------------------------------

    public void RestartCurrentLevel()
    {
        // Unfreeze time before loading, otherwise the new scene will be frozen too!
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Static ammo survives scene loads, so reset it for a fresh run.
        WeaponInstance.PistolAmmo = WeaponInstance.PistolMaxAmmo;
        SaveGameManager.pendingContinue = false;

        // Reloads the exact scene we are currently in
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
            SceneManager.LoadScene(scene.buildIndex);
        else
            SceneManager.LoadScene(scene.name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SaveGameManager.pendingContinue = false;

        // The menu scene is called "Main Menu" in Build Settings.
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
        else
        {
            Debug.LogWarning("GameManager: no main menu scene found in Build Settings.");
            SceneManager.LoadScene(0);
        }
    }
}
