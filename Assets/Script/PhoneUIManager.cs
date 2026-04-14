using UnityEngine;
using UnityEngine.UI;

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
    public Slider solarPowerSlider;

    [Header("Mobile Action Buttons")]
    public GameObject interactButton;  
    public GameObject dropButton;      

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        Screen.orientation = ScreenOrientation.LandscapeLeft;
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

    // UPDATE FUNCTION REMOVED: Hotkeys (P, Tab) are now disabled.

    public void UpdateVitals(float health, float hunger, float solar)
    {
        if (healthSlider != null) healthSlider.value = health;
        if (hungerSlider != null) hungerSlider.value = hunger;
        if (solarPowerSlider != null) solarPowerSlider.value = solar;
    }

    public void ShowInteractButton(bool show)
    {
        if (interactButton != null) interactButton.SetActive(true);
    }

    public void ToggleBackpack()
    {
        // bool isOpening = (backpackPanel != null && !backpackPanel.activeSelf);
        // CloseAllMenus(); 
        
        // if (isOpening)
        // {
        //     if (backpackPanel != null) backpackPanel.SetActive(true);
        //     if (mobileHUD != null) mobileHUD.SetActive(false); 
        // }
        // else
        // {
        //     if (mobileHUD != null) mobileHUD.SetActive(true);
        // }

        // Debug.Log("Backpack toggled: " + (isOpening ? "Opened" : "Closed"));
    }

    public void TogglePlayerPhone()
    {
        bool isOpening = (playerPhonePanel != null && !playerPhonePanel.activeSelf);
        CloseAllMenus(); 
        
        if (isOpening) 
        {
            if (playerPhonePanel != null) playerPhonePanel.SetActive(true);
            if (mobileHUD != null) mobileHUD.SetActive(false);
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