using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using KendoNET.DynamicLinq.Test.Models;

namespace KendoNET.DynamicLinq.Test.Data
{
    public class MockContext : DbContext
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

        public MockContext(DbContextOptions<MockContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>().HasData(MockData.Employees());
            modelBuilder.Entity<Company>().HasData(MockData.Companies());
        }
    }
}