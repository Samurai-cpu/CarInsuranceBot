namespace CarInsuranceBot.Models.Mindee
{
    /// <summary>
    /// Represnts response model from GET endpoint of Mindee API
    /// </summary>
    /// <remarks>
    /// Not all fields from the response are included in this model, only those necessary for the application
    /// </remarks>
    public class GetResponse<T>
    {
        public ApiRequestStatus? RequestStatus { get; set; }
        public Document<T>? Document { get; set; }
    }
}
