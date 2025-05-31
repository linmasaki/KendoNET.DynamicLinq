using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KendoNET.DynamicLinq
{
    // The response format of the group schema : https://docs.telerik.com/kendo-ui/api/javascript/data/datasource/configuration/schema#schemagroups
    /// <summary>
    /// Represents the result of a grouped query, compatible with the Kendo UI DataSource group schema.
    /// </summary>
    [DataContract(Name = "groupresult")]
    public class GroupResult
    {
        /// <summary>
        /// Gets or sets the value of the group. This is typically the key by which the data is grouped.
        /// </summary>
        [DataMember(Name = "value")]
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the field name used for grouping.
        /// </summary>
        public string SelectorField { get; set; } = string.Empty;

        /// <summary>
        /// Gets the field name and count in the format "FieldName (Count)".
        /// Used by Kendo UI for group display.
        /// </summary>
        [DataMember(Name = "field")]
        public string Field
        {
            get { return $"{this.SelectorField} ({this.Count})"; }
        }

        /// <summary>
        /// Gets or sets the number of items in the group.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the aggregate results for the group.
        /// </summary>
        [DataMember(Name = "aggregates")]
        public object? Aggregates { get; set; }

        /// <summary>
        /// Gets or sets the subgroups or items within this group.
        /// </summary>
        [DataMember(Name = "items")]
        public IEnumerable<GroupResult>? Items { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this group contains subgroups.
        /// </summary>
        [DataMember(Name = "hasSubgroups")]
        public bool HasSubgroups { get; set; } // true if there are subgroups
    }
}