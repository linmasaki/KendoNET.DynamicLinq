using System.Linq;
using System.Text.Json;
using KendoNET.DynamicLinq.Test.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using NUnit.Framework.Legacy;


namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class FilterTestSystem
    {
        private MockContext _dbContext;

        private readonly JsonSerializerOptions jsonSerializerOptions = CustomJsonSerializerOptions.DefaultOptions;


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

            ClassicAssert.AreEqual(2, result.Total);

            var result2 = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Filters =
                [
                    new Filter
                    {
                        Field = "Company.Name",
                        Operator = "contains",
                        Value = "Microsoft"
                    }
                ],
                Logic = "and"
            });

            ClassicAssert.AreEqual(2, result2.Total);
        }

        [Test]
        public void InputDataSourceRequest_DecimalGreaterAndLess_CheckResultCount()
        {
            // source string = {"take":20,"skip":0,"filter":{"logic":"and","filters":[{"field":"Salary","operator":"gt","value":999.00},{"field":"Salary","operator":"lt","value":6000.00}]}}


            var request = JsonSerializer.Deserialize<DataSourceRequest>(
                "{\"take\":20,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gt\",\"value\":999.00},{\"field\":\"Salary\",\"operator\":\"lt\",\"value\":6000.00}]}}",
                jsonSerializerOptions);

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            ClassicAssert.AreEqual(4, result.Total);
        }

        [Test]
        public void InputDataSourceRequest_DoubleGreaterAndLessEqual_CheckResultCount()
        {
            // source string = {"take":20,"skip":0,"filter":{"logic":"and","filters":[{"field":"Weight","operator":"gt","value":48},{"field":"Weight","operator":"lt","value":69.2}]}}

            var request = JsonSerializer.Deserialize<DataSourceRequest>(
                "{\"take\":20,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Weight\",\"operator\":\"gt\",\"value\":48},{\"field\":\"Weight\",\"operator\":\"lte\",\"value\":69.2}]}}",
                jsonSerializerOptions);

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            ClassicAssert.AreEqual(3, result.Total);
        }

        [Test]
        public void InputDataSourceRequest_ManyConditions_CheckResultCount()
        {
            // source string = {\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1986-10-09T16:00:00.000Z\"},{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1976-11-05T16:00:00.000Z\"}]},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}

            var request = JsonSerializer.Deserialize<DataSourceRequest>(
                "{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1986-10-09T00:00:00.000Z\"},{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1976-11-05T00:00:00.000Z\"}]},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}",
                jsonSerializerOptions);

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            ClassicAssert.AreEqual(2, result.Total);
        }
    }
}