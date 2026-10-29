using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace KendoNET.DynamicLinq.Test.LinqProviders
{
    /// <summary>
    /// An in-memory query that is also its own provider. Synchronous execution throws, so only the *Async methods below can run it.
    /// </summary>
    public class MockQueryable<T> : IOrderedQueryable<T>, IQueryProvider
    {
        private readonly IQueryable<T> _inner;

        public MockQueryable(IEnumerable<T> items) : this(items.AsQueryable(), new List<(string, CancellationToken)>())
        {
        }

        private MockQueryable(IQueryable<T> inner, List<(string Method, CancellationToken Token)> calls)
        {
            _inner = inner;
            Calls = calls;
        }

        /// <summary>
        /// The *Async methods called on this query and every query derived from it, in order.
        /// </summary>
        public List<(string Method, CancellationToken Token)> Calls { get; }

        internal IQueryable<T> Inner => _inner;

        public Type ElementType => typeof(T);

        public Expression Expression => _inner.Expression;

        public IQueryProvider Provider => this;

        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("Synchronous enumeration.");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new MockQueryable<TElement>(_inner.Provider.CreateQuery<TElement>(expression), Calls);

        public IQueryable CreateQuery(Expression expression)
        {
            var inner = _inner.Provider.CreateQuery(expression);
            return (IQueryable)Activator.CreateInstance(typeof(MockQueryable<>).MakeGenericType(inner.ElementType), BindingFlags.NonPublic | BindingFlags.Instance, null, [inner, Calls], null);
        }

        public TResult Execute<TResult>(Expression expression) => throw new InvalidOperationException("Synchronous execution.");

        public object Execute(Expression expression) => throw new InvalidOperationException("Synchronous execution.");
    }

    /// <summary>
    /// *Async methods following the naming convention LINQ providers use, for the types the tests aggregate.
    /// </summary>
    public static class MockAsyncMethods
    {
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
            => RunAsync(source, nameof(CountAsync), cancellationToken, q => q.Count());

        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
            => RunAsync(source, nameof(CountAsync), cancellationToken, q => q.Count(predicate));

        public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default)
            => RunAsync(source, nameof(SumAsync), cancellationToken, q => q.Sum(selector));

        public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default)
            => RunAsync(source, nameof(AverageAsync), cancellationToken, q => q.Average(selector));

        // Decoy: should return Task<double>, so FindAsyncMethod must ignore it.
        public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default)
            => RunAsync(source, nameof(AverageAsync), cancellationToken, q => (decimal)q.Average(selector));

        public static Task<TResult> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default)
            => RunAsync(source, nameof(MinAsync), cancellationToken, q => q.Min(selector));

        public static Task<TResult> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default)
            => RunAsync(source, nameof(MaxAsync), cancellationToken, q => q.Max(selector));

        public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
            => RunAsync(source, nameof(ToListAsync), cancellationToken, q => q.ToList());

        private static async Task<TResult> RunAsync<TSource, TResult>(IQueryable<TSource> source, string method, CancellationToken cancellationToken, Func<IQueryable<TSource>, TResult> run)
        {
            var query = (MockQueryable<TSource>)source;
            query.Calls.Add((method, cancellationToken));
            await Task.Yield();
            return run(query.Inner);
        }
    }
}
