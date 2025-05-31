using System.Linq.Dynamic.Core;
using System.Text.Json;
using KendoNET.DynamicLinq.Test.Data;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class GroupTestSystem
    {
        private MockContext _dbContext;


        private readonly JsonSerializerOptions _jsonSerializerOptions = CustomJsonSerializerOptions.DefaultOptions;


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
            var groupItems = result.Groups.ToDynamicList().Count;
            ClassicAssert.AreEqual(3, groupItems);
        }
    }
}