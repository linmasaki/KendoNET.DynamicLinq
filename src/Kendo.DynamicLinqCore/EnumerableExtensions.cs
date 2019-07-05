using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;

namespace Kendo.DynamicLinqCore
{
    public static class EnumerableExtensions
    {
        public static dynamic GroupByMany<TElement>(this IEnumerable<TElement> elements, IEnumerable<Group> groupSelectors)
        {
            //create a new list of Kendo Group Selectors 
            var selectors = new List<GroupSelector<TElement>>(groupSelectors.Count());
            foreach (var selector in groupSelectors)
            {
                //compile the Dynamic Expression Lambda for each one
                //var expression = DynamicExpressionParser.ParseLambda(typeof(TElement), typeof(object), selector.Field);
                var expression = DynamicExpressionParser.ParseLambda(false, typeof(TElement), typeof(object), selector.Field);
                
                //add it to the list
                selectors.Add(new GroupSelector<TElement>
                {
                    Selector = (Func<TElement, object>)expression.Compile(),
                    Field = selector.Field,
                    Aggregates = selector.Aggregates
                });
            }
            
            //call the actual group by method
            return elements.GroupByMany(selectors.ToArray());
        }

        public static dynamic GroupByMany<TElement>(this IEnumerable<TElement> elements, params GroupSelector<TElement>[] groupSelectors)
        {
            if (groupSelectors.Length > 0)
            {
                //get selector
                var selector = groupSelectors.First();
                var nextSelectors = groupSelectors.Skip(1).ToArray();   //reduce the list recursively until zero

                //group by and return                
                return  elements.GroupBy(selector.Selector).Select( 
                            g => new GroupResult
                            {
                                Value = g.Key,
                                Aggregates = Aggregates(g.AsQueryable(),selector.Aggregates),
                                HasSubgroups = groupSelectors.Length > 1,
                                Count = g.Count(),                                
                                Items = g.GroupByMany(nextSelectors),   //recursivly group the next selectors
                                SelectorField = selector.Field
                            });
            }

            //if there are not more group selectors return data
            return elements;
        }

        private static object Aggregates<T>(IQueryable<T> queryable, IEnumerable<Aggregator> aggregates)
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
        
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T item)
        {
            foreach (var i in source)
            {
                yield return i;
            }

            yield return item;
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, T item)
        {
            yield return item;

            foreach (T i in source)
            {
                yield return i;
            }
        }
    }
}