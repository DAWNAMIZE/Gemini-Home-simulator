using UnityEngine;

public class PotState : MonoBehaviour
{
    [Header("Pot Status")]
    public bool isLoaded = false;
    
    [Header("Recipe Output")]
    // This tells the stove exactly which dish to spawn when the timer finishes.
    // It MUST be public so the Stove can read it!
    public GameObject finishedDishPrefab; 

    public void LoadPot(GameObject dishToCook)
    {
        isLoaded = true;
        finishedDishPrefab = dishToCook;
        Debug.Log("Pot is now loaded with ingredients!");
    }

    public void EmptyPot()
    {
        isLoaded = false;
        finishedDishPrefab = null;
    }
}