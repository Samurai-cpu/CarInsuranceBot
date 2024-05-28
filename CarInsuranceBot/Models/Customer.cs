using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceBot.Models
{
    /// <summary>
    /// Model with required information to work with customers.
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public Stage FlowCurrentStage { get; set; }
        public string? VechileIdentificationNumber { get; set; }
        public string? FullName { get; set; }
        public string? PassTypeAndNumber { get; set; }

        public void UpdateCustomersPassportDataAndStage(Passport passport, Stage stage)
        {
            FullName = passport?.FullName;
            PassTypeAndNumber = $"{passport?.Type?.Value} {passport?.PassportNumber?.Value}";
            FlowCurrentStage = stage;
        }

        public void UpdateCustomersTitleDataAndStage(Title title, Stage stage)
        {
            VechileIdentificationNumber = title.VechileIdentificationNumber?.Value;
            FlowCurrentStage = stage ;
        }
    }
}