using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using KendoNET.DynamicLinq.Test.Models;

namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class DataSourceResultTest
    {
        [Test]
        public void ToDataSourceResult_DataProperty_IsStronglyTypedWithoutCast()
        {
            var employees = new List<Employee>
            {
                new Employee { Number = 1, Name = "Ada", Salary = 100m }
            }.AsQueryable();

            var result = employees.ToDataSourceResult(10, 0, null, null);

            IEnumerable<Employee> data = result.Data;

            Assert.AreEqual(1, data.Count());
            Assert.AreEqual("Ada", data.First().Name);
        }

        [Test]
        public void ToDataSourceResult_JsonSerialization_ProducesKendoSchemaFields()
        {
            var employees = new List<Employee>
            {
                new Employee { Number = 1, Name = "Ada", Salary = 100m, Gender = Gender.F }
            }.AsQueryable();

            var result = employees.ToDataSourceResult(10, 0, null, null, new[]
            {
                new Aggregator { Aggregate = "sum", Field = "Salary" }
            }, new[]
            {
                new Group { Field = "Gender", Dir = "asc" }
            });

            // Not CustomJsonSerializerOptions.DefaultOptions — its converter throws on Write.
            var json = JsonSerializer.Serialize(result);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.IsTrue(root.TryGetProperty("Data", out var dataElement));
            Assert.IsTrue(root.TryGetProperty("Groups", out var groupsElement));
            Assert.IsTrue(root.TryGetProperty("Aggregates", out var aggregatesElement));
            Assert.IsTrue(root.TryGetProperty("Total", out var totalElement));
            Assert.IsTrue(root.TryGetProperty("Errors", out var errorsElement));

            Assert.AreEqual(1, totalElement.GetInt32());
            Assert.AreEqual(100m, aggregatesElement.GetProperty("Salary").GetProperty("sum").GetDecimal());

            Assert.AreEqual(JsonValueKind.Null, dataElement.ValueKind);
            Assert.AreEqual(1, groupsElement.GetArrayLength());
            Assert.AreEqual(JsonValueKind.Null, errorsElement.ValueKind);
        }

        [Test]
        public void ToDataSourceResult_DataContractSerialization_KnownAggregateTypeSucceeds()
        {
            var employees = new List<Employee>
            {
                new Employee { Number = 1, Name = "Ada", Salary = 100m }
            }.AsQueryable();

            // No group: GroupResult.Field has no setter, which DataContractSerializer requires.
            var result = employees.ToDataSourceResult(10, 0, null, null, new[]
            {
                new Aggregator { Aggregate = "sum", Field = "Salary" }
            }, null);

            var serializer = new DataContractSerializer(typeof(DataSourceResult<Employee>));
            using var stream = new MemoryStream();

            Assert.DoesNotThrow(() => serializer.WriteObject(stream, result));
        }

        [Test]
        public async Task ToDataSourceResultAsync_DataProperty_IsStronglyTypedWithoutCast()
        {
            var employees = new List<Employee>
            {
                new Employee { Number = 1, Name = "Ada", Salary = 100m }
            }.AsQueryable();

            var result = await employees.ToDataSourceResultAsync(10, 0, null, null);

            IEnumerable<Employee> data = result.Data;

            Assert.AreEqual(1, data.Count());
            Assert.AreEqual("Ada", data.First().Name);
        }
    }
}
