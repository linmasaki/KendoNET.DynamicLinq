using NUnit.Framework;
using System.Collections.Generic;
using Kendo.DynamicLinqCore.Tests.Data;

#if NETCOREAPP3_1
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif

namespace Kendo.DynamicLinqCore.Tests
{
    [TestFixture]
    public class AggregatorTest
    {
        private MockContext _dbContext;

        #if NETCOREAPP3_1
        private static JsonSerializerOptions jsonSerializerOptions = CustomJsonSerializerOptions.DefaultOptions;
        #endif

        public static IEnumerable<DataSourceRequest> DataSourceRequestWithAggregateSalarySum
        {
            get
            {
                #if NETCOREAPP3_1
                yield return JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"}]}", jsonSerializerOptions);
                #else
                yield return JsonConvert.DeserializeObject<DataSourceRequest>("{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"}]}");
                #endif
            }
        }

        public static IEnumerable<DataSourceRequest> DataSourceRequestWithManyAggregates
        {
            get
            {
                #if NETCOREAPP3_1
                yield return JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"},{\"field\":\"Salary\",\"aggregate\":\"average\"},{\"field\":\"Number\",\"aggregate\":\"max\"}]}", jsonSerializerOptions);
                #else
                yield return JsonConvert.DeserializeObject<DataSourceRequest>("{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"},{\"field\":\"Salary\",\"aggregate\":\"average\"},{\"field\":\"Number\",\"aggregate\":\"max\"}]}");
                #endif
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

            object expectedObject = "{ Salary = { sum = 14850, average = 2970 }, Number = { max = 5 } }";
            Assert.AreEqual(expectedObject, result.Aggregates.ToString());
        }

        [TestCaseSource(nameof(DataSourceRequestWithManyAggregates))]
        public void InputDataSourceRequest_ManyAggregators_CheckResultObjectString(DataSourceRequest dataSourceRequest)
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(dataSourceRequest);

            object expectedObject = "{ Salary = { sum = 14850, average = 2970 }, Number = { max = 5 } }";
            Assert.AreEqual(expectedObject, result.Aggregates.ToString());
        }
    }
}