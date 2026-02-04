

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tolik.RemakeSoF.Runtime
{

    /// <summary>
    /// Custom JsonConverter that handles both single objects and arrays.
    /// Converts both cases to a List<T>.
    /// </summary>
    public class SingleOrArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<T>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Array)
            {
                // Es ist ein Array -> normal deserialisieren
                return token.ToObject<List<T>>();
            }
            else if (token.Type == JTokenType.Object)
            {
                // Es ist ein einzelnes Objekt -> als Liste mit einem Element zurückgeben
                var item = token.ToObject<T>();
                return new List<T> { item };
            }
            else if (token.Type == JTokenType.Null)
            {
                // Null -> leere Liste
                return new List<T>();
            }

            // Fallback: leere Liste
            return new List<T>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}