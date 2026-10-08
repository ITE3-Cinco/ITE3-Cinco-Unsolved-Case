using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveActiveScene
{
    public static void Execute()
    {
        EditorSceneManager.SaveOpenScenes();
        UnityEngine.Debug.Log("[SaveActiveScene] All open scenes saved.");
    }
}
