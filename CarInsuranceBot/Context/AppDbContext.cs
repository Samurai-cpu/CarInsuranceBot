using CarInsuranceBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsuranceBot.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
        {
        }
    }
}