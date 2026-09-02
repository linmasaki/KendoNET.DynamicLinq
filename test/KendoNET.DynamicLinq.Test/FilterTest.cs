using System.Linq;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using KendoNET.DynamicLinq.Test.Data;
using System.Text.Json;

namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class FilterTest
    {
        private MockContext _dbContext;

        private JsonSerializerOptions jsonSerializerOptions = CustomJsonSerializerOptions.DefaultOptions;

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
                        Field = "Company.Name",
                        Operator = "contains",
                        Value = "Microsoft"
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

            var request = JsonSerializer.Deserialize<DataSourceRequest>(
                "{\"take\":20,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gt\",\"value\":999.00},{\"field\":\"Salary\",\"operator\":\"lt\",\"value\":6000.00}]}}",
                jsonSerializerOptions);
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(4, result.Total);
        }

        [Test]
        public void InputDataSourceRequest_DoubleGreaterAndLessEqual_CheckResultCount()
        {
            // source string = {"take":20,"skip":0,"filter":{"logic":"and","filters":[{"field":"Weight","operator":"gt","value":48},{"field":"Weight","operator":"lt","value":69.2}]}}

            var request = JsonSerializer.Deserialize<DataSourceRequest>(
                "{\"take\":20,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Weight\",\"operator\":\"gt\",\"value\":48},{\"field\":\"Weight\",\"operator\":\"lte\",\"value\":69.2}]}}",
                jsonSerializerOptions);
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(3, result.Total);
        }

        [Test]
        public void InputDataSourceRequest_ManyConditions_CheckResultCount()
        {
            // source string = {\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1986-10-09T16:00:00.000Z\"},{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1976-11-05T16:00:00.000Z\"}]},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}

            var request = JsonSerializer.Deserialize<DataSourceRequest>(
                "{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1986-10-09T00:00:00.000Z\"},{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1976-11-05T00:00:00.000Z\"}]},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}",
                jsonSerializerOptions);
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(2, result.Total);
        }

        [TestCase("contains", "co", 1)]
        [TestCase("startswith", "PIKA", 1)]
        [TestCase("doesnotcontain", "coco", 5)]
        [TestCase("eq", "zed", 1)]
        [TestCase("neq", null, 6)]
        public void InputParameter_IgnoreCase_CheckResultCount(string op, string value, int expected)
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Field = "Name",
                Operator = op,
                Value = value,
                IgnoreCase = true,
                Logic = "and"
            });

            Assert.AreEqual(expected, result.Total);
        }

        [Test]
        public void InputParameter_IgnoreCase_NullFieldAndNullValue_CheckResultCount()
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Field = "Name",
                Operator = "eq",
                Value = null,
                IgnoreCase = true,
                Logic = "and"
            });

            Assert.AreEqual(1, result.Total);
        }


        [TestCase("contains", "co", 0)]
        [TestCase("contains", "Co", 1)]
        [TestCase("startswith", "PIKA", 0)]
        [TestCase("startswith", "Pika", 1)]
        [TestCase("doesnotcontain", "co", 6)]
        [TestCase("doesnotcontain", "Co", 5)]
        [TestCase("eq", "zed", 0)]
        [TestCase("eq", "Zed", 1)]
        [TestCase("neq", null, 6)]
        public void InputParameter_WithoutIgnoreCase_CheckResultCount(string op, string value, int expected)
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Field = "Name",
                Operator = op,
                Value = value,
                Logic = "and",
                IgnoreCase = false
            });

            Assert.AreEqual(expected, result.Total);
        }

        [TestCase("contains")]
        [TestCase("doesnotcontain")]
        [TestCase("startswith")]
        [TestCase("endswith")]
        public void InputParameter_StringOperatorWithNullValue_CheckErrors(string op)
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Field = "Name",
                Operator = op,
                Value = null,
                Logic = "and"
            });

            Assert.IsNotNull(result.Errors);
        }

        [TestCase("School", true)]
        [TestCase("Company.NickName", true)]
        [TestCase(null, true)]
        [TestCase("", true)]
        [TestCase("Name", false)]
        [TestCase("Company.Name", false)]
        public void InputParameter_WithUnknownField_CheckErrors(string field, bool hasErrors)
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Field = field,
                Operator = "eq",
                Value = "Zed",
                Logic = "and"
            });

            Assert.AreEqual(hasErrors, result.Errors != null);
        }
    }
}