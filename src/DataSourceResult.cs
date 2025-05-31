using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace KendoNET.DynamicLinq
{
    /// <summary>
    /// Describes the result of Kendo DataSource read operation.
    /// </summary>
    [KnownType(nameof(GetKnownTypes))]
    public class DataSourceResult<T>
    {
        /// <summary>
        /// Represents a single page of processed data.
        /// </summary>
        public IEnumerable<T> Data { get; set; } = [];

        /// <summary>
        /// Represents a single page of processed grouped data.
        /// </summary>
        public IEnumerable<GroupResult>? Groups { get; set; } = [];

        /// <summary>
        /// Represents a requested aggregates.
        /// </summary>
        public object? Aggregates { get; set; }

        /// <summary>
        /// The total number of records available.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Represents error information from server-side.
        /// </summary>
        public object? Errors { get; set; }

        /// <summary>
        /// Used by the KnownType attribute which is required for WCF serialization support
        /// </summary>
        /// <returns></returns>
        /// <exception cref="AppDomainUnloadedException"></exception>
        /// <exception cref="System.Reflection.ReflectionTypeLoadException"></exception>
        private static Type[] GetKnownTypes()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a?.FullName?.StartsWith("DynamicClasses", StringComparison.InvariantCulture) ?? false);
            return assembly == null ? [] : assembly.GetTypes().Where(t => t.Name.StartsWith("DynamicClass", StringComparison.InvariantCulture)).ToArray();
        }
    }
}