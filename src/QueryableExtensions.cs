using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;

namespace KendoNET.DynamicLinq
{
    /// <summary>
    /// Provides extension methods for asynchronously applying Kendo-style data operations (paging, sorting, filtering, grouping, and aggregation)
    /// to <see cref="IQueryable{T}"/> sources using Dynamic LINQ
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Applies data processing (paging, sorting and filtering) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="take">Specifies how many items to take. Configurable via the pageSize setting of the Kendo DataSource.</param>
        /// <param name="skip">Specifies how many items to skip.</param>
        /// <param name="sort">Specifies the current sort order.</param>
        /// <param name="filter">Specifies the current filter.</param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="AmbiguousMatchException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="TargetInvocationException"></exception>
        /// <exception cref="MethodAccessException"></exception>
        /// <exception cref="MemberAccessException"></exception>
        /// <exception cref="System.Runtime.InteropServices.InvalidComObjectException"></exception>
        /// <exception cref="System.Runtime.InteropServices.COMException"></exception>
        /// <exception cref="TypeLoadException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="TargetException"></exception>
        /// <exception cref="TargetParameterCountException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        public static DataSourceResult<T> ToDataSourceResult<T>(this IQueryable<T> queryable, int take, int skip, IEnumerable<Sort> sort, Filter filter)
        {
            return queryable.ToDataSourceResult(take, skip, sort, filter, null, null);
        }

        /// <summary>
        ///  Applies data processing (paging, sorting and filtering) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="request">The DataSourceRequest object containing take, skip, sort, filter, aggregates, and groups data.</param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="AmbiguousMatchException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="TargetInvocationException"></exception>
        /// <exception cref="MethodAccessException"></exception>
        /// <exception cref="MemberAccessException"></exception>
        /// <exception cref="System.Runtime.InteropServices.InvalidComObjectException"></exception>
        /// <exception cref="System.Runtime.InteropServices.COMException"></exception>
        /// <exception cref="TypeLoadException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="TargetException"></exception>
        /// <exception cref="TargetParameterCountException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        public static DataSourceResult<T> ToDataSourceResult<T>(this IQueryable<T> queryable, DataSourceRequest request)
        {
            return queryable.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Filter, request.Aggregate, request.Group);
        }

        /// <summary>
        /// Applies data processing (paging, sorting, filtering and aggregates) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="take">Specifies how many items to take. Configurable via the pageSize setting of the Kendo DataSource.</param>
        /// <param name="skip">Specifies how many items to skip.</param>
        /// <param name="sort">Specifies the current sort order.</param>
        /// <param name="filter">Specifies the current filter.</param>
        /// <param name="aggregates">Specifies the current aggregates.</param>
        /// <param name="group">Specifies the current groups.</param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="AmbiguousMatchException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="TargetInvocationException"></exception>
        /// <exception cref="MethodAccessException"></exception>
        /// <exception cref="MemberAccessException"></exception>
        /// <exception cref="System.Runtime.InteropServices.InvalidComObjectException"></exception>
        /// <exception cref="System.Runtime.InteropServices.COMException"></exception>
        /// <exception cref="TypeLoadException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="TargetException"></exception>
        /// <exception cref="TargetParameterCountException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        public static DataSourceResult<T> ToDataSourceResult<T>(this IQueryable<T> queryable,
            int take,
            int skip,
            IEnumerable<Sort> sort,
            Filter? filter,
            IEnumerable<Aggregator>? aggregates,
            IEnumerable<Group>? group)
        {
            var errors = new List<object>();

            // Filter the data first
            queryable = Filters(queryable, filter, errors);

            // Calculate the total number of records (needed for paging)
            var total = queryable.Count();

            // Calculate the aggregates
            var aggregate = queryable.Aggregates(aggregates);

            queryable = queryable.UpdateQuery(take, skip, sort, group);

            var result = new DataSourceResult<T>
            {
                Total = total,
                Aggregates = aggregate
            };

            // Group By
            if (group?.Any() == true)
            {
                result.Groups = queryable.GroupByMany(group);
            }
            else
            {
                result.Data = queryable.ToList();
            }

            // Set errors if any
            if (errors.Count > 0)
            {
                result.Errors = errors;
            }

            return result;
        }

        /// <summary>
        /// Updates the IQueryable with sorting and paging.
        /// </summary>
        /// <exception cref="OutOfMemoryException"></exception>
        public static IQueryable<T> UpdateQuery<T>(this IQueryable<T> queryable, int take, int skip, IEnumerable<Sort> sort, IEnumerable<Group>? group)
        {
            if (group?.Any() == true)
            {
                sort ??= [];
                foreach (var source in group.Reverse())
                {
                    sort = sort.Append(new Sort
                    {
                        Field = source.Field,
                        Dir = source.Dir
                    });
                }
            }

            // Sort the data
            queryable = queryable.Sort(sort);

            // Finally page the data
            if (take > 0)
            {
                queryable = queryable.Page(take, skip);
            }
            return queryable;
        }

        /// <summary>
        /// Set Filters for IQueryable using Dynamic Linq.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="AmbiguousMatchException"></exception>
        public static IQueryable<T> Filters<T>(this IQueryable<T> queryable, Filter? filter, List<object> errors)
        {
            if (filter?.Logic != null)
            {
                // Pretreatment some work
                filter = PreliminaryWork(typeof(T), filter);

                // Collect a flat list of all filters
                var filters = filter.All();

                /* Method.1 Use the combined expression string */
                // Step.1 Create a predicate expression e.g. Field1 = @0 And Field2 > @1
                string predicate;
                try
                {
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
            }

            return queryable;
        }

        /// <summary>
        /// Agregates the IQueryable using Dynamic Linq.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="AmbiguousMatchException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="TargetInvocationException"></exception>
        /// <exception cref="MethodAccessException"></exception>
        /// <exception cref="MemberAccessException"></exception>
        /// <exception cref="System.Runtime.InteropServices.InvalidComObjectException"></exception>
        /// <exception cref="System.Runtime.InteropServices.COMException"></exception>
        /// <exception cref="TypeLoadException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="TargetException"></exception>
        /// <exception cref="TargetParameterCountException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        public static object? Aggregates<T>(this IQueryable<T> queryable, IEnumerable<Aggregator>? aggregates)
        {
            if (aggregates?.Any() == true)
            {
                var objProps = new Dictionary<DynamicProperty, object>();
                var groups = aggregates.GroupBy(g => g.Field);
                Type? type = null;

                foreach (var group in groups)
                {
                    var fieldProps = new Dictionary<DynamicProperty, object?>();
                    foreach (var aggregate in group)
                    {
                        var prop = typeof(T).GetProperty(aggregate.Field) ?? throw new ArgumentException($"Property '{aggregate.Field}' does not exist on type '{typeof(T).Name}'.");
                        var param = Expression.Parameter(typeof(T), "s");
                        var selector = aggregate.Aggregate == "count" && (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                            ? Expression.Lambda(Expression.NotEqual(Expression.MakeMemberAccess(param, prop), Expression.Constant(null, prop.PropertyType)), param)
                            : Expression.Lambda(Expression.MakeMemberAccess(param, prop), param);
                        var mi = aggregate.MethodInfo(typeof(T));
                        if (mi == null) continue;

                        var val = queryable.Provider.Execute(Expression.Call(null, mi, aggregate.Aggregate == "count" && (Nullable.GetUnderlyingType(prop.PropertyType) == null)
                            ? (IEnumerable<Expression>)[queryable.Expression]
                            : (IEnumerable<Expression>)[queryable.Expression, Expression.Quote(selector)]));

                        fieldProps.Add(new DynamicProperty(aggregate.Aggregate, typeof(object)), val);
                    }

                    type = DynamicClassFactory.CreateType(fieldProps.Keys.ToList());
                    var fieldObj = Activator.CreateInstance(type);
                    foreach (var p in fieldProps.Keys)
                    {
                        type.GetProperty(p.Name)?.SetValue(fieldObj, fieldProps[p], null);
                    }
                    if (fieldObj == null)
                    {
                        throw new InvalidOperationException($"Failed to create instance of type '{type.Name}'.");
                    }
                    objProps.Add(new DynamicProperty(group.Key, fieldObj.GetType()), fieldObj);
                }

                type = DynamicClassFactory.CreateType(objProps.Keys.ToList());

                var obj = Activator.CreateInstance(type);
                foreach (var p in objProps.Keys)
                {
                    type.GetProperty(p.Name)?.SetValue(obj, objProps[p], null);
                }

                return obj;
            }

            return null;
        }

        /// <summary>
        /// Sorts the IQueryable using Dynamic Linq.
        /// </summary>
        /// <exception cref="OutOfMemoryException"></exception>
        public static IQueryable<T> Sort<T>(this IQueryable<T> queryable, IEnumerable<Sort> sort)
        {
            if (sort?.Any() == true)
            {
                // Create ordering expression e.g. Field1 asc, Field2 desc
                var ordering = string.Join(",", sort.Select(s => s.ToExpression()));

                // Use the OrderBy method of Dynamic Linq to sort the data
                return queryable.OrderBy(ordering);
            }

            return queryable;
        }

        /// <summary>
        /// Applies paging to the <see cref="IQueryable{T}"/> by skipping a specified number of elements and then taking a specified number of elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the source queryable.</typeparam>
        /// <param name="queryable">The source <see cref="IQueryable{T}"/> to page.</param>
        /// <param name="take">The number of elements to take (page size).</param>
        /// <param name="skip">The number of elements to skip (used for paging).</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> that contains the elements that occur after skipping <paramref name="skip"/> elements and then taking <paramref name="take"/> elements from the input sequence.
        /// </returns>
        public static IQueryable<T> Page<T>(this IQueryable<T> queryable, int take, int skip)
        {
            return queryable.Skip(skip).Take(take);
        }

        /// <summary>
        /// Pretreatment of specific DateTime type and convert some illegal value type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="filter"></param>
        /// <exception cref="AmbiguousMatchException"></exception>
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
            if ((currentPropertyType == typeof(decimal) || currentPropertyType == typeof(decimal?)) && decimal.TryParse(filter.Value.ToString(), out var number))
            {
                filter.Value = number;
                return filter;
            }

            // Convert datetime-string to DateTime
            if (currentPropertyType == typeof(DateTime) && DateTime.TryParse(filter.Value.ToString(), DateTimeFormatInfo.CurrentInfo, out var dateTime))
            {
                filter.Value = dateTime;

                // Copy the time from the filter
                var localTime = dateTime.ToLocalTime();

                // Used when the datetime's operator value is eq and local time is 00:00:00
                if (filter.Operator == "eq")
                {
                    if (localTime.Hour != 0 || localTime.Minute != 0 || localTime.Second != 0)
                        return filter;

                    var newFilter = new Filter
                    {
                        Logic = "and",
                        Filters =
                        [
                        // Instead of comparing for exact equality, we compare as greater than the start of the day...
                            new() {
                                Field = filter.Field,
                                Filters = filter.Filters??[],
                                Value = new DateTime(localTime.Year, localTime.Month, localTime.Day, 0, 0, 0,DateTimeKind.Unspecified),
                                Operator = "gte"
                            },
                        // ...and less than the end of that same day (we're making an additional filter here)
                            new() {
                                Field = filter.Field,
                                Filters = filter.Filters??[],
                                Value = new DateTime(localTime.Year, localTime.Month, localTime.Day, 23, 59, 59,DateTimeKind.Unspecified),
                                Operator = "lte"
                            }
                        ]
                    };

                    return newFilter;
                }

                // Convert datetime to local
                filter.Value = new DateTime(localTime.Year, localTime.Month, localTime.Day, localTime.Hour, localTime.Minute, localTime.Second, localTime.Millisecond, DateTimeKind.Unspecified);
            }

            return filter;
        }
    }
}