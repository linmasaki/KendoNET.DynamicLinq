using System;
using KendoNET.DynamicLinq.Test.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KendoNET.DynamicLinq.Test.Data
{
    public class MockContext(DbContextOptions<MockContext> options) : DbContext(options)
    {
        private static MockContext _defaultDbContext;

        public DbSet<Company> Company { get; set; }
        public DbSet<Employee> Employee { get; set; }

        public static MockContext GetDefaultInMemoryDbContext()
        {
            if (_defaultDbContext != null) return _defaultDbContext;

            var serviceProvider = new ServiceCollection().AddDbContext<MockContext>(options => options.UseLazyLoadingProxies().UseInMemoryDatabase("Kendo")).BuildServiceProvider();

            _defaultDbContext = serviceProvider.GetRequiredService<MockContext>();
            _defaultDbContext.Database.EnsureDeleted();
            _defaultDbContext.Database.EnsureCreated();
            return _defaultDbContext;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Add employee data
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    Number = 1,
                    Name = "Monie",
                    Identification = Guid.Parse("ff5f9bb3-f805-4f52-a5f9-fbd0493d5b8f"),
                    Introduce = "I'm Monie",
                    Weight = 48.5,
                    Salary = 1000,
                    Gender = Gender.F,
                    Birthday = new DateTime(2000, 5, 5),
                    CompanyId = Guid.Parse("c2cbfe28-f82a-4904-8075-bf98729d434f")
                },
                new Employee
                {
                    Number = 2,
                    Identification = Guid.Parse("a3ee172c-fdf3-4390-9bb1-d5a70ccfbb3b"),
                    Name = "CoCo",
                    Introduce = "I'm CoCo",
                    Salary = 2500,
                    Weight = 69.2,
                    Gender = Gender.M,
                    Birthday = new DateTime(1986, 10, 9, 0, 0, 0, DateTimeKind.Utc),
                    CompanyId = Guid.Parse("c2cbfe28-f82a-4904-8075-bf98729d434f")
                },
                new Employee
                {
                    Number = 3,
                    Identification = Guid.Parse("aad8a5ec-9b5a-4c5f-9d3b-d7a39df6392f"),
                    Name = "Kirin",
                    Introduce = "I'm Kirin",
                    Weight = 73.8,
                    Gender = Gender.M,
                    Birthday = new DateTime(1984, 7, 8),
                    CompanyId = Guid.Parse("5dd641dd-2ba4-4dfd-9572-81325ecd8940")
                },
                new Employee
                {
                    Number = 4,
                    Identification = Guid.Parse("a4e918a9-46f9-4a13-8f37-e8771bf7dc5c"),
                    Name = "Rock",
                    Introduce = "I'm Rock",
                    Salary = 1750,
                    Weight = 82.1,
                    Gender = Gender.M,
                    Birthday = new DateTime(1976, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                    CompanyId = Guid.Parse("80a6570c-ca98-4661-adde-e4d5a8637ee5")
                },
                new Employee
                {
                    Number = 5,
                    Identification = Guid.Parse("3e5f4514-1a3f-402a-8e97-a2fa95ccb91b"),
                    Name = "Pikachu",
                    Introduce = "Pika~ Pika~",
                    Salary = 6600,
                    Weight = 52.9,
                    Gender = Gender.P,
                    Birthday = new DateTime(2005, 3, 16),
                    CompanyId = Guid.Parse("80a6570c-ca98-4661-adde-e4d5a8637ee5")
                },
                new Employee
                {
                    Number = 6,
                    Identification = Guid.Parse("0814bceb-3481-49c6-8dfb-0a398f2b55f3"),
                    Name = "Zed",
                    Introduce = "Zaaaaaaaaaa!!!!!",
                    Salary = 3000,
                    Weight = 71.6,
                    Gender = Gender.M,
                    Birthday = new DateTime(2003, 1, 22),
                    CompanyId = Guid.Parse("80a6570c-ca98-4661-adde-e4d5a8637ee5")
                }
            );

            // Add company data
            modelBuilder.Entity<Company>().HasData(
                new Company
                {
                    Id = Guid.Parse("c2cbfe28-f82a-4904-8075-bf98729d434f"),
                    Name = "Microsoft"
                },
                new Company
                {
                    Id = Guid.Parse("5dd641dd-2ba4-4dfd-9572-81325ecd8940"),
                    Name = "Google"
                },
                new Company
                {
                    Id = Guid.Parse("80a6570c-ca98-4661-adde-e4d5a8637ee5"),
                    Name = "Apple"
                }
            );
        }
    }
}