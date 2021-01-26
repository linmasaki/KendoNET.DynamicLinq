using System;
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;


namespace Kendo.DynamicLinqCore
{
    /// <summary>
    /// Describes the result of Kendo DataSource read operation. 
    /// </summary>
    [KnownType("GetKnownTypes")]
    public class DataSourceResult
    {
        /// <summary>
        /// Represents a single page of processed data.
        /// </summary>
        public IEnumerable Data { get; set; }

        /// <summary>
        /// Represents a single page of processed grouped data.
        /// </summary>
        public IEnumerable Groups { get; set; }

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
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.StartsWith("DynamicClasses"));
            return assembly == null ? new Type[0] : assembly.GetTypes().Where(t => t.Name.StartsWith("DynamicClass")).ToArray();
        }

    }
}
