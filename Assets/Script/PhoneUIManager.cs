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
        lastActivityTime = Time.unscaledTime;
        hudHiddenByIdle = false;

        bool isOpening = (playerPhonePanel != null && !playerPhonePanel.activeSelf);
        CloseAllMenus(); 
        
        if (isOpening) 
        {
            if (playerPhonePanel != null) playerPhonePanel.SetActive(true);
            if (mobileHUD != null) mobileHUD.SetActive(false);

            CacheSurvivalUI();
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
