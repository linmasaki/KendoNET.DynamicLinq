using NUnit.Framework;
using System.Linq.Dynamic.Core;
using Kendo.DynamicLinqCore.Tests.Data;

#if NETCOREAPP3_1
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif

namespace Kendo.DynamicLinqCore.Tests
{
    [TestFixture]
    public class GroupTest
    {
        private MockContext _dbContext;

        #if NETCOREAPP3_1
        private JsonSerializerOptions jsonSerializerOptions = CustomJsonSerializerOptions.DefaultOptions;
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
                var request = JsonSerializer.Deserialize<DataSourceRequest>("{\"take\":20,\"skip\":0,\"sort\":[{\"field\":\"Number\",\"dir\":\"desc\"}],\"group\":[{\"field\":\"Gender\"}]}", jsonSerializerOptions);
            #else
                var request = JsonConvert.DeserializeObject<DataSourceRequest>("{\"take\":20,\"skip\":0,\"sort\":[{\"field\":\"Number\",\"dir\":\"desc\"}],\"group\":[{\"field\":\"Gender\"}]}");
            #endif

            var result = _dbContext.Employee.AsQueryable().ToDataSourceResult(request);
            var groupItems = result.Groups.ToDynamicList().Count;
            Assert.AreEqual(3, groupItems);
        }

    }
}