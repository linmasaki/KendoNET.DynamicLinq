using NUnit.Framework;
using System.Collections.Generic;
using KendoNET.DynamicLinq.Test.Data;
using System.Text.Json;

namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class AggregatorTest
    {
        private MockContext _dbContext;

        private static JsonSerializerOptions jsonSerializerOptions = CustomJsonSerializerOptions.DefaultOptions;

        public static IEnumerable<DataSourceRequest> DataSourceRequestWithAggregateSalarySum
        {
            get
            {
                yield return JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"}]}", jsonSerializerOptions);
            }
        }

        public static IEnumerable<DataSourceRequest> DataSourceRequestWithManyAggregates
        {
            get
            {
                yield return JsonSerializer.Deserialize<DataSourceRequest>(
                    "{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"},{\"field\":\"Salary\",\"aggregate\":\"average\"},{\"field\":\"Number\",\"aggregate\":\"max\"}]}",
                    jsonSerializerOptions);
            }
        }

        [SetUp]
        public void Setup()
        {
            _dbContext = MockContext.GetDefaultInMemoryDbContext();
        }

        [Test]
        public void InputParameter_DecimalSum_CheckResultObjectString()
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, null, new[]
            {
                new Aggregator
                {
                    Aggregate = "sum",
                    Field = "Salary"
                }
            }, null);

            object expectedObject = "{ Salary = { sum = 14850 } }";
            Assert.AreEqual(expectedObject, result.Aggregates.ToString());
        }

        [TestCaseSource(nameof(DataSourceRequestWithAggregateSalarySum))]
        public void InputDataSourceRequest_DecimalSum_CheckResultObjectString(DataSourceRequest dataSourceRequest)
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(dataSourceRequest);

            object expectedObject = "{ Salary = { sum = 14850 } }";
            Assert.AreEqual(expectedObject, result.Aggregates.ToString());
        }

        [TestCaseSource(nameof(DataSourceRequestWithAggregateSalarySum))]
        public void InputDataSourceRequest_DecimalSum_CheckResultSum(DataSourceRequest dataSourceRequest)
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(dataSourceRequest);
            var salaryAggregates = result.Aggregates.GetType().GetProperty("Salary")?.GetValue(result.Aggregates, null);
            var salarySum = salaryAggregates?.GetType().GetProperty("sum")?.GetValue(salaryAggregates, null);

            const decimal expectedSalarySum = 14850;
            Assert.AreEqual(expectedSalarySum, salarySum);

            const decimal incorrectSalarySum = 9999;
            Assert.AreNotEqual(incorrectSalarySum, salarySum);
        }

        [Test]
        public void InputParameter_ManyAggregators_CheckResultObjectString()
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, null, new[]
            {
                new Aggregator
                {
                    Aggregate = "sum",
                    Field = "Salary"
                },
                new Aggregator
                {
                    Aggregate = "average",
                    Field = "Salary"
                },
                new Aggregator
                {
                    Aggregate = "max",
                    Field = "Number"
                },
            }, null);

            object expectedObject = "{ Salary = { sum = 14850, average = 2970 }, Number = { max = 7 } }";
            Assert.AreEqual(expectedObject, result.Aggregates.ToString());
        }

        [TestCaseSource(nameof(DataSourceRequestWithManyAggregates))]
        public void InputDataSourceRequest_ManyAggregators_CheckResultObjectString(DataSourceRequest dataSourceRequest)
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(dataSourceRequest);

            object expectedObject = "{ Salary = { sum = 14850, average = 2970 }, Number = { max = 7 } }";
            Assert.AreEqual(expectedObject, result.Aggregates.ToString());
        }
    }
}