using System;
using Newtonsoft.Json;

namespace Script.CommonLib
{
    public static class JsonSerialize
    {
        private static readonly JsonSerializerSettings settings = new()
        {
            Converters = { new Vector3Converter() },
        };
        
        public static string SerializeToJson(this object obj)
        {
            var json = JsonConvert.SerializeObject(obj, settings);
            return json;
        }
        
        public static T DeserializeObject<T>(string str)
        {
            var obj = JsonConvert.DeserializeObject<T>(str, settings);
            return obj;
        }
    }
}
