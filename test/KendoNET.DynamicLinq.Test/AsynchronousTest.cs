using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using KendoNET.DynamicLinq.Test.Data;
using KendoNET.DynamicLinq.Test.LinqProviders;
using KendoNET.DynamicLinq.Test.Models;

namespace KendoNET.DynamicLinq.Test
{
    [TestFixture]
    public class AsynchronousTest
    {
        // Uses every step at once, so every kind of query runs.
        // - filter: Weight > 40 drops #7 (Weight 0), so Total = 6
        // - aggregates: over those 6 rows; Birthday count = 5 (Kirin has none), Number count = 6
        // - sort, skip 1, take 4: #2 to #5, grouped into M (#2, #3, #4) and P (#5)
        private const string AllFeaturesRequest = "{\"take\":4,\"skip\":1,\"sort\":[{\"field\":\"Number\",\"dir\":\"asc\"}]," +
                                                  "\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Weight\",\"operator\":\"gt\",\"value\":40}]}," +
                                                  "\"group\":[{\"field\":\"Gender\",\"dir\":\"asc\"}]," +
                                                  "\"aggregate\":[{\"field\":\"Salary\",\"aggregate\":\"sum\"},{\"field\":\"Salary\",\"aggregate\":\"average\"},{\"field\":\"Number\",\"aggregate\":\"max\"},{\"field\":\"Number\",\"aggregate\":\"min\"},{\"field\":\"Number\",\"aggregate\":\"count\"},{\"field\":\"Birthday\",\"aggregate\":\"count\"}]}";

        [Test]
        public async Task ToDataSourceResultAsync_Should_RunEveryQueryThroughAsyncMethods()
        {
            var employees = MockData.Employees();
            var source = new MockQueryable<Employee>(employees);

            var result = await source.ToDataSourceResultAsync(ParseRequest(AllFeaturesRequest));

            CollectionAssert.AreEqual(new[] { "CountAsync", "SumAsync", "AverageAsync", "MaxAsync", "MinAsync", "CountAsync", "CountAsync", "ToListAsync" }, source.Calls.Select(c => c.Method));
            AssertSameResult(employees.AsQueryable().ToDataSourceResult(ParseRequest(AllFeaturesRequest)), result);
        }

        [Test]
        public async Task ToDataSourceResultAsync_Should_PassSameCancellationTokenToEveryQuery()
        {
            var source = new MockQueryable<Employee>(MockData.Employees());
            using var cancellation = new CancellationTokenSource();

            await source.ToDataSourceResultAsync(ParseRequest(AllFeaturesRequest), cancellation.Token);

            Assert.IsTrue(source.Calls.All(c => c.Token == cancellation.Token));
        }

        [Test]
        public void ToDataSourceResultAsync_Should_ThrowAsyncMethodExceptionWithoutSyncRetry()
        {
            var source = new MockQueryable<Employee>(MockData.Employees());
            var request = ParseRequest("{\"take\":10,\"skip\":0,\"filter\":{\"logic\":\"and\",\"filters\":[{\"field\":\"Number\",\"operator\":\"gt\",\"value\":100}]},\"aggregate\":[{\"field\":\"Number\",\"aggregate\":\"max\"}]}");

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => source.ToDataSourceResultAsync(request));

            // A sync retry would hit MockQueryable and throw "Synchronous execution." instead, so "no elements" indicates the async method failed directly without triggering a sync retry.
            StringAssert.Contains("no elements", exception.Message);
            CollectionAssert.AreEqual(new[] { "CountAsync", "MaxAsync" }, source.Calls.Select(c => c.Method));
        }

        [Test]
        public void CalculateAggregatesAsync_Should_RunSynchronouslyWhenUseAsyncIsFalse()
        {
            var source = new MockQueryable<Employee>(MockData.Employees());

            var task = QueryableExtensions.CalculateAggregatesAsync(source, [new Aggregator { Field = "Salary", Aggregate = "sum" }], useAsync: false, cancellationToken: default);

            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual("Synchronous execution.", Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult()).Message);
            Assert.IsEmpty(source.Calls);
        }

        [Test]
        public async Task ToDataSourceResultAsync_Should_FallBackToSyncWithoutAsyncMethods()
        {
            var employees = MockData.Employees();
            var provider = employees.AsQueryable().Provider.GetType();  // EnumerableQuery<T>

            // List's provider has no *Async methods, so every query falls back to sync
            Assert.IsNull(QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, int>>(Queryable.Count)));
            Assert.IsNull(QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IEnumerable<Employee>, List<Employee>>>(Enumerable.ToList)));

            var asyncResult = await employees.AsQueryable().ToDataSourceResultAsync(ParseRequest(AllFeaturesRequest));
            AssertSameResult(employees.AsQueryable().ToDataSourceResult(ParseRequest(AllFeaturesRequest)), asyncResult);
        }

        [Test]
        public void ToDataSourceResultAsync_Should_ThrowSameAsSyncForUnknownAggregateField()
        {
            var request = "{\"take\":10,\"skip\":0,\"aggregate\":[{\"field\":\"Unknown\",\"aggregate\":\"sum\"}]}";
            var syncException = Assert.Catch(() => MockData.Employees().AsQueryable().ToDataSourceResult(ParseRequest(request)));
            var asyncException = Assert.CatchAsync(() => MockData.Employees().AsQueryable().ToDataSourceResultAsync(ParseRequest(request)));

            Assert.AreEqual(syncException.GetType(), asyncException.GetType());
        }

        [Test]
        public void ToDataSourceResultAsync_Should_ThrowWhenCancelled()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.ThrowsAsync<OperationCanceledException>(() => MockData.Employees().AsQueryable().ToDataSourceResultAsync(ParseRequest(AllFeaturesRequest), cancellation.Token));
        }

        [Test]
        public void FindAsyncMethod_Should_IgnoreMismatchedSignature()
        {
            var provider = typeof(MockQueryable<Employee>);

            // MockAsyncMethods has no SumAsync over double, and its AverageAsync over double returns the wrong type.
            Assert.IsNull(QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, Expression<Func<Employee, double>>, double>>(Queryable.Sum)));
            Assert.IsNull(QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, Expression<Func<Employee, double>>, double>>(Queryable.Average)));
            VerifyAsyncMethodFound(typeof(MockAsyncMethods), "AverageAsync", QueryableExtensions.FindAsyncMethod(provider, GetOverloadMethod<Func<IQueryable<Employee>, Expression<Func<Employee, decimal?>>, decimal?>>(Queryable.Average)));
        }

        private static DataSourceRequest ParseRequest(string json) => JsonSerializer.Deserialize<DataSourceRequest>(json, CustomJsonSerializerOptions.DefaultOptions);

        private static MethodInfo GetOverloadMethod<TDelegate>(TDelegate method) where TDelegate : Delegate => method.Method;

        // Compared as JSON so the dynamic aggregate objects and nested groups are compared too
        private static void AssertSameResult<T>(DataSourceResult<T> expected, DataSourceResult<T> actual)
        {
            Assert.AreEqual(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        }

        private static void VerifyAsyncMethodFound(Type declaringType, string name, MethodInfo asyncMethod)
        {
            Assert.IsNotNull(asyncMethod);
            Assert.AreEqual(declaringType, asyncMethod.DeclaringType);
            Assert.AreEqual(name, asyncMethod.Name);
        }
    }
}
