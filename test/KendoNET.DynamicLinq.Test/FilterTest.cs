using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using KendoNET.DynamicLinq.Test.Data;

namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class FilterTest
    {
        private MockContext _dbContext;
        private readonly JsonSerializerOptions _serializerOptions = CustomJsonSerializerOptions.DefaultOptions;

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
                _serializerOptions);
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(4, result.Total);
        }

        [Test]
        public void InputDataSourceRequest_DoubleGreaterAndLessEqual_CheckResultCount()
        {
            // source string = {"take":20,"skip":0,"filter":{"logic":"and","filters":[{"field":"Weight","operator":"gt","value":48},{"field":"Weight","operator":"lt","value":69.2}]}}
            var request = JsonSerializer.Deserialize<DataSourceRequest>(
                "{\"take\":20,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Weight\",\"operator\":\"gt\",\"value\":48},{\"field\":\"Weight\",\"operator\":\"lte\",\"value\":69.2}]}}",
                _serializerOptions);
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(3, result.Total);
        }

        [Test]
        public void InputDataSourceRequest_ManyConditions_CheckResultCount()
        {
            // source string = {\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1986-10-09T16:00:00.000Z\"},{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1976-11-05T16:00:00.000Z\"}]},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}
            var request = JsonSerializer.Deserialize<DataSourceRequest>(
                "{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"logic\":\"or\",\"filters\":[{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1986-10-09T00:00:00.000Z\"},{\"field\":\"Birthday\",\"operator\":\"eq\",\"value\":\"1976-11-05T00:00:00.000Z\"}]},{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gte\",\"value\":1000},{\"field\":\"Salary\",\"operator\":\"lte\",\"value\":6000}]}]}}",
                _serializerOptions);
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

        // The same instant in different time zones
        private static readonly object[] DifferentTimeZoneData =
        {
            new DateTime(2000, 5, 5),                                          // No time zone
            new DateTimeOffset(2000, 5, 5, 8, 0, 0, TimeSpan.FromHours(8)),    // UTC+8
            new DateTimeOffset(2000, 5, 4, 19, 0, 0, TimeSpan.FromHours(-5)),  // UTC-5
        };

        [TestCaseSource(nameof(DifferentTimeZoneData))]
        public void InputParameter_DateTimeInDifferentTimeZones_CheckResultCount(object value)
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, new Filter
            {
                Field = "Birthday",
                Value = value,
                Operator = "eq",
                Logic = "and"
            });

            Assert.AreEqual(1, result.Total);
        }

        [Test]
        public void InputDataSourceRequest_DateTimeOffsetAcrossDates_CheckResultCount()
        {
            // Kirin was hired at 07:00 on the 2nd at +08:00, which is 23:00 on the 1st in UTC.
            var request = JsonSerializer.Deserialize<DataSourceRequest>(
                "{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"CreatedOn\",\"operator\":\"eq\",\"value\":\"2018-01-01T23:00:00.000Z\"}]}}",
                _serializerOptions);

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            Assert.AreEqual(1, result.Total);
        }
    }
}