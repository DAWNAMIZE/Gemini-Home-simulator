using UnityEngine;
using UnityEngine.SceneManagement; // CRITICAL: This allows us to load scenes

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Type the exact name of your main game scene here.")]
    public string mainGameSceneName = "Home"; // Change "Home" to whatever your scene is named!

    // This function will be linked to the Start Button
    public void StartGame()
    {
        Debug.Log("Loading Main Game...");
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
}