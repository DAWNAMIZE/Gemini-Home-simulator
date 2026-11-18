using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the runtime-generated cooking, market and backpack UI directly into
/// the active scene so their RectTransforms can be arranged in edit mode.
/// </summary>
public static class EditableUILayoutBuilder
{
    private const string RootName = "NewUI_Editable";

    [MenuItem("Tools/Gemini Home/UI/Build Editable UI In Scene")]
    public static void BuildEditableUI()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Editable UI",
                "Exit play mode before building the editable UI hierarchy.", "OK");
            return;
        }

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("Editable UI",
                "A " + RootName + " hierarchy already exists. Rebuilding replaces it " +
                "and discards any manual arrangement changes. Continue?",
                "Rebuild", "Cancel");
            if (!replace) return;

            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Editable UI");

        CreateScreen<IngredientBagUI>(root.transform, "IngredientBagUI");
        CreateScreen<PotCookUI>(root.transform, "PotCookUI");
        CreateScreen<MarketUI>(root.transform, "MarketUI");

        Undo.RegisterFullObjectHierarchyUndo(root, "Build Editable UI");
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;

        Debug.Log("Editable UI built under '" + RootName + "'. " +
                  "Panels are visible in edit mode and close automatically in play mode.");
    }

    [MenuItem("Tools/Gemini Home/UI/Remove Editable UI")]
    public static void RemoveEditableUI()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Editable UI",
                "Exit play mode before removing the editable UI hierarchy.", "OK");
            return;
        }

        GameObject existing = GameObject.Find(RootName);
        if (existing == null)
        {
            EditorUtility.DisplayDialog("Editable UI",
                "No " + RootName + " hierarchy was found in the active scene.", "OK");
            return;
        }

        bool remove = EditorUtility.DisplayDialog("Editable UI",
            "Remove the entire " + RootName + " hierarchy from the scene?", "Remove", "Cancel");
        if (!remove) return;

        Undo.DestroyObjectImmediate(existing);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Editable UI removed from the active scene.");
    }

    private static void CreateScreen<T>(Transform parent, string objectName)
        where T : Component
    {
        GameObject screen = new GameObject(objectName);
        screen.transform.SetParent(parent, false);

        T component = screen.AddComponent<T>();

        IngredientBagUI bag = component as IngredientBagUI;
        if (bag != null)
        {
            bag.BuildForEditor();
            return;
        }

        PotCookUI pot = component as PotCookUI;
        if (pot != null)
        {
            pot.BuildForEditor();
            return;
        }

        MarketUI market = component as MarketUI;
        if (market != null)
        {
            market.BuildForEditor();
        }
    }
}
