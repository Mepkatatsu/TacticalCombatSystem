using System;
using System.Linq;
using Script.ClientLib;
using UnityEditor;
using UnityEngine;

namespace Script.EditorLib.Tools
{
    public class ScriptableObjectWindow : EditorWindow
    {
        private Type[] _types;
    
        [MenuItem("Tools/Find ScriptableObject &S")]
        public static void FindScriptableObject()
        {
            GetWindow<ScriptableObjectWindow>().Show();
        }

        private void OnEnable()
        {
            _types = TypeCache.GetTypesDerivedFrom<ScriptableObjectBase>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .OrderBy(t => t.Name)
                .ToArray();
        }
    
        private void OnGUI()
        {
            if (_types == null)
                return;

            foreach (var t in _types)
            {
                if (GUILayout.Button(t.Name))
                {
                    SelectScriptableObject(t);
                }
            }
        }

        private void SelectScriptableObject(Type type)
        {
            var asset = ScriptableObjectBase.GetScriptableObject(type);
        
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Close();
        }
    }
}
