using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhoneUIManager : MonoBehaviour
{
    public static PhoneUIManager instance;

    [Header("Core UI Panels")]
    public GameObject mobileHUD;       
    public GameObject backpackPanel;   
    public GameObject prepStationUI;   
    public GameObject tutorialPanel; 
    public GameObject playerPhonePanel;

    [Header("Survival Vitals")]
    public Slider healthSlider;
    public Slider hungerSlider;
    public Slider thirstSlider;
    public Slider hygieneSlider;
    public Slider sanitySlider;
    public Slider solarPowerSlider;
    public Slider waterTankSlider;

    [Header("Systems")]
    public PlayerVitals playerVitals;
    public WaterTankSystem waterTank;
    public SolarSystem solarSystem;
    public MobilePhoneSystem phoneSystem;

    [Header("Battery Sliders & Labels")]
    public Slider phoneBatterySlider;
    public TMP_Text solarValueLabel;
    public TMP_Text phoneBatteryValueLabel;

    [Header("World Clock")]
    public TMP_Text worldClockLabel;

    [Header("Survival Bar Colors")]
    public Color healthBarColor = new Color(0.91f, 0.25f, 0.29f, 1f);
    public Color hungerBarColor = new Color(0.98f, 0.66f, 0.24f, 1f);
    public Color thirstBarColor = new Color(0.24f, 0.78f, 0.98f, 1f);
    public Color hygieneBarColor = new Color(0.35f, 0.86f, 0.60f, 1f);
    public Color sanityBarColor = new Color(0.68f, 0.50f, 0.98f, 1f);
    public Color vitalsTrackColor = new Color(0.10f, 0.11f, 0.14f, 0.92f);

    [Header("Mobile Action Buttons")]
    public GameObject interactButton;  
    public GameObject dropButton;      

    [Header("Idle HUD Auto-Hide")]
    public bool autoHideHud = true;
    [Tooltip("Seconds of no player input before the HUD fades out of the way.")]
    public float idleHideSeconds = 120f;
    public GameObject crosshair;

    private float lastActivityTime;
    private bool hudHiddenByIdle;
    private Vector3 lastMousePosition;

    private TMP_Text healthValueLabel;
    private TMP_Text hungerValueLabel;
    private TMP_Text thirstValueLabel;
    private TMP_Text hygieneValueLabel;
    private TMP_Text sanityValueLabel;
    private int lastToggleFrame = -1;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        Screen.orientation = ScreenOrientation.LandscapeLeft;

        CacheSurvivalUI();
        CacheSceneSystems();
        EnsureWorldClockLabel();
    }

    void Start()
    {
        CloseAllMenus();
        CacheSurvivalUI();
        CacheSceneSystems();
        EnsureDropButton();
        EnsureVitalsBars();

        if (crosshair == null)
        {
            Transform found = transform.Find("Crosshair");
            if (found != null) crosshair = found.gameObject;
        }

        if (tutorialPanel != null)
        {
            if (mobileHUD != null) mobileHUD.SetActive(false); 
            tutorialPanel.SetActive(true);
            Time.timeScale = 0f; 
        }
        else
        {
            if (mobileHUD != null) mobileHUD.SetActive(true);
        }
        
        if (interactButton != null) interactButton.SetActive(true); 
        if (dropButton != null) dropButton.SetActive(false);

        lastActivityTime = Time.unscaledTime;
        lastMousePosition = Input.mousePosition;
    }

    void Update()
    {
        // Keep the phone's vitals in sync while it is on screen.
        if (playerPhonePanel != null && playerPhonePanel.activeInHierarchy)
        {
            RefreshFromVitals();
            UpdateWorldClock();
        }

        TrackIdleHud();
    }

    // ------------------------------------------------------------------
    // IDLE HUD
    // ------------------------------------------------------------------

    /// <summary>
    /// Hides the mobile HUD, crosshair, weapon HUD and phone after a few
    /// minutes without input. Any touch, key, mouse movement or joystick push
    /// brings the HUD straight back.
    /// </summary>
    private void TrackIdleHud()
    {
        if (!autoHideHud) return;
        if (tutorialPanel != null && tutorialPanel.activeInHierarchy) return;

        bool active = PlayerIsActive();

        if (active)
        {
            lastActivityTime = Time.unscaledTime;
            if (hudHiddenByIdle) RestoreHudFromIdle();
            return;
        }

        if (!hudHiddenByIdle &&
            Time.unscaledTime - lastActivityTime >= Mathf.Max(5f, idleHideSeconds))
        {
            HideHudForIdle();
        }
    }

    private bool PlayerIsActive()
    {
        if (Input.touchCount > 0) return true;
        if (Input.anyKey || Input.GetMouseButton(0) ||
            Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            lastMousePosition = Input.mousePosition;
            return true;
        }

        Vector3 mouse = Input.mousePosition;
        if ((mouse - lastMousePosition).sqrMagnitude > 1f)
        {
            lastMousePosition = mouse;
            return true;
        }

        MobileInputManager input = Object.FindFirstObjectByType<MobileInputManager>();
        if (input != null)
        {
            if (input.MoveInput.sqrMagnitude > 0.01f) return true;
            if (input.LookInput.sqrMagnitude > 0.01f) return true;
            if (input.InteractPressed || input.InteractHeld ||
                input.JumpPressed || input.CrouchToggled)
                return true;
        }

        return false;
    }

    private void HideHudForIdle()
    {
        hudHiddenByIdle = true;

        if (mobileHUD != null) mobileHUD.SetActive(false);
        if (crosshair != null) crosshair.SetActive(false);
        if (playerPhonePanel != null && playerPhonePanel.activeSelf)
            playerPhonePanel.SetActive(false);

        if (WeaponHUD.instance != null)
            WeaponHUD.instance.gameObject.SetActive(false);
    }

    private void RestoreHudFromIdle()
    {
        hudHiddenByIdle = false;

        if (mobileHUD != null) mobileHUD.SetActive(true);
        if (crosshair != null) crosshair.SetActive(true);

        if (WeaponHUD.instance != null)
            WeaponHUD.instance.gameObject.SetActive(true);
    }

    // ------------------------------------------------------------------
    // SCENE SYSTEM REFERENCES
    // ------------------------------------------------------------------

    private void CacheSceneSystems()
    {
        if (playerVitals == null) playerVitals = PlayerVitals.instance;
        if (playerVitals == null) playerVitals = Object.FindFirstObjectByType<PlayerVitals>();

        if (phoneSystem == null)
            phoneSystem = Object.FindFirstObjectByType<MobilePhoneSystem>(FindObjectsInactive.Include);

        if (solarSystem == null)
            solarSystem = FindChargingSolarSystem();

        if (playerPhonePanel != null)
        {
            if (solarValueLabel == null)
            {
                Transform solar = playerPhonePanel.transform.Find("SolarLabel");
                if (solar != null) solarValueLabel = solar.GetComponent<TMP_Text>();
            }

            if (phoneBatteryValueLabel == null)
            {
                Transform battery = playerPhonePanel.transform.Find("PhoneBatteryValueText");
                if (battery != null) phoneBatteryValueLabel = battery.GetComponent<TMP_Text>();
            }
        }
    }

    private static SolarSystem FindChargingSolarSystem()
    {
        SolarSystem[] systems = Object.FindObjectsByType<SolarSystem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        SolarSystem fallback = null;
        foreach (SolarSystem system in systems)
        {
            if (system == null) continue;
            fallback = system;

            if (system.timeSystem != null) return system;
            if (system.gameObject.name.IndexOf("Inverter", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return system;
        }

        return fallback;
    }

    private void EnsureDropButton()
    {
        if (dropButton == null && mobileHUD != null)
        {
            Transform[] children = mobileHUD.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != null && child.name == "DropButton")
                {
                    dropButton = child.gameObject;
                    break;
                }
            }
        }

        if (dropButton == null) return;

        Button button = dropButton.GetComponent<Button>();
        if (button == null) button = dropButton.GetComponentInChildren<Button>(true);
        if (button == null) return;

        button.onClick.RemoveListener(OnDropButtonClicked);
        button.onClick.AddListener(OnDropButtonClicked);
    }

    private void OnDropButtonClicked()
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null)
            inventory = Object.FindFirstObjectByType<PlayerInventory>();

        if (inventory != null) inventory.DropSelectedItem();
    }

    // ------------------------------------------------------------------
    // WORLD CLOCK
    // ------------------------------------------------------------------

    /// <summary>
    /// Mirrors the SolarSystem clock: 12-hour time with a size-tagged AM/PM.
    /// </summary>
    public void UpdateWorldClock()
    {
        if (worldClockLabel == null) return;

        DayNightCycle clock = DayNightCycle.instance;
        if (clock == null) clock = Object.FindFirstObjectByType<DayNightCycle>();
        if (clock == null) return;

        float hours24 = clock.timeOfDay * 24f;
        int hours = Mathf.FloorToInt(hours24);
        int minutes = Mathf.FloorToInt((hours24 - hours) * 60f);
        string suffix = hours >= 12 ? "PM" : "AM";
        int hours12 = hours % 12;
        if (hours12 == 0) hours12 = 12;

        worldClockLabel.text = $"{hours12:00}:{minutes:00} <size=60%>{suffix}</size>";
    }

    private void EnsureWorldClockLabel()
    {
        if (worldClockLabel != null) return;

        if (playerPhonePanel == null)
        {
            GameObject panel = GameObject.Find("PhonePanel");
            if (panel != null) playerPhonePanel = panel;
        }

        if (playerPhonePanel == null &&
            gameObject.name.IndexOf("Phone", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            playerPhonePanel = gameObject;
        }

        if (playerPhonePanel == null) return;

        TMP_Text existing = playerPhonePanel.transform.Find("WorldClock") != null
            ? playerPhonePanel.transform.Find("WorldClock").GetComponent<TMP_Text>()
            : null;

        if (existing != null)
        {
            worldClockLabel = existing;
            return;
        }

        worldClockLabel = RuntimeUI.CreateText(playerPhonePanel.transform, "WorldClock",
            "12:00 <size=60%>AM</size>", 52f, new Color(1f, 0.95f, 0.82f));

        RectTransform rt = worldClockLabel.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(440f, 80f);
        rt.anchoredPosition = new Vector2(0f, -16f);

        UpdateWorldClock();
    }

    // ------------------------------------------------------------------
    // VITALS
    // ------------------------------------------------------------------

    public void UpdateVitals(float health, float hunger, float solar)
    {
        if (healthSlider != null) healthSlider.value = health;
        if (hungerSlider != null) hungerSlider.value = hunger;
        if (solarPowerSlider != null) solarPowerSlider.value = solar;
    }

    public void RefreshFromVitals()
    {
        if (playerVitals == null) playerVitals = PlayerVitals.instance;
        if (playerVitals == null) return;

        SetSlider(healthSlider, playerVitals.currentHealth, playerVitals.maxHealth);
        SetSlider(hungerSlider, playerVitals.hunger, 100f);
        SetSlider(thirstSlider, playerVitals.thirst, 100f);
        SetSlider(hygieneSlider, playerVitals.hygiene, 100f);
        SetSlider(sanitySlider, playerVitals.Sanity, 100f);

        UpdateBarValue(healthValueLabel, playerVitals.currentHealth, playerVitals.maxHealth);
        UpdateBarValue(hungerValueLabel, playerVitals.hunger, 100f);
        UpdateBarValue(thirstValueLabel, playerVitals.thirst, 100f);
        UpdateBarValue(hygieneValueLabel, playerVitals.hygiene, 100f);
        UpdateBarValue(sanityValueLabel, playerVitals.Sanity, 100f);

        if (solarSystem == null) solarSystem = FindChargingSolarSystem();
        if (solarSystem != null)
        {
            SetSlider(solarPowerSlider, solarSystem.CurrentCharge, solarSystem.MaxCharge);
            UpdateValueLabel(solarValueLabel, solarSystem.CurrentCharge, solarSystem.MaxCharge);
        }

        if (phoneSystem == null)
            phoneSystem = Object.FindFirstObjectByType<MobilePhoneSystem>(FindObjectsInactive.Include);
        if (phoneSystem != null)
        {
            SetSlider(phoneBatterySlider, phoneSystem.CurrentBattery, phoneSystem.MaxBattery);
            UpdateValueLabel(phoneBatteryValueLabel,
                phoneSystem.CurrentBattery, phoneSystem.MaxBattery);
        }

        if (waterTankSlider != null)
        {
            if (waterTank == null) waterTank = Object.FindFirstObjectByType<WaterTankSystem>();

            if (waterTank != null)
            {
                waterTankSlider.minValue = 0f;
                waterTankSlider.maxValue = Mathf.Max(1f, waterTank.MaxWater);
                waterTankSlider.value = Mathf.Clamp(waterTank.CurrentWater, 0f, waterTankSlider.maxValue);
            }
        }
    }

    private static void SetSlider(Slider slider, float value, float maxValue)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = Mathf.Max(1f, maxValue);
        slider.value = Mathf.Clamp(value, 0f, slider.maxValue);
    }

    private static void UpdateValueLabel(TMP_Text label, float value, float maxValue)
    {
        if (label == null) return;

        float safeMax = Mathf.Max(1f, maxValue);
        int percent = Mathf.RoundToInt(Mathf.Clamp01(value / safeMax) * 100f);
        label.text = percent + "%";
    }

    private void EnsureVitalsBars()
    {
        if (playerPhonePanel == null)
        {
            GameObject panel = GameObject.Find("PhonePanel");
            if (panel != null) playerPhonePanel = panel;
        }

        if (playerPhonePanel == null) return;

        CacheSurvivalUI();

        if (healthSlider == null) healthSlider = FindBarSlider("Health Slider");
        if (hungerSlider == null) hungerSlider = FindBarSlider("Hunger Slider");
        if (thirstSlider == null) thirstSlider = FindBarSlider("Thirst Slider");
        if (hygieneSlider == null) hygieneSlider = FindBarSlider("Hygiene Slider");
        if (sanitySlider == null) sanitySlider = FindBarSlider("Stress Slider");

        StyleVitalsBar(healthSlider, healthBarColor, "HEALTH", ref healthValueLabel);
        StyleVitalsBar(hungerSlider, hungerBarColor, "HUNGER", ref hungerValueLabel);
        StyleVitalsBar(thirstSlider, thirstBarColor, "THIRST", ref thirstValueLabel);
        StyleVitalsBar(hygieneSlider, hygieneBarColor, "HYGIENE", ref hygieneValueLabel);
        StyleVitalsBar(sanitySlider, sanityBarColor, "SANITY", ref sanityValueLabel);

        EnsureVitalsHeader();
    }

    private Slider FindBarSlider(string childName)
    {
        if (playerPhonePanel == null) return null;

        Transform child = playerPhonePanel.transform.Find(childName);
        return child != null ? child.GetComponent<Slider>() : null;
    }

    private void StyleVitalsBar(Slider slider, Color fillColor, string labelText,
        ref TMP_Text valueLabel)
    {
        if (slider == null) return;

        // The bars are read-only: no dragging, no handle knob.
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        if (slider.handleRect != null)
            slider.handleRect.gameObject.SetActive(false);

        Transform background = slider.transform.Find("Background");
        if (background != null)
        {
            Image track = background.GetComponent<Image>();
            if (track != null)
            {
                track.color = vitalsTrackColor;
                track.raycastTarget = false;
            }
        }

        if (slider.fillRect != null)
        {
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = fillColor;
                fill.raycastTarget = false;
            }
        }

        RectTransform barRect = slider.GetComponent<RectTransform>();
        if (barRect == null) return;

        Vector2 barPosition = barRect.anchoredPosition;
        float halfWidth = barRect.sizeDelta.x * 0.5f;

        TMP_Text nameLabel = EnsurePhoneLabel("Vitals" + labelText + "Name", labelText, 16f);
        RectTransform nameRect = nameLabel.rectTransform;
        nameRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRect.pivot = new Vector2(0f, 0f);
        nameRect.anchoredPosition = new Vector2(barPosition.x - halfWidth, barPosition.y + 11f);
        nameRect.sizeDelta = new Vector2(barRect.sizeDelta.x, 17f);
        nameLabel.alignment = TextAlignmentOptions.BottomLeft;
        nameLabel.color = new Color(1f, 1f, 1f, 0.72f);

        valueLabel = EnsurePhoneLabel("Vitals" + labelText + "Value", "100%", 18f);
        RectTransform valueRect = valueLabel.rectTransform;
        valueRect.anchorMin = new Vector2(0.5f, 0.5f);
        valueRect.anchorMax = new Vector2(0.5f, 0.5f);
        valueRect.pivot = new Vector2(0f, 0.5f);
        valueRect.anchoredPosition = new Vector2(barPosition.x + halfWidth + 10f, barPosition.y);
        valueRect.sizeDelta = new Vector2(64f, 24f);
        valueLabel.alignment = TextAlignmentOptions.MidlineLeft;
        valueLabel.color = fillColor;
        valueLabel.fontStyle = FontStyles.Bold;
    }

    private TMP_Text EnsurePhoneLabel(string name, string text, float fontSize)
    {
        if (playerPhonePanel == null) return null;

        Transform existing = playerPhonePanel.transform.Find(name);
        if (existing != null)
        {
            TMP_Text label = existing.GetComponent<TMP_Text>();
            if (label != null) return label;
        }

        return RuntimeUI.CreateText(playerPhonePanel.transform, name, text, fontSize,
            Color.white, TextAlignmentOptions.MidlineLeft);
    }

    private void EnsureVitalsHeader()
    {
        if (playerPhonePanel == null) return;
        if (playerPhonePanel.transform.Find("VitalsHeader") != null) return;

        TMP_Text header = RuntimeUI.CreateText(playerPhonePanel.transform, "VitalsHeader",
            "VITALS", 20f, new Color(1f, 1f, 1f, 0.55f),
            TextAlignmentOptions.MidlineLeft);

        RectTransform headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0.5f, 0.5f);
        headerRect.anchorMax = new Vector2(0.5f, 0.5f);
        headerRect.pivot = new Vector2(0f, 0.5f);
        headerRect.anchoredPosition = new Vector2(-80f, -30f);
        headerRect.sizeDelta = new Vector2(260f, 24f);
    }

    private static void UpdateBarValue(TMP_Text label, float value, float maxValue)
    {
        if (label == null) return;

        float safeMax = Mathf.Max(1f, maxValue);
        label.text = Mathf.RoundToInt(Mathf.Clamp01(value / safeMax) * 100f) + "%";
    }

    private void CacheSurvivalUI()
    {
        if (playerVitals == null) playerVitals = PlayerVitals.instance;
        if (playerVitals == null) playerVitals = Object.FindFirstObjectByType<PlayerVitals>();
        if (waterTank == null) waterTank = Object.FindFirstObjectByType<WaterTankSystem>();

        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            string n = slider.gameObject.name.ToLowerInvariant().Replace(" ", string.Empty);

            if (healthSlider == null && n.Contains("health")) healthSlider = slider;
            if (hungerSlider == null && n.Contains("hunger")) hungerSlider = slider;
            if (thirstSlider == null && n.Contains("thirst")) thirstSlider = slider;
            if (hygieneSlider == null && n.Contains("hygiene")) hygieneSlider = slider;
            if (sanitySlider == null && n.Contains("stress")) sanitySlider = slider;
            if (sanitySlider == null && n.Contains("sanity")) sanitySlider = slider;
            if (solarPowerSlider == null && n.Contains("solar")) solarPowerSlider = slider;
            if (phoneBatterySlider == null && n.Contains("phonebattery")) phoneBatterySlider = slider;
            if (waterTankSlider == null && n.Contains("watertank")) waterTankSlider = slider;
        }

        PrepareSlider(healthSlider);
        PrepareSlider(hungerSlider);
        PrepareSlider(thirstSlider);
        PrepareSlider(hygieneSlider);
        PrepareSlider(sanitySlider);
        PrepareSlider(solarPowerSlider);
        PrepareSlider(phoneBatterySlider);

        RenameStressLabels();
    }

    private static void PrepareSlider(Slider slider)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 100f;
    }

    private void RenameStressLabels()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null || string.IsNullOrEmpty(text.text)) continue;

            string trimmed = text.text.Trim().TrimEnd(':').Trim();
            if (string.Equals(trimmed, "Stress", System.StringComparison.OrdinalIgnoreCase))
                text.text = "Sanity";
        }
    }

    // ------------------------------------------------------------------
    // PANELS
    // ------------------------------------------------------------------

    public void ShowInteractButton(bool show)
    {
        if (interactButton != null) interactButton.SetActive(show);
    }

    public void ToggleBackpack()
    {
        // Backpack panel is intentionally disabled in this build.
    }

    public void TogglePlayerPhone()
    {
        // PhoneButton is wired both in the scene and by MobileInputManager.
        // Ignore the duplicate callback so one tap only toggles once.
        if (lastToggleFrame == Time.frameCount) return;
        lastToggleFrame = Time.frameCount;

        lastActivityTime = Time.unscaledTime;
        hudHiddenByIdle = false;

        bool isOpening = (playerPhonePanel != null && !playerPhonePanel.activeSelf);
        CloseAllMenus(); 
        
        if (isOpening) 
        {
            if (playerPhonePanel != null) playerPhonePanel.SetActive(true);
            if (mobileHUD != null) mobileHUD.SetActive(false);

            EnsureVitalsBars();
            RefreshFromVitals();
        }
        else
        {
            if (mobileHUD != null) mobileHUD.SetActive(true);
        }
    }

    public void CloseAllMenus()
    {
        if (backpackPanel != null) backpackPanel.SetActive(false);
        if (prepStationUI != null) prepStationUI.SetActive(false);
        if (playerPhonePanel != null) playerPhonePanel.SetActive(false);
    }

    public void CloseTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (mobileHUD != null) mobileHUD.SetActive(true);
        if (interactButton != null) interactButton.SetActive(true); 
        Time.timeScale = 1f; 
    }
}
