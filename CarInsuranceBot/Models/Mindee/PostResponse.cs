namespace CarInsuranceBot.Models.Mindee
{
    /// <summary>
    /// Represnts response model from POST endpoint of Mindee API
    /// </summary>
    /// <remarks>
    /// Not all fields from the response are included in this model, only those necessary for the application
    /// </remarks>
    public class PostResponse
    {
        public ApiRequestStatus? RequestStatus { get; set; }
        public Job Job { get; set; } = default!;
    }
}