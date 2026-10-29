using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace KendoNET.DynamicLinq
{
    public static class QueryableExtensions
    {
        private static readonly MethodInfo s_countMethod = new Func<IQueryable<object>, int>(Queryable.Count).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo s_executeMethod = typeof(IQueryProvider).GetMethods().Single(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod);
        private static readonly ConcurrentDictionary<Assembly, ILookup<string, MethodInfo>> s_assemblyAsyncMethods = new ConcurrentDictionary<Assembly, ILookup<string, MethodInfo>>();

        /// <summary>
        /// Applies data processing (paging, filtering and sorting) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="take">Specifies how many items to take. Configurable via the pageSize setting of the Kendo DataSource.</param>
        /// <param name="skip">Specifies how many items to skip.</param>
        /// <param name="filter">Specifies the current filter.</param>
        /// <param name="sorts">Specifies the current sort order.</param>
        /// <returns>A DataSourceResult&lt;T&gt; object populated from the processed IQueryable.</returns>
        public static DataSourceResult<T> ToDataSourceResult<T>(this IQueryable<T> queryable, int take, int skip, Filter filter, IEnumerable<Sort> sorts)
        {
            return queryable.ToDataSourceResult(take, skip, filter, null, null, sorts);
        }

        /// <summary>
        ///  Applies data processing (paging, sorting and filtering) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="request">The DataSourceRequest object containing take, skip, sort, filter, aggregates, and groups data.</param>
        /// <returns>A DataSourceResult&lt;T&gt; object populated from the processed IQueryable.</returns>
        public static DataSourceResult<T> ToDataSourceResult<T>(this IQueryable<T> queryable, DataSourceRequest request)
        {
            return queryable.ToDataSourceResult(request.Take, request.Skip, request.Filter, request.Group, request.Aggregate, request.Sort);
        }

        /// <summary>
        /// Applies data processing (paging, filtering, grouping, aggregates and sorting) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="take">Specifies how many items to take. Configurable via the pageSize setting of the Kendo DataSource.</param>
        /// <param name="skip">Specifies how many items to skip.</param>
        /// <param name="filter">Specifies the current filter.</param>
        /// <param name="groups">Specifies the current groups.</param>
        /// <param name="aggregates">Specifies the current aggregates.</param>
        /// <param name="sorts">Specifies the current sort order.</param>
        /// <returns>A DataSourceResult&lt;T&gt; object populated from the processed IQueryable.</returns>
        public static DataSourceResult<T> ToDataSourceResult<T>(this IQueryable<T> queryable, int take, int skip, Filter filter, IEnumerable<Group> groups, IEnumerable<Aggregator> aggregates, IEnumerable<Sort> sorts)
        {
            var errors = new List<object>();

            // Filter the data first
            queryable = Filters(queryable, filter, errors);

            // Calculate the total number of records (needed for paging)
            var total = queryable.Count();

            var aggregate = CalculateAggregatesAsync(queryable, aggregates, useAsync: false, cancellationToken: default).GetAwaiter().GetResult();

            queryable = ApplySortAndPaging(queryable, take, skip, groups, sorts);

            var result = new DataSourceResult<T>
            {
                Total = total,
                Aggregates = aggregate
            };

            // Fetch the page now, so the result stays readable after the caller disposes its DbContext
            var data = queryable.ToList();
            if (groups?.Any() == true)
            {
                result.Groups = (IEnumerable<GroupResult>)data.GroupByMany(groups);
            }
            else
            {
                result.Data = data;
            }

            if (errors.Count > 0)
            {
                result.Errors = errors;
            }

            return result;
        }

        /// <summary>
        /// Asynchronously applies data processing (paging, filtering, grouping, aggregates and sorting) over IQueryable using Dynamic Linq.
        /// Queries run through the provider's own *Async methods when it follows that naming convention, otherwise synchronously.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="take">Specifies how many items to take. Configurable via the pageSize setting of the Kendo DataSource.</param>
        /// <param name="skip">Specifies how many items to skip.</param>
        /// <param name="filter">Specifies the current filter.</param>
        /// <param name="groups">Specifies the current groups.</param>
        /// <param name="aggregates">Specifies the current aggregates.</param>
        /// <param name="sorts">Specifies the current sort order.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A DataSourceResult&lt;T&gt; object populated from the processed IQueryable.</returns>
        public static async Task<DataSourceResult<T>> ToDataSourceResultAsync<T>(this IQueryable<T> queryable, int take, int skip, Filter filter, IEnumerable<Group> groups = null, IEnumerable<Aggregator> aggregates = null, IEnumerable<Sort> sorts = null, CancellationToken cancellationToken = default)
        {
            var errors = new List<object>();

            queryable = Filters(queryable, filter, errors);

            var total = (int)await ExecuteAsync(queryable, s_countMethod.MakeGenericMethod(typeof(T)), null, useAsync: true, cancellationToken).ConfigureAwait(false);

            var aggregate = await CalculateAggregatesAsync(queryable, aggregates, useAsync: true, cancellationToken).ConfigureAwait(false);

            queryable = ApplySortAndPaging(queryable, take, skip, groups, sorts);

            // Fetch the page: the provider's ToListAsync if it has one, else IAsyncEnumerable, else synchronously
            List<T> data;
            var toListMethod = FindAsyncMethod(queryable.Provider.GetType(), new Func<IEnumerable<T>, List<T>>(Enumerable.ToList).Method);
            if (toListMethod != null)
            {
                var items = (IEnumerable<T>)await InvokeAsync(toListMethod, [queryable, cancellationToken]).ConfigureAwait(false);
                data = items as List<T> ?? items.ToList();
            }
            else if (queryable is IAsyncEnumerable<T> asyncEnumerable)
            {
                data = [];
                await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    data.Add(item);
                }
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                data = queryable.ToList();
            }

            var result = new DataSourceResult<T>
            {
                Total = total,
                Aggregates = aggregate
            };

            // Group the fetched page in memory
            if (groups?.Any() == true)
            {
                result.Groups = (IEnumerable<GroupResult>)data.GroupByMany(groups);
            }
            else
            {
                result.Data = data;
            }

            if (errors.Count > 0)
            {
                result.Errors = errors;
            }

            return result;
        }

        /// <summary>
        /// Asynchronously applies data processing (paging, sorting, filtering, grouping and aggregates) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="request">The DataSourceRequest object containing take, skip, sort, filter, aggregates, and groups data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A DataSourceResult&lt;T&gt; object populated from the processed IQueryable.</returns>
        public static Task<DataSourceResult<T>> ToDataSourceResultAsync<T>(this IQueryable<T> queryable, DataSourceRequest request, CancellationToken cancellationToken = default)
        {
            return queryable.ToDataSourceResultAsync(request.Take, request.Skip, request.Filter, request.Group, request.Aggregate, request.Sort, cancellationToken);
        }

        private static IQueryable<T> Filters<T>(IQueryable<T> queryable, Filter filter, List<object> errors)
        {
            if (filter?.Logic != null)
            {
                /* Method.1 Use the combined expression string */
                // Step.1 Create a predicate expression e.g. Field1 = @0 And Field2 > @1
                IList<Filter> filters;
                string predicate;
                try
                {
                    // Pretreatment some work
                    filter = PreliminaryWork(typeof(T), filter);

                    // Collect a flat list of all filters
                    filters = filter.All();

                    predicate = filter.ToExpression(typeof(T), filters);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                    return queryable;
                }

                // Step.2 Get all filter values as array (needed by the Where method of Dynamic Linq)
                var values = filters.Select(f => f.Value).ToArray();

                // Step.3 Use the Where method of Dynamic Linq to filter the data
                queryable = queryable.Where(predicate, values);

                /* Method.2 Use the combined lambda expression */
                // Step.1 Create a parameter "p"
                //var parameter = Expression.Parameter(typeof(T), "p");

                // Step.2 Make up expression e.g. (p.Number >= 3) AndAlso (p.Company.Name.Contains("M"))
                //Expression expression;
                //try
                //{
                //    expression = filter.ToLambdaExpression<T>(parameter, filters);
                //}
                //catch(Exception ex)
                //{
                //    errors.Add(ex.Message);
                //    return queryable;
                //}

                // Step.3 The result is e.g. p => (p.Number >= 3) AndAlso (p.Company.Name.Contains("M"))
                //var predicateExpression = Expression.Lambda<Func<T, bool>>(expression, parameter);
                //queryable = queryable.Where(predicateExpression);
            }

            return queryable;
        }

        /// <summary>
        /// Calculates the aggregate values for the specified queryable based on the provided aggregators.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the queryable.</typeparam>
        /// <param name="queryable">The queryable to calculate aggregates for.</param>
        /// <param name="aggregates">The collection of aggregators to apply.</param>
        /// <param name="useAsync">
        /// true: use the provider's *Async methods when it has them.
        /// false: run every query synchronously; the returned task is already finished, so GetAwaiter().GetResult() never blocks.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the calculated aggregate values.</returns>
        internal static async Task<object> CalculateAggregatesAsync<T>(IQueryable<T> queryable, IEnumerable<Aggregator> aggregates, bool useAsync, CancellationToken cancellationToken)
        {
            if (aggregates?.Any() == true)
            {
                var objProps = new Dictionary<DynamicProperty, object>();
                var groups = aggregates.GroupBy(g => g.Field);
                Type type = null;

                foreach (var group in groups)
                {
                    var fieldProps = new Dictionary<DynamicProperty, object>();
                    foreach (var aggregate in group)
                    {
                        var prop = typeof(T).GetProperty(aggregate.Field);
                        var param = Expression.Parameter(typeof(T), "s");

                        // What gets passed to the query:
                        //   count on a non-nullable field → nothing (count every row)
                        //   count on a nullable field     → s => s.Field != null (count only rows that have a value)
                        //   sum / average / min / max     → s => s.Field
                        var selector = aggregate.Aggregate == "count" && Nullable.GetUnderlyingType(prop.PropertyType) == null
                            ? null
                            : Expression.Lambda(aggregate.Aggregate == "count"
                                ? Expression.NotEqual(Expression.MakeMemberAccess(param, prop), Expression.Constant(null, prop.PropertyType))
                                : Expression.MakeMemberAccess(param, prop), param);
                        var mi = aggregate.MethodInfo(typeof(T));
                        if (mi == null) continue;

                        // One query at a time: most providers' contexts and sessions reject concurrent queries
                        var val = await ExecuteAsync(queryable, mi, selector, useAsync, cancellationToken).ConfigureAwait(false);
                        fieldProps.Add(new DynamicProperty(aggregate.Aggregate, typeof(object)), val);
                    }

                    type = DynamicClassFactory.CreateType(fieldProps.Keys.ToList());
                    var fieldObj = Activator.CreateInstance(type);
                    foreach (var p in fieldProps.Keys)
                    {
                        type.GetProperty(p.Name).SetValue(fieldObj, fieldProps[p], null);
                    }

                    objProps.Add(new DynamicProperty(group.Key, fieldObj.GetType()), fieldObj);
                }

                type = DynamicClassFactory.CreateType(objProps.Keys.ToList());
                var obj = Activator.CreateInstance(type);
                foreach (var p in objProps.Keys)
                {
                    type.GetProperty(p.Name).SetValue(obj, objProps[p], null);
                }

                return obj;
            }

            return null;
        }

        /// <summary>
        /// Finds the provider's async version of a synchronous method (Queryable.Count, Queryable.Sum, ..., or Enumerable.ToList).
        /// </summary>
        internal static MethodInfo FindAsyncMethod(Type providerType, MethodInfo syncMethod)
        {
            // Every public "*Async" extension method in the provider's assembly, keyed by name without "Async". Built once per assembly.
            var candidates = s_assemblyAsyncMethods.GetOrAdd(providerType.Assembly, assembly =>
            {
                Type[] types;
                try
                {
                    types = assembly.IsDynamic ? [] : assembly.GetExportedTypes();
                }
                catch (Exception)
                {
                    types = [];     // Types that fail to load mean no async support, not an error
                }

                return types.Where(t => t.IsSealed && t.IsAbstract && t.IsDefined(typeof(ExtensionAttribute), false))
                            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                            .Where(m => m.Name.EndsWith("Async", StringComparison.Ordinal) && m.IsDefined(typeof(ExtensionAttribute), false))
                            .ToLookup(m => m.Name[..^"Async".Length]);
            })[syncMethod.Name];

            // What the async version must look like:
            //   ToList                    → (IQueryable<T>, CancellationToken), returning a Task of any IEnumerable<T> (Marten returns IReadOnlyList<T>)
            //   Count, Sum, and the rest  → the same parameters plus a CancellationToken, returning a Task of the same result
            Type[] parameterTypes;
            Func<Type, bool> matchesReturnType;
            if (syncMethod.DeclaringType == typeof(Enumerable) && syncMethod.Name == nameof(Enumerable.ToList))
            {
                var elementType = syncMethod.GetGenericArguments()[0];
                var itemsType = typeof(IEnumerable<>).MakeGenericType(elementType);
                parameterTypes = [typeof(IQueryable<>).MakeGenericType(elementType), typeof(CancellationToken)];
                matchesReturnType = t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>) && itemsType.IsAssignableFrom(t.GetGenericArguments()[0]);
            }
            else
            {
                var taskType = typeof(Task<>).MakeGenericType(syncMethod.ReturnType);
                parameterTypes = [.. syncMethod.GetParameters().Select(p => p.ParameterType), typeof(CancellationToken)];
                matchesReturnType = t => t == taskType;
            }

            var typeArguments = syncMethod.GetGenericArguments();
            MethodInfo match = null;
            foreach (var candidate in candidates)
            {
                if (candidate.GetGenericArguments().Length != typeArguments.Length) continue;

                var method = candidate;
                if (method.IsGenericMethodDefinition)
                {
                    try
                    {
                        method = method.MakeGenericMethod(typeArguments);
                    }
                    catch (ArgumentException)
                    {
                        continue;   // Violates a generic constraint
                    }
                }

                if (!matchesReturnType(method.ReturnType) || !method.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes)) continue;
                if (match != null) return null;     // More than one fits: too ambiguous to pick, so run synchronously
                match = method;
            }

            return match;
        }

        // Uses the provider's matching *Async method(Count, Sum, Average, Min or Max) when useAsync is true and the provider has one; otherwise runs the query synchronously.
        private static async Task<object> ExecuteAsync(IQueryable source, MethodInfo queryableMethod, LambdaExpression selector, bool useAsync, CancellationToken cancellationToken)
        {
            var asyncMethod = useAsync ? FindAsyncMethod(source.Provider.GetType(), queryableMethod) : null;
            if (asyncMethod == null)
            {
                // Sync queries take no token, so check it here
                cancellationToken.ThrowIfCancellationRequested();

                var callExpr = Expression.Call(null, queryableMethod, selector == null ? [source.Expression] : [source.Expression, Expression.Quote(selector)]);

                // Same call Queryable.Sum and friends make. Some providers, e.g. MongoDB, throw on the non-generic Execute(Expression).
                return s_executeMethod.MakeGenericMethod(queryableMethod.ReturnType).Invoke(source.Provider, BindingFlags.DoNotWrapExceptions, null, [callExpr], null);
            }

            object[] arguments = selector == null ? [source, cancellationToken] : [source, selector, cancellationToken];
            return await InvokeAsync(asyncMethod, arguments).ConfigureAwait(false);
        }

        private static async Task<object> InvokeAsync(MethodInfo asyncMethod, object[] arguments)
        {
            var task = (Task)asyncMethod.Invoke(null, BindingFlags.DoNotWrapExceptions, null, arguments, null);
            await task.ConfigureAwait(false);
            return task.GetType().GetProperty(nameof(Task<object>.Result)).GetValue(task);
        }

        /// <summary>
        /// Group fields are appended to the sort order, since grouping needs the data sorted by them.
        /// </summary>
        private static IQueryable<T> ApplySortAndPaging<T>(IQueryable<T> queryable, int take, int skip, IEnumerable<Group> groups, IEnumerable<Sort> sorts)
        {
            if (groups?.Any() == true)
            {
                //if(sorts == null) sorts = GetDefaultSort(queryable.ElementType, sorts);
                if (sorts == null) sorts = new List<Sort>();

                foreach (var source in groups.Reverse())
                {
                    sorts = sorts.Append(new Sort
                    {
                        Field = source.Field,
                        Dir = source.Dir
                    });
                }
            }

            if (sorts?.Any() == true)
            {
                var ordering = string.Join(",", sorts.Select(s => s.ToExpression()));
                queryable = queryable.OrderBy(ordering);
            }

            if (take > 0)
            {
                queryable = queryable.Skip(skip).Take(take);
            }

            return queryable;
        }

        /// <summary>
        /// Pretreatment of specific DateTime type and convert some illegal value type
        /// </summary>
        /// <param name="filter"></param>
        private static Filter PreliminaryWork(Type type, Filter filter)
        {
            if (filter.Filters != null && filter.Logic != null)
            {
                var newFilters = new List<Filter>();
                foreach (var f in filter.Filters)
                {
                    newFilters.Add(PreliminaryWork(type, f));
                }

                filter.Filters = newFilters;
            }

            if (filter.Value == null) return filter;

            // When we have a decimal value, it gets converted to an integer/double that will result in the query break
            var currentPropertyType = Filter.GetLastPropertyType(type, filter.Field);
            if ((currentPropertyType == typeof(decimal) || currentPropertyType == typeof(decimal?)) && decimal.TryParse(filter.Value.ToString(), out decimal number))
            {
                filter.Value = number;
                return filter;
            }

            // if(currentPropertyType.GetTypeInfo().IsEnum && int.TryParse(filter.Value.ToString(), out int enumValue))
            // {
            //     filter.Value = Enum.ToObject(currentPropertyType, enumValue);
            //     return filter;
            // }

            if (currentPropertyType == typeof(DateTime) || currentPropertyType == typeof(DateTime?) || currentPropertyType == typeof(DateTimeOffset) || currentPropertyType == typeof(DateTimeOffset?))
            {
                var isDateTimeOffsetField = currentPropertyType == typeof(DateTimeOffset) || currentPropertyType == typeof(DateTimeOffset?);

                // Read the value as an instant. The server's own time zone is never consulted, so the same request gives the same answer wherever this is deployed.
                DateTimeOffset instant;
                if (filter.Value is DateTimeOffset offset)
                {
                    instant = offset;
                }
                else if (filter.Value is DateTime value)
                {
                    // A value that states no zone is taken to be UTC rather than the server's.
                    instant = new DateTimeOffset(value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime());
                }
                else if (!DateTimeOffset.TryParse(filter.Value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out instant))
                {
                    return filter;
                }

                filter.Value = isDateTimeOffsetField ? (object)instant : instant.UtcDateTime;
            }

            return filter;
        }

        /// <summary>
        /// The way this extension works it pages the records using skip and takes to do that we need at least one sort property.
        /// </summary>
        private static IEnumerable<Sort> GetDefaultSort(Type type, IEnumerable<Sort> sorts)
        {
            if (sorts == null)
            {
                var elementType = type;
                var properties = elementType.GetProperties().ToList();

                //by default make dir desc
                var sortByObject = new Sort { Dir = "desc" };

                PropertyInfo propertyInfo;
                if (properties.Any(p => string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase)))   //look for property that is called id
                {
                    propertyInfo = properties.FirstOrDefault(p => string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase));
                }
                else if (properties.Any(p => p.Name.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0))    //or contains id
                {
                    propertyInfo = properties.FirstOrDefault(p => p.Name.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0);
                }
                else    //or just get the first property
                {
                    propertyInfo = properties.FirstOrDefault();
                }

                if (propertyInfo != null)
                {
                    sortByObject.Field = propertyInfo.Name;
                }

                sorts = new List<Sort> { sortByObject };
            }

            return sorts;
        }
    }
}
