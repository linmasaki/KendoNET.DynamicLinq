using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace KendoNET.DynamicLinq.EFCore
{
    /// <summary>
    /// Provides extension methods for asynchronously applying Kendo-style data operations (paging, sorting, filtering, grouping, and aggregation)
    /// to <see cref="IQueryable{T}"/> sources using Dynamic LINQ and Entity Framework Core.
    /// </summary>
    public static class QueryableAsyncExtensionsAsync
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
        /// <param name="ct"></param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
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
        /// <exception cref="OperationCanceledException"></exception>
        public static Task<DataSourceResult<T>> ToDataSourceResultAsync<T>(this IQueryable<T> queryable, int take, int skip, IEnumerable<Sort> sort, Filter filter, CancellationToken ct)
        {
            return queryable.ToDataSourceResultAsync(take, skip, sort, filter, null, null, ct);
        }

        /// <summary>
        ///  Applies data processing (paging, sorting and filtering) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="request">The DataSourceRequest object containing take, skip, sort, filter, aggregates, and groups data.</param>
        /// <param name="ct"></param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
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
        /// <exception cref="OperationCanceledException"></exception>
        public static Task<DataSourceResult<T>> ToDataSourceResultAsync<T>(this IQueryable<T> queryable, DataSourceRequest request, CancellationToken ct)
        {
            return queryable.ToDataSourceResultAsync(request.Take, request.Skip, request.Sort, request.Filter, request.Aggregate, request.Group, ct);
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
        /// <param name="ct"></param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
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
        /// <exception cref="OperationCanceledException"></exception>
        public static async Task<DataSourceResult<T>> ToDataSourceResultAsync<T>(this IQueryable<T> queryable,
            int take,
            int skip,
            IEnumerable<Sort> sort,
            Filter? filter,
            IEnumerable<Aggregator>? aggregates,
            IEnumerable<Group>? group,
            CancellationToken ct)
        {
            var errors = new List<object>();

            // Filter the data first
            queryable = queryable.Filters(filter, errors);

            // Calculate the total number of records (needed for paging)
            var total = await queryable.CountAsync(ct);

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
                result.Groups = await queryable.GroupByMany(group).AsQueryable().ToListAsync(ct);
            }
            else
            {
                result.Data = await queryable.ToListAsync(ct);
            }

            // Set errors if any
            if (errors.Count > 0)
            {
                result.Errors = errors;
            }

            return result;
        }
    }
}
