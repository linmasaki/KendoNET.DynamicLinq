using System;
using System.Collections.Generic;

namespace KendoNET.DynamicLinq
{
    /// <summary>
    /// Represents a group selector for Kendo DataSource grouping operations.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements to group.</typeparam>
    public class GroupSelector<TElement>
    {
        /// <summary>
        /// Gets or sets the selector function used to extract the grouping key from an element.
        /// </summary>
        public Func<TElement, object> Selector { get; set; } = _ => new object();

        /// <summary>
        /// Gets or sets the name of the field to group by.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of aggregate expressions to apply to each group.
        /// </summary>
        public IEnumerable<Aggregator> Aggregates { get; set; } = [];
    }
}
