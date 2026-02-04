using System;
using Script.CommonLib;
using UnityEngine;

namespace Script.ClientLib
{
    public abstract class ScriptableObjectBase : ScriptableObject
    {
        public static string GetAssetPath(string assetName)
        {
            return $"Assets/Data/ScriptableObject/{assetName}.asset";
        }

        public static ScriptableObjectBase GetScriptableObject(Type type)
        {
#if UNITY_EDITOR
            var path = GetAssetPath(type.Name);
            IOHelper.EnsureDirectory(path);
            
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObjectBase>(path);

            if (!asset)
            {
                asset = CreateInstance(type) as ScriptableObjectBase;
                UnityEditor.AssetDatabase.CreateAsset(asset, path);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }

            return asset;
#endif
            throw new NotImplementedException();
        }
    }

    public abstract class ScriptableObjectBase<T> : ScriptableObjectBase where T : ScriptableObjectBase<T>
    {
        public static string AssetPath => GetAssetPath(typeof(T).Name);
    
        public static T Instance => GetScriptableObject(typeof(T)) as T;
    }
}