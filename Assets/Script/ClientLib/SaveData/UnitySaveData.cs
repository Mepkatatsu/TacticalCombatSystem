using UnityEngine;

namespace Script.ClientLib.SaveData
{
    public static class UnitySaveData
    {
        public static string GetData(string key, string defVal)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetString(key);
            }
            else
            {
                PlayerPrefs.SetString(key, defVal);
            }
        
            return defVal;
        }
    
        public static void SetData(string key, string val)
        {
            PlayerPrefs.SetString(key, val);
        }
    
        public static float GetData(string key, float defVal)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetFloat(key);
            }
            else
            {
                PlayerPrefs.SetFloat(key, defVal);
            }
        
            return defVal;
        }
    
        public static void SetData(string key, float val)
        {
            PlayerPrefs.SetFloat(key, val);
        }
    
        public static int GetData(string key, int defVal)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetInt(key);
            }
            else
            {
                PlayerPrefs.SetInt(key, defVal);
            }
        
            return defVal;
        }
    
        public static void SetData(string key, int val)
        {
            PlayerPrefs.SetInt(key, val);
        }
        
        public static bool GetData(string key, bool defVal)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetInt(key) == 1 ? true : false;
            }
            else
            {
                PlayerPrefs.SetInt(key, defVal ? 1 : 0);
            }
        
            return defVal;
        }
    
        public static void SetData(string key, bool val)
        {
            PlayerPrefs.SetInt(key, val ? 1 : 0);
        }
    }
}
