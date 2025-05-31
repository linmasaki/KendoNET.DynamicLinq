using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace KendoNET.DynamicLinq
{
    /// <summary>
    /// Provides extension methods for grouping enumerable collections by multiple selectors.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Groups the elements of a sequence according to multiple group selectors specified as <see cref="Group"/> objects.
        /// </summary>
        /// <typeparam name="TElement">The type of the elements in the source sequence.</typeparam>
        /// <param name="elements">The sequence of elements to group.</param>
        /// <param name="groupSelectors">A collection of <see cref="Group"/> objects that define the grouping fields and aggregates.</param>
        /// <returns>
        /// An <see cref="IEnumerable{GroupResult}"/> where each element represents a group and its subgroups, including aggregate results.
        /// </returns>
        /// <exception cref="OverflowException">Thrown if an arithmetic operation in the grouping or aggregation overflows.</exception>
        public static IEnumerable<GroupResult> GroupByMany<TElement>(this IEnumerable<TElement> elements, IEnumerable<Group> groupSelectors)
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

        /// <summary>
        /// Groups the elements of a sequence according to multiple group selectors specified as <see cref="GroupSelector{TElement}"/> objects.
        /// </summary>
        /// <typeparam name="TElement">The type of the elements in the source sequence.</typeparam>
        /// <param name="elements">The sequence of elements to group.</param>
        /// <param name="groupSelectors">An array of <see cref="GroupSelector{TElement}"/> objects that define the grouping selectors and aggregates.</param>
        /// <returns>
        /// An <see cref="IEnumerable{GroupResult}"/> where each element represents a group and its subgroups, including aggregate results.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the group selectors are invalid.</exception>
        /// <exception cref="OverflowException">Thrown if an arithmetic operation in the grouping or aggregation overflows.</exception>
        public static IEnumerable<GroupResult> GroupByMany<TElement>(this IEnumerable<TElement> elements, params GroupSelector<TElement>[] groupSelectors)
        {
            if (groupSelectors.Length > 0)
            {
                // Get selector
                var selector = groupSelectors[0];
                var nextSelectors = groupSelectors.Skip(1).ToArray();   // Reduce the list recursively until zero
                                                                        // Group by and return                
                return elements.GroupBy(selector.Selector).Select(
                            g => new GroupResult
                            {
                                Value = g.Key,
                                Aggregates = g.AsQueryable().Aggregates(selector.Aggregates),
                                HasSubgroups = groupSelectors.Length > 1,
                                Count = g.Count(),
                                Items = g.GroupByMany(nextSelectors),   // Recursively group the next selectors
                                SelectorField = selector.Field
                            });
            }

            // If there are not more group selectors return data
            return elements.Select(s => new GroupResult
            {
                Aggregates = elements.AsQueryable().Aggregates(null),
                Count = 1,
                HasSubgroups = false,
                SelectorField = string.Empty,
                Value = s
            });
        }
    }
}