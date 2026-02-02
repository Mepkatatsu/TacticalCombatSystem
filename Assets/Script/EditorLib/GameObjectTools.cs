using Script.CommonLib;
using UnityEditor;
using UnityEngine;

namespace Script.EditorLib
{
    public static class GameObjectTools
    {
        [MenuItem("Tools/Remove Missing Scripts From Selections")]
        public static void RemoveMissingScripts()
        {
            var selectedObjects = Selection.gameObjects;
            
            foreach (GameObject go in selectedObjects)
            {
                RemoveMissingScriptsRecursive(go);
            }
        }
        
        private static void RemoveMissingScriptsRecursive(GameObject go)
        {
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removedCount > 0)
            {
                LogHelper.Log($"{removedCount} missing scripts removed from {go.name}");
            }
            
            foreach (Transform child in go.transform)
            {
                RemoveMissingScriptsRecursive(child.gameObject);
            }
        }

    }
}
