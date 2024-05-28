using CarInsuranceBot.Models;

namespace CarInsuranceBot.Intefaces
{
    public interface ICustomerService
    {
        public Task InitializeCustomerIfItNotExistAsync(long telegramId);
        public Task<Stage> GetCustomerCurrentStageAsync(long telegramId);
        public Task<Customer> GetCustomerByIdAsync(long telegramId);
        public Task UpdateCustomer(Customer customer);
        public Task SetCustomerStageAsync(long telegramId, Stage stage);
        public Task DeleteCustomerAsync(long telegramId);
    }
}
