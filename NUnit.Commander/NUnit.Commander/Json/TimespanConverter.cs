using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NUnit.Commander.Json
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public TimeSpanConverter() { }

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var timespanStr = reader.GetString();
            TimeSpan.TryParse(timespanStr, out var timeSpan);
            return timeSpan;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
