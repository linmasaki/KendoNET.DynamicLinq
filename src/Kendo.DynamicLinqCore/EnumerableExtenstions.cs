using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Kendo.DynamicLinqCore
{
    public static class EnumerableExtenstions
    {
        public static dynamic GroupByMany<TElement>(this IEnumerable<TElement> elements, params string[] groupSelectors)
        {
            //create a new list of Kendo Group Selectors 
            var selectors = new List<GroupSelector<TElement>>(groupSelectors.Length);
            foreach (var selector in groupSelectors)
            {
                //compile the Dynamic Expression Lambda for each one
                var expression = DynamicExpressionParser.ParseLambda(false, typeof(TElement), typeof(object), selector);
                //add it to the list
                selectors.Add(new GroupSelector<TElement>
                {
                    Selector = (Func<TElement, object>)expression.Compile(),
                    Field = selector
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
                var nextSelectors = groupSelectors.Skip(1).ToArray(); //reduce the list recursively until zero
                return
                    //group by and return 
                    elements.GroupBy(selector.Selector).Select(
                                g => new GroupResult
                                {
                                    Value = g.Key,
                                    Aggregates = new List<Aggregator>
                                    {
                                        //todo fix that and make it dynamic from the request
                                        new Aggregator
                                        {
                                            Field = selector.ToString(),
                                            Aggregate = "count"
                                        }
                                    },
                                    HasSubgroups = groupSelectors.Length > 1,
                                    Count = g.Count(),
                                    Items = g.GroupByMany(nextSelectors), //recursivly group the next selectors
                                    SelectorField = selector.Field
                                });
            }
            //if there are not more group selectors return data
            return elements;
        }

    }
}
