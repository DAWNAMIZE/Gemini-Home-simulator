using UnityEngine;
using UnityEngine.UI;

public class PhoneUIManager : MonoBehaviour
{
    [Header("Phone UI Panel")]
    public GameObject phonePanel; // The UI panel that represents the phone screen

    [Header("System References")]
    public SolarSystem solarSystem;
    public WaterTankSystem waterTankSystem;
    public GasSystem gasSystem;
    public GeneratorSystem generatorSystem;
    public MobilePhoneSystem mobilePhoneSystem;

    [Header("Sliders")]
    public Slider solarSlider;
    public Slider waterSlider;
    public Slider gasSlider;
    public Slider generatorFuelSlider;
    public Slider phoneBatterySlider;

    [Header("UI Text (Optional)")]
    public Text solarText;
    public Text waterText;
    public Text gasText;
    public Text generatorText;
    public Text phoneBatteryText;

    private bool isOpen = false;

    void Start()
    {
        if (phonePanel != null)
            phonePanel.SetActive(false);

        // Initialize slider max values
        if (solarSystem != null && solarSlider != null)
            solarSlider.maxValue = solarSystem.MaxCharge;

        if (waterTankSystem != null && waterSlider != null)
            waterSlider.maxValue = waterTankSystem.MaxWater;

        if (gasSystem != null && gasSlider != null)
            gasSlider.maxValue = gasSystem.MaxGas;

        if (generatorSystem != null && generatorFuelSlider != null)
            generatorFuelSlider.maxValue = generatorSystem.MaxFuel;

        if (mobilePhoneSystem != null && phoneBatterySlider != null)
            phoneBatterySlider.maxValue = mobilePhoneSystem.MaxBattery;
    }

    void Update()
    {
        if (!isOpen) return; // Only update if phone is open

        // Update UI values
        if (solarSystem != null && solarSlider != null)
        {
            solarSlider.value = solarSystem.CurrentCharge;
            if (solarText) solarText.text = $"{solarSystem.CurrentCharge:0}/{solarSystem.MaxCharge}";
        }

        if (waterTankSystem != null && waterSlider != null)
        {
            waterSlider.value = waterTankSystem.CurrentWater;
            if (waterText) waterText.text = $"{waterTankSystem.CurrentWater:0}/{waterTankSystem.MaxWater}";
        }

        if (gasSystem != null && gasSlider != null)
        {
            gasSlider.value = gasSystem.CurrentGas;
            if (gasText) gasText.text = $"{gasSystem.CurrentGas:0}/{gasSystem.MaxGas}";
        }

        if (generatorSystem != null && generatorFuelSlider != null)
        {
            generatorFuelSlider.value = generatorSystem.CurrentFuel;
            if (generatorText) generatorText.text = $"{generatorSystem.CurrentFuel:0}/{generatorSystem.MaxFuel}";
        }

        if (mobilePhoneSystem != null && phoneBatterySlider != null)
        {
            phoneBatterySlider.value = mobilePhoneSystem.CurrentBattery;
            if (phoneBatteryText) phoneBatteryText.text = $"{mobilePhoneSystem.CurrentBattery:0}%";
        }
    }

    public void TogglePhone()
    {
        isOpen = !isOpen;

        if (phonePanel != null)
            phonePanel.SetActive(isOpen);
    }

    public void OpenPhone()
    {
        isOpen = true;
        if (phonePanel != null)
            phonePanel.SetActive(true);
    }

    public void ClosePhone()
    {
        isOpen = false;
        if (phonePanel != null)
            phonePanel.SetActive(false);
    }
}
