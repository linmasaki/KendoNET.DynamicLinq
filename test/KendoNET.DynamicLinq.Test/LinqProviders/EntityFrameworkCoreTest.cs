using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using KendoNET.DynamicLinq.Test.Data;
using KendoNET.DynamicLinq.Test.Models;

namespace KendoNET.DynamicLinq.Test.LinqProviders
{
    /// <summary>
    /// Runs against EF Core InMemory. See MockContext.
    /// </summary>
    [TestFixture]
    public class EntityFrameworkCoreTest
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };

        private MockContext _dbContext;

        // Each case is parsed afresh per call, because ToDataSourceResult rewrites filter values in place.
        public static IEnumerable<string> TestRequests
        {
            get
            {
                yield return "{\"take\":3,\"skip\":2,\"sort\":[{\"field\":\"Number\",\"dir\":\"desc\"}]}";
                yield return "{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Salary\",\"operator\":\"gt\",\"value\":999},{\"field\":\"Salary\",\"operator\":\"lt\",\"value\":6000}]}}";
                yield return "{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Name\",\"operator\":\"contains\",\"value\":\"i\"}]}}";
                yield return "{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"},{\"field\":\"Salary\",\"aggregate\":\"average\"},{\"field\":\"Number\",\"aggregate\":\"max\"},{\"field\":\"Number\",\"aggregate\":\"min\"},{\"field\":\"Number\",\"aggregate\":\"count\"},{\"field\":\"Birthday\",\"aggregate\":\"count\"}]}";
                yield return "{\"take\":4,\"skip\":0,\"sort\":[{\"field\":\"Number\",\"dir\":\"asc\"}],\"group\":[{\"field\":\"Gender\",\"dir\":\"asc\",\"aggregates\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"}]}],\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"}]}";
                yield return "{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Unknown\",\"operator\":\"eq\",\"value\":1}]}}";
                yield return "{\"take\":0,\"skip\":0}";
                yield return "{\"take\":10,\"skip\":0,\"group\":[{\"field\":\"Gender\",\"dir\":\"desc\"}]}";
                yield return "{\"take\":10,\"skip\":0,\"aggregate\":[]}";
                yield return "{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"median\"},{\"field\":\"Number\",\"aggregate\":\"max\"}]}";
            }
        }

        [SetUp]
        public void Setup()
        {
            _dbContext = MockContext.GetDefaultInMemoryDbContext();
        }

        [Test]
        public void FindAsyncMethod_Should_ReturnProviderAsyncMethod()
        {
            var provider = _dbContext.Employee.AsQueryable().Provider.GetType();

            VerifyAsyncMethodFound(typeof(EntityFrameworkQueryableExtensions), "CountAsync", QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, int>>(Queryable.Count)));
            VerifyAsyncMethodFound(typeof(EntityFrameworkQueryableExtensions), "SumAsync", QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, Expression<Func<Employee, decimal?>>, decimal?>>(Queryable.Sum)));
            VerifyAsyncMethodFound(typeof(EntityFrameworkQueryableExtensions), "MaxAsync", QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, Expression<Func<Employee, long>>, long>>(Queryable.Max)));
            VerifyAsyncMethodFound(typeof(EntityFrameworkQueryableExtensions), "ToListAsync", QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IEnumerable<Employee>, List<Employee>>>(Enumerable.ToList)));
        }

        [TestCaseSource(nameof(TestRequests))]
        public async Task ToDataSourceResultAsync_Should_MatchSyncResult(string request)
        {
            var asyncResult = await _dbContext.Employee.AsQueryable().ToDataSourceResultAsync(ParseRequest(request));
            var syncResult = _dbContext.Employee.AsQueryable().ToDataSourceResult(ParseRequest(request));

            AssertSameResult(syncResult, asyncResult);
        }

        [Test]
        public async Task ToDataSourceResultAsync_Should_MatchSyncResultWithParameterOverload()
        {
            var filter = new Filter { Logic = "and", Filters = [new Filter { Field = "Company.Name", Operator = "contains", Value = "Microsoft" }] };

            var asyncResult = await _dbContext.Employee.AsQueryable().ToDataSourceResultAsync(10, 0, filter, null, [new Aggregator { Field = "Salary", Aggregate = "sum" }], [new Sort { Field = "Number", Dir = "asc" }]);
            var syncResult = _dbContext.Employee.AsQueryable().ToDataSourceResult(10, 0, filter, null, [new Aggregator { Field = "Salary", Aggregate = "sum" }], [new Sort { Field = "Number", Dir = "asc" }]);

            Assert.AreEqual(2, asyncResult.Total);
            AssertSameResult(syncResult, asyncResult);
        }

        private static DataSourceRequest ParseRequest(string json) => JsonSerializer.Deserialize<DataSourceRequest>(json, CustomJsonSerializerOptions.DefaultOptions);

        private static MethodInfo GetOverloadMethod<TDelegate>(TDelegate method) where TDelegate : Delegate => method.Method;

        // Compared as JSON so the dynamic aggregate objects and nested groups are compared too
        private static void AssertSameResult<T>(DataSourceResult<T> expected, DataSourceResult<T> actual)
        {
            Assert.AreEqual(JsonSerializer.Serialize(expected, s_jsonOptions), JsonSerializer.Serialize(actual, s_jsonOptions));
        }

        private static void VerifyAsyncMethodFound(Type declaringType, string name, MethodInfo asyncMethod)
        {
            Assert.IsNotNull(asyncMethod);
            Assert.AreEqual(declaringType, asyncMethod.DeclaringType);
            Assert.AreEqual(name, asyncMethod.Name);
        }
    }
}
