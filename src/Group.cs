using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KendoNET.DynamicLinq
{
    /// <summary>
    /// Represents a group expression of Kendo DataSource, including sorting and aggregation information.
    /// </summary>
    public class Group : Sort
    {
        /// <summary>
        /// Gets or sets the collection of aggregate expressions to be applied to the group.
        /// </summary>
        [DataMember(Name = "aggregates")]
        public IEnumerable<Aggregator> Aggregates { get; set; } = [];
    }
}
