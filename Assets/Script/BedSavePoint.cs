using UnityEngine;

/// <summary>
/// Proximity save point for Bed_Save. Shows the sleep prompt and writes the
/// save file when the player chooses to sleep.
/// </summary>
public class BedSavePoint : MonoBehaviour
{
    [Header("Bed")]
    public string bedName = "Bed";
    public float interactionRange = 2.8f;

    private bool promptVisible;

    public bool IsInRange(Vector3 position)
    {
        return Vector3.Distance(position, transform.position) <= interactionRange;
    }

    void Update()
    {
        if (PlayerInventory.instance == null)
        {
            if (promptVisible) { promptVisible = false; SleepUI.HideIf(this); }
            return;
        }

        bool inRange = IsInRange(PlayerInventory.instance.transform.position);

        if (inRange && !promptVisible)
        {
            promptVisible = true;
            SleepUI.Show(this);
        }
        else if (!inRange && promptVisible)
        {
            promptVisible = false;
            SleepUI.HideIf(this);
        }
    }

    public void Sleep()
    {
        bool saved = SaveGameManager.Save();

        if (DayNightCycle.instance != null)
            DayNightCycle.instance.timeOfDay = 0.25f; // 06:00

        PlayerVitals vitals = PlayerVitals.instance;
        if (vitals != null)
        {
            vitals.Heal(25f);
            vitals.hunger = Mathf.Clamp(vitals.hunger + 20f, 0f, 100f);
            vitals.thirst = Mathf.Clamp(vitals.thirst + 15f, 0f, 100f);
            vitals.stress = Mathf.Clamp(vitals.stress - 35f, 0f, 100f);
        }

        if (saved) ToastUI.Show("Progress saved. Good morning!");
        else ToastUI.Show("Could not save your progress.");

        promptVisible = false;
    }
}
