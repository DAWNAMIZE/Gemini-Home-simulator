using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // The Singleton: Allows any script to call GameManager.instance safely
    public static GameManager instance;

    [Header("Game States")]
    public bool isGamePaused = false;
    public bool isGameOver = false;

    [Header("UI Connections")]
    // Drag your death screen UI panel here (create a simple red panel with a restart button)
    public GameObject gameOverPanel; 

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

    void Start()
    {
        // Ensure the game starts cleanly
        ResumeGame();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        // We constantly watch the player's health to trigger Game Over
        CheckPlayerVitals();
    }

    private void CheckPlayerVitals()
    {
        if (isGameOver) return; // Don't run this if they are already dead

        // Find the player's vitals in the scene
        PlayerVitals vitals = FindFirstObjectByType<PlayerVitals>();
        
        if (vitals != null && vitals.currentHealth <= 0)
        {
            TriggerGameOver();
        }
    }

    // --- GAME STATE CONTROLS ---

    public void PauseGame()
    {
        if (isGameOver) return;

        isGamePaused = true;
        Time.timeScale = 0f; // This freezes everything (movement, solar power, day/night)
        
        // AudioListener.pause = true; // Optional: Mutes all sound while paused
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (isGameOver) return;

        isGamePaused = false;
        Time.timeScale = 1f; // Resumes time
        
        // AudioListener.pause = false;
        Debug.Log("Game Resumed");
    }

    // --- GAME OVER & SCENE MANAGEMENT ---

    public void TriggerGameOver()
    {
        isGameOver = true;
        PauseGame(); // Freeze the game when they die

        // Turn off the mobile HUD and show the death screen
        if (PhoneUIManager.instance != null)
        {
            PhoneUIManager.instance.mobileHUD.SetActive(false);
            PhoneUIManager.instance.CloseAllMenus();
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        
        Debug.Log("GAME OVER!");
    }

    public void RestartCurrentLevel()
    {
        // Unfreeze time before loading, otherwise the new scene will be frozen too!
        Time.timeScale = 1f; 
        
        // Reloads the exact scene we are currently in
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        
        // Ensure you type the exact name of your Main Menu scene here
        SceneManager.LoadScene("MainMenu"); 
    }
}