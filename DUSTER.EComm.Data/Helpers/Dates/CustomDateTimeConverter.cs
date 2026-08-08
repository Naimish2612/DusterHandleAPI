using System.Globalization;
using System.Text.Json;

namespace DUSTER.EComm.Data.Helpers.Dates
{
    public class CustomDateTimeConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        private static readonly string[] formats = new[]
        {
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd",
            "dd-MM-yyyy"
        };

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();

            if (string.IsNullOrWhiteSpace(value))
                throw new JsonException("Date value is null or empty.");

            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            // fallback (optional but useful)
            if (DateTime.TryParse(value, out date))
            {
                return date;
            }

            throw new JsonException($"Invalid date format: {value}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Standardize output
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
