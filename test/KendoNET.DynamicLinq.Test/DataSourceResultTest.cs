using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KendoNET.DynamicLinq.Test.Data;
using KendoNET.DynamicLinq.Test.Models;

namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class DataSourceResultTest
    {
        private MockContext _dbContext;

        [SetUp]
        public void Setup()
        {
            _dbContext = MockContext.GetDefaultInMemoryDbContext();
        }

        [Test]
        public void InputParameter_PlainQuery_CheckDataIsStronglyTyped()
        {
            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, null, null);

            IEnumerable<Employee> data = result.Data;

            Assert.AreEqual(7, data.Count());
        }

        [Test]
        public void InputParameter_GroupedWithAggregate_CheckJsonSchemaFields()
        {
            var employees = new List<Employee>
            {
                new Employee { Number = 1, Name = "Ada", Salary = 100m, Gender = Gender.F },
                new Employee { Number = 2, Name = "Grace", Salary = 200m, Gender = Gender.F },
                new Employee { Number = 3, Name = "Bob", Salary = 300m, Gender = Gender.M }
            }.AsQueryable();

            var result = employees.ToDataSourceResult(10, 0, null, [new Group { Field = "Gender", Dir = "asc" }], [new Aggregator { Aggregate = "sum", Field = "Salary" }], null);
            var json = JsonSerializer.Serialize(result);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.IsTrue(root.TryGetProperty("Data", out var dataElement));
            Assert.IsTrue(root.TryGetProperty("Groups", out var groupsElement));
            Assert.IsTrue(root.TryGetProperty("Aggregates", out var aggregatesElement));
            Assert.IsTrue(root.TryGetProperty("Total", out var totalElement));
            Assert.IsTrue(root.TryGetProperty("Errors", out var errorsElement));

            Assert.AreEqual(3, totalElement.GetInt32());
            Assert.AreEqual(600m, aggregatesElement.GetProperty("Salary").GetProperty("sum").GetDecimal());     // "{ Salary = { sum = 600 } }"
            Assert.AreEqual(2, groupsElement.GetArrayLength());

            Assert.AreEqual(JsonValueKind.Null, dataElement.ValueKind);
            Assert.AreEqual(JsonValueKind.Null, errorsElement.ValueKind);
        }
    }
}
