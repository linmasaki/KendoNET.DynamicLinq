using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;
using KendoNET.DynamicLinq.Test.Data;
using KendoNET.DynamicLinq.Test.Models;

namespace KendoNET.DynamicLinq.Test.LinqProviders
{
    /// <summary>
    /// Runs only when KENDONET_MONGODB holds a connection string; otherwise every test is ignored. See MongoDbContext.
    /// </summary>
    [TestFixture]
    public class MongoDbTest
    {
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

        [Test]
        public void FindAsyncMethod_Should_ReturnProviderAsyncMethod()
        {
            var provider = GetEmployees().AsQueryable().Provider.GetType();

            VerifyAsyncMethodFound(typeof(MongoQueryable), "CountAsync", QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, int>>(Queryable.Count)));
            VerifyAsyncMethodFound(typeof(MongoQueryable), "SumAsync", QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, Expression<Func<Employee, decimal?>>, decimal?>>(Queryable.Sum)));
            VerifyAsyncMethodFound(typeof(MongoQueryable), "MaxAsync", QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, Expression<Func<Employee, long>>, long>>(Queryable.Max)));
            VerifyAsyncMethodFound(typeof(MongoQueryable), "ToListAsync", QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IEnumerable<Employee>, List<Employee>>>(Enumerable.ToList)));
        }

        [Test]
        public void ToDataSourceResult_Should_ComputeAggregates()
        {
            var result = GetEmployees().AsQueryable().ToDataSourceResult(ParseRequest("{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"},{\"field\":\"Number\",\"aggregate\":\"max\"}]}"));

            Assert.AreEqual("{ Salary = { sum = 14850 }, Number = { max = 7 } }", result.Aggregates.ToString());
        }

        [TestCaseSource(nameof(TestRequests))]
        public async Task ToDataSourceResultAsync_Should_MatchSyncResult(string request)
        {
            var employees = GetEmployees();
            var asyncResult = await employees.AsQueryable().ToDataSourceResultAsync(ParseRequest(request));
            var syncResult = employees.AsQueryable().ToDataSourceResult(ParseRequest(request));

            // Compared as JSON so the dynamic aggregate objects and nested groups are compared too
            Assert.AreEqual(JsonSerializer.Serialize(syncResult), JsonSerializer.Serialize(asyncResult));
        }

        private static DataSourceRequest ParseRequest(string json) => JsonSerializer.Deserialize<DataSourceRequest>(json, CustomJsonSerializerOptions.DefaultOptions);

        private static MethodInfo GetOverloadMethod<TDelegate>(TDelegate method) where TDelegate : Delegate => method.Method;

        private static IMongoCollection<Employee> GetEmployees()
        {
            var employees = MongoDbContext.Employees();
            if (employees == null) Assert.Ignore($"Set {MongoDbContext.ConnectionStringVariable} to run the MongoDB tests.");
            return employees;
        }

        private static void VerifyAsyncMethodFound(Type declaringType, string name, MethodInfo asyncMethod)
        {
            Assert.IsNotNull(asyncMethod);
            Assert.AreEqual(declaringType, asyncMethod.DeclaringType);
            Assert.AreEqual(name, asyncMethod.Name);
        }
    }
}
