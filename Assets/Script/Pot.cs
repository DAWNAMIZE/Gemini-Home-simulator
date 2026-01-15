using UnityEngine;

public class Pot : MonoBehaviour
{
    public enum CookState { Raw, Cooking, Cooked, Burnt }
    public CookState currentState = CookState.Raw;

    [Range(0f, 1f)] public float cookProgress = 0f;
    public Renderer foodRenderer;
    public Color rawColor = Color.red;
    public Color cookedColor = Color.brown;
    public Color burntColor = Color.black;

    public void UpdateCookProgress(float progress)
    {
        cookProgress = Mathf.Clamp01(progress);
        currentState = CookState.Cooking;

        if (foodRenderer)
            foodRenderer.material.color = Color.Lerp(rawColor, cookedColor, cookProgress);
    }

    public void SetCooked()
    {
        currentState = CookState.Cooked;
        if (foodRenderer)
            foodRenderer.material.color = cookedColor;
    }

    public void BurnFood()
    {
        currentState = CookState.Burnt;
        if (foodRenderer)
            foodRenderer.material.color = burntColor;
    }
}
