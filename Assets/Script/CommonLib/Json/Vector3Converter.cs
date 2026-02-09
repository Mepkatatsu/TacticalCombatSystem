using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Script.CommonLib;

public class Vector3Converter : JsonConverter<Vec3>
{
    public override void WriteJson(JsonWriter writer, Vec3 value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x"); writer.WriteValue(value.x);
        writer.WritePropertyName("y"); writer.WriteValue(value.y);
        writer.WritePropertyName("z"); writer.WriteValue(value.z);
        writer.WriteEndObject();
    }

    public override Vec3 ReadJson(JsonReader reader, Type objectType, Vec3 existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        return new Vec3(
            (float)obj["x"],
            (float)obj["y"],
            (float)obj["z"]
        );
    }
}
