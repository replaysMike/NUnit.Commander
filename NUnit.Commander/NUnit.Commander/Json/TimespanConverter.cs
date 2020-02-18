using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NUnit.Commander.Json
{
    public class TimespanConverter : JsonConverter<TimeSpan>
    {
        public TimespanConverter() { }

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
