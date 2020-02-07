using NUnit.Framework;
using Kendo.DynamicLinqCore.Tests.Data;

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
        public void InputParameter_EmployeeCompanyNameContainsMicrosoft_CheckResultCount()
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter {
                Field ="Company.Name",
                Value = "Microsoft",
                Operator = "contains",
                Logic = "and"
            });

            Assert.AreEqual(2, result.Total);

            var result2 = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter {
                Filters = new [] 
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
        public void InputDataSourceRequest_EmployeeSalaryMoreThanAndLess_CheckResultCount()
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
        public void InputDataSourceRequest_ManyFilters_CheckResultCount()
        {
            // source string = {"take":10,"skip":0,"filter":{"logic":"and","filters":[{"field":"Birthday","operator":"gt","value":"1980-11-05T00:00:00.000Z"},{"logic":"and","filters":[{"field":"Salary","operator":"gte","value":1000},{"field":"Salary","operator":"lte","value":6000}]}]}}

            #if NETCOREAPP3_1
                var request = JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"gt\",\"value\":\"1980-11-05T00:00:00.000Z\"},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}", jsonSerializerOptions);            
            #else
                var request = JsonConvert.DeserializeObject<DataSourceRequest>("{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"gt\",\"value\":\"1980-11-05T00:00:00.000Z\"},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}");
            #endif

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(3, result.Total);
        }

    }
}