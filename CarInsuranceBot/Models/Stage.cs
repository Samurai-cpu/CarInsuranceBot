namespace CarInsuranceBot.Models
{
    /// <summary>
    /// Represents customer's current stage in bot workflow
    /// </summary>
    public enum Stage
    {
        Greeting,
        Passport,
        PassportConfirmation,
        Title,
        TitleConfirmation,
        PriceQuotation,
        Finish
    }
}