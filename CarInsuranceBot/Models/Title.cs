using CarInsuranceBot.Models.Mindee;

namespace CarInsuranceBot.Models
{
    /// <summary>
    /// Represents US Title required data model.
    /// </summary>
    public class Title
    {
        public FieldValue<string>? VechileIdentificationNumber { get; set; }
        public FieldValue<int>? YearModel { get; set; }
        public FieldValue<string>? Make { get; set; }
        public FieldValue<string>? BodyStyle { get; set; }
        public FieldValue<string>? TitleNumber { get; set; }
        public FieldValue<DateOnly>? TitleIssueDate { get; set; }
        public FieldValue<string>? PreviousTitleNumber { get; set; }
        public override string ToString()
        {
            return $"""
                Your Car Title
                ----------------------------
                Vechile Identification Number: {VechileIdentificationNumber?.Value}
                YearModel: {YearModel?.Value}
                Make: {Make?.Value}
                BodyStyle: {BodyStyle?.Value}
                TitleNumber: {TitleNumber?.Value}
                TitleIssueDate: {TitleIssueDate?.Value}
                PreviousTitleNumber: {PreviousTitleNumber?.Value}
                ----------------------------
                """;
        }
    }
}