using System;
using Newtonsoft.Json;

namespace Script.CommonLib
{
    public static class JsonSerialize
    {
        private static readonly JsonSerializerSettings Settings = new();
        
        public static string SerializeToJson(this object obj)
        {
            var json = JsonConvert.SerializeObject(obj, Settings);
            return json;
        }
        
        public static T DeserializeObject<T>(string str)
        {
            var obj = JsonConvert.DeserializeObject<T>(str, Settings);
            return obj;
        }
    }
}
