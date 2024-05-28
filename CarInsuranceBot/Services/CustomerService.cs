using CarInsuranceBot.Context;
using CarInsuranceBot.Intefaces;
using CarInsuranceBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarInsuranceBot.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IServiceProvider _serviceProvider;
        public CustomerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Checks wheter user exist in DB, if no added new user to DB.
        /// </summary>
        /// <param name="telegramId"></param>
        /// <returns></returns>
        public async Task InitializeCustomerIfItNotExistAsync(long telegramId)
        {
            using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (!await db.Customers.AnyAsync(x => x.TelegramId == telegramId))
                {
                    db.Customers.Add(new Customer { TelegramId = telegramId, FlowCurrentStage = Stage.Greeting });
                    await db.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Gets customer's current stage in bot's flow.
        /// </summary>
        /// <param name="telegramId">Telegram id</param>
        /// <returns></returns>
        public async Task<Stage> GetCustomerCurrentStageAsync(long telegramId)
        {
            using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Customers.Where(x => x.TelegramId == telegramId)
                    .Select(x => x.FlowCurrentStage).FirstAsync();
            }
        }

        /// <summary>
        /// Gets customer data by telegram id.
        /// </summary>
        /// <param name="telegramId">Telegram id</param>
        /// <returns></returns>
        public async Task<Customer> GetCustomerByIdAsync(long telegramId)
        {
            using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                 var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();   
                 return await db.Customers.FirstAsync(x => x.TelegramId == telegramId);        
            }
        }

        /// <summary>
        /// Updates customers personal data.
        /// </summary>
        /// <param name="customer">Updated customer data</param>
        /// <returns></returns>
        public async Task UpdateCustomer(Customer customer)
        {
            using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var db = scope.ServiceProvider.GetRequiredService<AppDbContext>())
                {
                    db.Customers.Update(customer);
                    await db.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Updates usrs's current stage in bot's flow
        /// </summary>
        /// <param name="telegramId">Customer's telegram Id</param>
        /// <param name="stage"></param>
        /// <returns></returns>
        public async Task SetCustomerStageAsync(long telegramId, Stage stage)
        {
            using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Customers
                    .Where(x => x.TelegramId == telegramId)
                    .ExecuteUpdateAsync(x => x.SetProperty(pr => pr.FlowCurrentStage, stage));
            }
        }

        public async Task DeleteCustomerAsync(long telegramId)
        {
            using (var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Customers
                    .Where(x => x.TelegramId == telegramId)
                    .ExecuteDeleteAsync();
            }
        }
    }
}