using System.Text.Json.Serialization;

namespace CarInsuranceBot.Models.Mindee
{
    /// <summary>
    /// Represents fields from Mindee's data model
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FieldValue<T>
    {
        [JsonPropertyName("value")]
        public T? Value { get; set; }
    }
}
