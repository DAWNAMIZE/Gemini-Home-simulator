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

    [Header("Mobile Action Buttons")]
    public GameObject interactButton;  
    public GameObject dropButton;      

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        Screen.orientation = ScreenOrientation.LandscapeLeft;

        CacheSurvivalUI();
    }

    void Start()
    {
        CloseAllMenus();
        
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
    }

    void Update()
    {
        // Keep the phone's vitals in sync while it is on screen.
        if (playerPhonePanel != null && playerPhonePanel.activeInHierarchy)
            RefreshFromVitals();
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

        if (waterTankSlider != null)
        {
            if (waterTank == null) waterTank = Object.FindAnyObjectByType<WaterTankSystem>();

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

    private void CacheSurvivalUI()
    {
        if (playerVitals == null) playerVitals = PlayerVitals.instance;
        if (playerVitals == null) playerVitals = Object.FindAnyObjectByType<PlayerVitals>();
        if (waterTank == null) waterTank = Object.FindAnyObjectByType<WaterTankSystem>();

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
            if (waterTankSlider == null && n.Contains("watertank")) waterTankSlider = slider;
        }

        PrepareSlider(healthSlider);
        PrepareSlider(hungerSlider);
        PrepareSlider(thirstSlider);
        PrepareSlider(hygieneSlider);
        PrepareSlider(sanitySlider);

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
