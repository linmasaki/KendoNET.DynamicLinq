using NUnit.Framework;
using Kendo.DynamicLinqCore.Tests.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

#if NETCOREAPP3_1
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif


namespace Kendo.DynamicLinqCore.Tests
{
    [TestFixture]
    public class FilterTest
    {
        private MockContext _dbContext;

#if NETCOREAPP3_1
        private JsonSerializerOptions jsonSerializerOptions = CustomJsonSerializerOptions.DefaultOptions;
#endif

        [SetUp]
        public void Setup()
        {
            _dbContext = MockContext.GetDefaultInMemoryDbContext();
        }

        [Test]
        public void InputParameter_SubPropertyContains_CheckResultCount()
        {
            var result = _dbContext.Employee.Include(x => x.Company).AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Field = "Company.Name",
                Value = "Microsoft",
                Operator = "contains",
                Logic = "and"
            });

            Assert.AreEqual(2, result.Total);

            var result2 = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Filters = new[]
                {
                    new Filter
                    {
                        Field ="Company.Name",
                        Operator = "contains",
                        Value = "Microsoft"
                    }
                },
                Logic = "and"
            });

            Assert.AreEqual(2, result2.Total);
        }

        [Test]
        public void InputParameter_SubPropertyContains_Case_Insensitive_CheckResultCount()
        {
            var result = _dbContext.Employee.Include(x => x.Company).AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Field = "Company.Name",
                Value = "microsoft",
                Operator = "contains",
                Logic = "and",
                ignoreCase = true,
            });

            Assert.AreEqual(2, result.Total);

            var result2 = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Filters = new[]
                {
                    new Filter
                    {
                        Field ="Company.Name",
                        Operator = "contains",
                        Value = "microsoft",
                        ignoreCase = true,
                    }
                },
                Logic = "and"
            });

            Assert.AreEqual(2, result2.Total);
        }

        [Test]
        public void InputDataSourceRequest_DecimalGreaterAndLess_CheckResultCount()
        {
            // source string = {"take":20,"skip":0,"filter":{"logic":"and","filters":[{"field":"Salary","operator":"gt","value":999.00},{"field":"Salary","operator":"lt","value":6000.00}]}}

#if NETCOREAPP3_1
                var request = JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":20,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gt\",\"value\":999.00},{\"field\":\"Salary\",\"operator\":\"lt\",\"value\":6000.00}]}}", jsonSerializerOptions);
#else
            var request = JsonConvert.DeserializeObject<DataSourceRequest>("{\"take\":20,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gt\",\"value\":999.00},{\"field\":\"Salary\",\"operator\":\"lt\",\"value\":6000.00}]}}");
#endif

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(4, result.Total);
        }

        [Test]
        public void InputDataSourceRequest_DoubleGreaterAndLessEqual_CheckResultCount()
        {
            // source string = {"take":20,"skip":0,"filter":{"logic":"and","filters":[{"field":"Weight","operator":"gt","value":48},{"field":"Weight","operator":"lt","value":69.2}]}}

#if NETCOREAPP3_1
                var request = JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":20,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Weight\",\"operator\":\"gt\",\"value\":48},{\"field\":\"Weight\",\"operator\":\"lte\",\"value\":69.2}]}}", jsonSerializerOptions);
#else
            var request = JsonConvert.DeserializeObject<DataSourceRequest>("{\"take\":20,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Weight\",\"operator\":\"gt\",\"value\":48},{\"field\":\"Weight\",\"operator\":\"lte\",\"value\":69.2}]}}");
#endif

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(3, result.Total);
        }

        [Test]
        public void InputDataSourceRequest_ManyConditions_CheckResultCount()
        {
            // source string = {\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1986-10-09T16:00:00.000Z\"},{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1976-11-05T16:00:00.000Z\"}]},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}

#if NETCOREAPP3_1
                var request = JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1986-10-09T16:00:00.000Z\"},{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1976-11-05T16:00:00.000Z\"}]},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}", jsonSerializerOptions);            
#else
            var request = JsonConvert.DeserializeObject<DataSourceRequest>("{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1986-10-09T16:00:00.000Z\"},{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1976-11-05T16:00:00.000Z\"}]},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}");
#endif

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(2, result.Total);
        }

    }
}