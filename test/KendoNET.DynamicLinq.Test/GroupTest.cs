using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using KendoNET.DynamicLinq.Test.Data;
using System.Text.Json;

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
        public void DataSourceRequest_EnumField_GroupedCount()
        {
            // source string = {"take":20,"skip":0,"sort":[{"field":"Number","dir":"desc"}],"group":[{"field":"Gender"}]}

            var request = JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":20,\"skip\":0,\"sort\":[{\"field\":\"Number\",\"dir\":\"desc\"}],\"group\":[{\"field\":\"Gender\"}]}",
                _jsonSerializerOptions);

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);

            IEnumerable<GroupResult> groups = result.Groups;

            Assert.AreEqual(3, groups.Count());
        }
    }
}