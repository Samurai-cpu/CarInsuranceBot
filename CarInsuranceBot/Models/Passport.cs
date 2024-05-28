using CarInsuranceBot.Models.Mindee;
using System.Text.Json.Serialization;

namespace CarInsuranceBot.Models
{
    /// <summary>
    /// Represents US Passport required data model.
    /// </summary>
    public class Passport
    {
        [JsonPropertyName("type")]
        public FieldValue<string>? Type { get; set; }
        [JsonPropertyName("code")]
        public FieldValue<string>? Code { get; set; }
        [JsonPropertyName("passport_no")]
        public FieldValue<string>? PassportNumber { get; set; }
        [JsonPropertyName("surname")]
        public FieldValue<string>? Surname { get; set; }
        [JsonPropertyName("given_name")]
        public FieldValue<string>? GivenName { get; set; }

        public string? FullName { get 
            {
                return $"{Surname?.Value} + {GivenName?.Value}"; 
            } }
        public override string ToString()
        {
            return $"""
                Your Passport
                ----------------------------
                Type: {Type?.Value}
                CountryCode: {Code?.Value}
                Number: {PassportNumber?.Value}
                FullName: {FullName}
                ----------------------------
                """;
        }
    }
}
