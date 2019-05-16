using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace Kendo.DynamicLinqCore
{
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
        public static DataSourceResult ToDataSourceResult<T>(this IQueryable<T> queryable, int take, int skip, IEnumerable<Sort> sort, Filter filter)
        {
            return queryable.ToDataSourceResult(take, skip, sort, filter, null, null);
        }
        
        /// <summary>
        ///  Applies data processing (paging, sorting and filtering) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="request">The DataSourceRequest object containing take, skip, order, and filter data.</param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
        public static DataSourceResult ToDataSourceResult<T>(this IQueryable<T> queryable, DataSourceRequest request)
        {
            return queryable.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Filter, null, request.Group);
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
        public static DataSourceResult ToDataSourceResult<T>(this IQueryable<T> queryable, int take, int skip, IEnumerable<Sort> sort, Filter filter, IEnumerable<Aggregator> aggregates, IEnumerable<Group> group)
        {            
            var errors = new List<object>();

            // Filter the data first
            queryable = Filter(queryable, filter, errors);

            // Calculate the total number of records (needed for paging)            
            var total = queryable.Count();

            // Calculate the aggregates
            var aggregate = Aggregate(queryable, aggregates);

            if (group != null && group.Any())
            {
                //if(sort == null) sort = GetDefaultSort(queryable.ElementType, sort);
                if(sort == null) sort = new List<Sort>();
                
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
            queryable = Sort(queryable, sort);

            // Finally page the data
            if (take > 0)
            {
                queryable = Page(queryable, take, skip);
            }

            var result = new DataSourceResult
            {
                Total = total,
                Aggregates = aggregate
            };

            // Group By
            if ((group != null) && group.Any())
            {
                var groupedQuery = queryable.ToList().GroupByMany(group);
                result.Group = groupedQuery;
            }
            else
            {
                result.Data = queryable.ToList();
            }

            // Set errors if any
            if(errors.Any())
            {
                result.Errors = errors;
            }
            
            return result;
        }
        
        private static IQueryable<T> Filter<T>(IQueryable<T> queryable, Filter filter, List<object> errors)
        {
            if ((filter != null) && (filter.Logic != null))
            {
                // Pretreatment some work
                filter = PreliminaryWork(filter);
                
                // Collect a flat list of all filters
                var filters = filter.All();

                // Get all filter values as array (needed by the Where method of Dynamic Linq)
                var values = filters.Select(f => f.Value).ToArray();

                string predicate;
                try 
                {
                    // Create a predicate expression e.g. Field1 = @0 And Field2 > @1
                    predicate = filter.ToExpression(typeof(T),filters);                     
                }
                catch(Exception ex)
                {
                    errors.Add(ex.Message);
                    return queryable;
                }

                // Use the Where method of Dynamic Linq to filter the data
                queryable = queryable.Where(predicate, values);
            }

            return queryable;
        }

        private static object Aggregate<T>(IQueryable<T> queryable, IEnumerable<Aggregator> aggregates)
        {
            if (aggregates != null && aggregates.Any())
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
                        var selector = aggregate.Aggregate == "count" && (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                            ? Expression.Lambda(Expression.NotEqual(Expression.MakeMemberAccess(param, prop), Expression.Constant(null, prop.PropertyType)), param)
                            : Expression.Lambda(Expression.MakeMemberAccess(param, prop), param);
                        var mi = aggregate.MethodInfo(typeof(T));
                        if (mi == null) continue;

                        var val = queryable.Provider.Execute(Expression.Call(null, mi, aggregate.Aggregate == "count" && (Nullable.GetUnderlyingType(prop.PropertyType) == null)
                                  ? new[] { queryable.Expression }
                                  : new[] { queryable.Expression, Expression.Quote(selector) }));

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

        private static IQueryable<T> Sort<T>(IQueryable<T> queryable, IEnumerable<Sort> sort)
        {
            if (sort != null && sort.Any())
            {
                // Create ordering expression e.g. Field1 asc, Field2 desc
                var ordering = string.Join(",", sort.Select(s => s.ToExpression()));

                // Use the OrderBy method of Dynamic Linq to sort the data
                return queryable.OrderBy(ordering);
            }

            return queryable;
        }

        private static IQueryable<T> Page<T>(IQueryable<T> queryable, int take, int skip)
        {            
            return queryable.Skip(skip).Take(take); 
        }

        /// <summary>
        /// Pretreatment of specific datetime condition and disallowed value type 
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static Filter PreliminaryWork(Filter filter)
        {
            if (filter.Filters != null && filter.Logic != null)
            {
                var newFilters = new List<Filter>();
                foreach (var f in filter.Filters)
                {
                    newFilters.Add(PreliminaryWork(f));
                }

                filter.Filters = newFilters;
            }
            
            // Used when the datetime's operator value is eq and local time is 00:00:00 
            if (filter.Value is DateTime utcTime && filter.Operator == "eq")
            {
                // Copy the time from the filter
                var localTime = utcTime.ToLocalTime();
                if (localTime.Hour != 0 || localTime.Minute != 0 || localTime.Second != 0) 
                    return filter;
                
                var newFilter = new Filter { Logic = "and"};
                var filtersList = new List<Filter>
                {
                    // Instead of comparing for exact equality, we compare as greater than the start of the day...
                    new Filter
                    {
                        Field = filter.Field,
                        Filters = filter.Filters,
                        Value = new DateTime(localTime.Year, localTime.Month, localTime.Day, 0, 0, 0),  
                        Operator = "gte"
                    },
                    // ...and less than the end of that same day (we're making an additional filter here)
                    new Filter
                    {
                        Field = filter.Field,
                        Filters = filter.Filters,
                        Value = new DateTime(localTime.Year, localTime.Month, localTime.Day, 23, 59, 59),  
                        Operator = "lte"
                    }
                };

                newFilter.Filters = filtersList;
                    
                return newFilter;
            }
            
            // Convert datetime to local 
            if(filter.Value is DateTime utcTime2)
            {
                var localTime = utcTime2.ToLocalTime();
                filter.Value = new DateTime(localTime.Year, localTime.Month, localTime.Day, localTime.Hour, localTime.Minute, localTime.Second, localTime.Millisecond);
            }

            // When we have a decimal value it gets converted to double and the query will break
            if(filter.Value is Double)
            {               
                filter.Value = Convert.ToDecimal(filter.Value);
            }

            return filter;
        }
        
        /// <summary>
        /// The way this extension works it pages the records using skip and take in order to do that we need at least one sorted property.
        /// </summary>
        private static IEnumerable<Sort> GetDefaultSort(Type type, IEnumerable<Sort> sort)
        {
            if (sort == null)
            {
                var elementType = type;
                var properties = elementType.GetProperties().ToList();
                
                //by default make dir desc
                var sortByObject = new Sort
                {
                    Dir = "desc"
                };

                PropertyInfo propertyInfo;                
                //look for property that is called id
                if (properties.Any(p => p.Name.ToLower() == "id"))
                {
                    propertyInfo = properties.FirstOrDefault(p => p.Name.ToLower() == "id");
                }
                //or contains id
                else if (properties.Any(p => p.Name.ToLower().Contains("id")))
                {
                    propertyInfo = properties.FirstOrDefault(p => p.Name.ToLower().Contains("id"));
                }
                //or just get the first property
                else
                {
                    propertyInfo = properties.FirstOrDefault();
                }
                if (propertyInfo != null)
                {
                    sortByObject.Field = propertyInfo.Name;
                }
                sort = new List<Sort> { sortByObject };
            }

            return sort;
        }
    }

}
