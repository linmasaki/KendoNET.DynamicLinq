using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Kendo.DynamicLinqCore
{
    public static class EnumerableExtensions
    {
        public static dynamic GroupByMany<TElement>(this IEnumerable<TElement> elements, IEnumerable<Group> groupSelectors)
        {
            // Create a new list of Kendo Group Selectors 
            var selectors = new List<GroupSelector<TElement>>(groupSelectors.Count());
            foreach (var selector in groupSelectors)
            {
                // Compile the Dynamic Expression Lambda for each one
                var expression = DynamicExpressionParser.ParseLambda(false, typeof(TElement), typeof(object), selector.Field);

                // Add it to the list
                selectors.Add(new GroupSelector<TElement>
                {
                    Selector = (Func<TElement, object>)expression.Compile(),
                    Field = selector.Field,
                    Aggregates = selector.Aggregates
                });
            }

            // Call the actual group by method
            return elements.GroupByMany(selectors.ToArray());
        }

        public static dynamic GroupByMany<TElement>(this IEnumerable<TElement> elements, params GroupSelector<TElement>[] groupSelectors)
        {
            if (groupSelectors.Length > 0)
            {
                // Get selector
                var selector = groupSelectors[0];
                var nextSelectors = groupSelectors.Skip(1).ToArray();   // Reduce the list recursively until zero

                // Group by and return                
                return  elements.GroupBy(selector.Selector).Select(
                            g => new GroupResult
                            {
                                Value = g.Key,
                                Aggregates = QueryableExtensions.Aggregates(g.AsQueryable(), selector.Aggregates),
                                HasSubgroups = groupSelectors.Length > 1,
                                Count = g.Count(),
                                Items = g.GroupByMany(nextSelectors),   // Recursivly group the next selectors
                                SelectorField = selector.Field
                            });
            }

            // If there are not more group selectors return data
            return elements;
        }
    }
}