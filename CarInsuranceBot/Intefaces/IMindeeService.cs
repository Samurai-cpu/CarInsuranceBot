using CarInsuranceBot.Models;

namespace CarInsuranceBot.Intefaces
{
    public interface IMindeeService
    {
        public Task<Title> ParseTitleImageAsync(string base64string);
        public Task<Passport> ParsePassportImageAsync(string base64string);

    }
}
