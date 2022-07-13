using NUnit.Framework;
using System.Linq.Dynamic.Core;
using KendoNET.DynamicLinq.Test.Data;

#if NETCOREAPP3_1
using System.Text.Json;
#endif

#if NETCOREAPP2_1 || NETCOREAPP2_2
using Newtonsoft.Json;
#endif

namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class GroupTest
    {
        private MockContext _dbContext;

#if NETCOREAPP3_1
        private JsonSerializerOptions _jsonSerializerOptions = CustomJsonSerializerOptions.DefaultOptions;
#endif

        [SetUp]
        public void Setup()
        {
            _dbContext = MockContext.GetDefaultInMemoryDbContext();
        }

        [Test]
        public void DataSourceRequest_EnumField_GroupedCount()
        {
            // source string = {"take":20,"skip":0,"sort":[{"field":"Number","dir":"desc"}],"group":[{"field":"Gender"}]}

#if NETCOREAPP3_1
            var request = JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":20,\"skip\":0,\"sort\":[{\"field\":\"Number\",\"dir\":\"desc\"}],\"group\":[{\"field\":\"Gender\"}]}",
                _jsonSerializerOptions);
#else
            var request = JsonConvert.DeserializeObject<DataSourceRequest>("{\"take\":20,\"skip\":0,\"sort\":[{\"field\":\"Number\",\"dir\":\"desc\"}],\"group\":[{\"field\":\"Gender\"}]}");
#endif

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            var groupItems = result.Groups.ToDynamicList().Count;
            Assert.AreEqual(3, groupItems);
        }
    }
}