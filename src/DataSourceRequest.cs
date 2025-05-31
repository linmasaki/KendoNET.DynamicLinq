using System.Collections.Generic;

namespace KendoNET.DynamicLinq
{
    /// <summary>
    /// Represents a request for data operations such as paging, sorting, filtering, grouping, and aggregation.
    /// Used by Kendo UI DataSource to describe the desired data manipulation.
    /// </summary>
    public class DataSourceRequest
    {
        /// <summary>
        /// Gets or sets the number of items to take (page size).
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// Gets or sets the number of items to skip (used for paging).
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// Gets or sets the collection of sort expressions that define the requested sort order.
        /// </summary>
        public IEnumerable<Sort> Sort { get; set; } = [];

        /// <summary>
        /// Gets or sets the filter expression that defines the requested filtering.
        /// </summary>
        public Filter? Filter { get; set; }

        /// <summary>
        /// Gets or sets the collection of group expressions that define the requested grouping.
        /// </summary>
        public IEnumerable<Group>? Group { get; set; }

        /// <summary>
        /// Gets or sets the collection of aggregate expressions that define the requested aggregations.
        /// </summary>
        public IEnumerable<Aggregator>? Aggregate { get; set; }
    }
}