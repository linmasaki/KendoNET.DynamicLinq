using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Kendo.DynamicLinqCore.Tests
{
    [TestFixture]
    public class AggregatorTest
    {
        private MockContext dbContext;

        [SetUp]
        public void Setup()
        {
            //dbContext = MockContext.GetDefaultInMemoryDbContext();
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Check_Employee_Count(int count)
        {
            var serviceProvider = new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();
            var builder = new DbContextOptionsBuilder<MockContext>();
            var options = builder.UseInMemoryDatabase("Kendo").UseInternalServiceProvider(serviceProvider).Options;
            
            using (var context = new MockContext(options))
            {
                //You have to create the database
                context.Database.EnsureCreated();

                var result = context.Employee.CountAsync().Result;
                Assert.AreEqual(result, count);
            }
        
            
        }

        [Test]
        public void Test1()
        {


            Assert.Pass();
        }
    }
}