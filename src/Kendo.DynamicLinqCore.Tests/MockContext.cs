using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Kendo.DynamicLinqCore.Tests.Models;


namespace Kendo.DynamicLinqCore.Tests
{
    public class MockContext : DbContext
    {
        public DbSet<Company> Company { get; set; }
        public DbSet<Employee> Employee { get; set; }

        public static MockContext GetDefaultInMemoryDbContext()
        {
            var serviceProvider = new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();
            var builder = new DbContextOptionsBuilder<MockContext>();
            var options = builder.UseInMemoryDatabase("Kendo").UseInternalServiceProvider(serviceProvider).Options;
            
            var dbContext = new MockContext(options);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            return dbContext;
        }

        public MockContext(DbContextOptions<MockContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>().HasData(
                new Employee { 
                    Number = 1,
                    Name = "Monie",
                    Identification = Guid.Parse("F057D609-F1F3-4E5C-BC09-0AC0BBE1007D"),
                    Introduce = "I'm Monie",
                    Salary = 1000,
                    Birthday = new DateTime(2000,5,5) 
                },
                new Employee { 
                    Number = 2,
                    Identification = Guid.Parse("F586A608-4095-4E8E-8F21-AEFC0DFDB61F"),
                    Name = "CoCo",
                    Introduce = "I'm CoCo",
                    Salary = 2500,
                    Birthday = new DateTime(1986,10,10) 
                }
            );
        }
    }
}