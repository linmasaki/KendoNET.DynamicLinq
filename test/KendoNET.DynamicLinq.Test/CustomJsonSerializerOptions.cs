

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KendoNET.DynamicLinq.Test
{
    public class CustomJsonSerializerOptions
    {
        public static readonly JsonSerializerOptions DefaultOptions = new() { PropertyNameCaseInsensitive = true };

        static CustomJsonSerializerOptions()
        {
            // System.Text.Json didn't deserialize inferred type to object properties now.
            // https://docs.microsoft.com/en-gb/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to#deserialization-of-object-properties
            // https://docs.microsoft.com/en-gb/dotnet/standard/serialization/system-text-json-converters-how-to#deserialize-inferred-types-to-object-properties
            DefaultOptions.Converters.Add(new ObjectToInferredTypesConverter());
        }
    }

    public class ObjectToInferredTypesConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }

            if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out var l))
                {
                    return l;
                }

                return reader.GetDouble();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                if (reader.TryGetDateTime(out var datetime))
                {
                    return datetime;
                }

                return reader.GetString();
            }

            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.Clone();
        }

        public override void Write(Utf8JsonWriter writer, object objectToWrite, JsonSerializerOptions options) =>
            throw new InvalidOperationException("Should not get here.");
    }
}
