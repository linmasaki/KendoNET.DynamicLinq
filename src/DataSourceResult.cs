using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace KendoNET.DynamicLinq
{
    /// <summary>
    /// Describes the result of Kendo DataSource read operation.
    /// </summary>
    [KnownType("GetKnownTypes")]
    public class DataSourceResult<T>
    {
        /// <summary>
        /// Represents a single page of processed data.
        /// </summary>
        public IEnumerable<T> Data { get; set; }

        /// <summary>
        /// Represents a single page of processed grouped data.
        /// </summary>
        public IEnumerable<GroupResult> Groups { get; set; }

        /// <summary>
        /// Represents a requested aggregates.
        /// </summary>
        public object Aggregates { get; set; }

        /// <summary>
        /// The total number of records available.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Represents error information from server-side.
        /// </summary>
        public object Errors { get; set; }

        /// <summary>
        /// Used by the KnownType attribute which is required for WCF serialization support
        /// </summary>
        /// <returns></returns>
        private static Type[] GetKnownTypes()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "System.Linq.Dynamic.Core.DynamicClasses");
            return assembly == null ? new Type[0] : assembly.GetTypes();
        }
    }
}
