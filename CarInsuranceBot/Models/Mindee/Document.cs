using System.Text.Json.Serialization;

namespace CarInsuranceBot.Models.Mindee
{
    public class Document<T>
    {
        [JsonPropertyName("inference")]
        public Inference<T>? Inference { get; set; }
    }
}
