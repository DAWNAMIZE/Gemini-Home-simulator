using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Type the exact name of your main game scene here.")]
    public string mainGameSceneName = "Home"; // Change "Home" to whatever your scene is named!

    [Header("Buttons")]
    public Button startButton;
    public Button continueButton;

    void Start()
    {
        EnsureContinueButton();
    }

    // This function will be linked to the Start Button
    public void StartGame()
    {
        SaveGameManager.pendingContinue = false;
        Debug.Log("Loading Main Game...");
        SceneManager.LoadScene(mainGameSceneName);
    }

    public void ContinueGame()
    {
        SaveGameManager.pendingContinue = true;
        Debug.Log("Loading saved game...");
        SceneManager.LoadScene(mainGameSceneName);
    }

    // This function will be linked to the Exit Button
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");

        // This closes the game when built
        Application.Quit();

        // Note: Application.Quit() does not work inside the Unity Editor.
        // The Debug.Log above lets you know the button is working while testing!
    }

    /// <summary>
    /// Adds the Continue button under Start only when a save file exists.
    /// </summary>
    private void EnsureContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(SaveGameManager.HasSave);
            return;
        }

        if (startButton == null)
        {
            GameObject start = GameObject.Find("StartButton");
            if (start != null) startButton = start.GetComponent<Button>();
        }

        if (startButton == null)
        {
            Debug.LogWarning("MainMenuController: StartButton was not found; cannot add Continue.");
            return;
        }

        if (!SaveGameManager.HasSave) return;

        Button clone = Instantiate(startButton, startButton.transform.parent, false);
        clone.name = "ContinueButton";
        clone.gameObject.SetActive(true);

        clone.onClick = new Button.ButtonClickedEvent();
        clone.onClick.AddListener(ContinueGame);

        TMP_Text label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "Continue";

        // Sit directly below the Start button, unless a layout group already
        // handles the arrangement.
        if (clone.transform.parent != null &&
            clone.transform.parent.GetComponent<LayoutGroup>() == null)
        {
            RectTransform startRect = startButton.GetComponent<RectTransform>();
            RectTransform continueRect = clone.GetComponent<RectTransform>();

            if (startRect != null && continueRect != null)
            {
                continueRect.anchorMin = startRect.anchorMin;
                continueRect.anchorMax = startRect.anchorMax;
                continueRect.pivot = startRect.pivot;
                continueRect.sizeDelta = startRect.sizeDelta;
                continueRect.anchoredPosition = startRect.anchoredPosition +
                    new Vector2(0f, -(startRect.sizeDelta.y + 24f));
            }
        }

        clone.transform.SetSiblingIndex(startButton.transform.GetSiblingIndex() + 1);
        continueButton = clone;
    }
}
