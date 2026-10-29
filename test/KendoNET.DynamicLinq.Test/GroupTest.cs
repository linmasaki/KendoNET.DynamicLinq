using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using KendoNET.DynamicLinq.Test.Data;
using KendoNET.DynamicLinq.Test.Models;

namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class GroupTest
    {
        private MockContext _dbContext;

        private JsonSerializerOptions _jsonSerializerOptions = CustomJsonSerializerOptions.DefaultOptions;

        [SetUp]
        public void Setup()
        {
            _dbContext = MockContext.GetDefaultInMemoryDbContext();
        }

        [Test]
        public void ToDataSourceResult_Should_GroupByEnumField()
        {
            // source string = {"take":20,"skip":0,"sort":[{"field":"Number","dir":"desc"}],"group":[{"field":"Gender"}]}
            var request = JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":20,\"skip\":0,\"sort\":[{\"field\":\"Number\",\"dir\":\"desc\"}],\"group\":[{\"field\":\"Gender\"}]}", _jsonSerializerOptions);

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            IEnumerable<GroupResult> groups = result.Groups;

            Assert.AreEqual(3, groups.Count());
        }

        [Test]
        public void ToDataSourceResult_Should_KeepGroupsReadableAfterContextDisposed()
        {
            // The README's pattern: the method disposes its own context and the caller serializes the result afterwards.
            var options = new DbContextOptionsBuilder<MockContext>().UseInMemoryDatabase(nameof(ToDataSourceResult_Should_KeepGroupsReadableAfterContextDisposed)).Options;
            DataSourceResult<Employee> result;
            using (var context = new MockContext(options))
            {
                context.Database.EnsureCreated();
                result = context.Employee.AsQueryable().ToDataSourceResult(10, 0, null, [new Group { Field = "Gender", Dir = "asc" }], null, null);
            }

            Assert.AreEqual(3, result.Groups.Count());
        }

        [Test]
        public void ToDataSourceResult_Should_SerializeEveryFieldWithAggregates()
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