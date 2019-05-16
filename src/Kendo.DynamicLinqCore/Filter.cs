using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Kendo.DynamicLinqCore
{
    /// <summary>
    /// Represents a filter expression of Kendo DataSource.
    /// </summary>
    [DataContract]
    public class Filter
    {
        /// <summary>
        /// Gets or sets the name of the sorted field (property). Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        [DataMember(Name = "field")]
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the filtering operator. Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        [DataMember(Name = "operator")]
        public string Operator { get; set; }

        /// <summary>
        /// Gets or sets the filtering value. Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        [DataMember(Name = "value")]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the filtering logic. Can be set to "or" or "and". Set to <c>null</c> unless <c>Filters</c> is set.
        /// </summary>
        [DataMember(Name = "logic")]
        public string Logic { get; set; }

        /// <summary>
        /// Gets or sets the child filter expressions. Set to <c>null</c> if there are no child expressions.
        /// </summary>
        [DataMember(Name = "filters")]
        public IEnumerable<Filter> Filters { get; set; }

        /// <summary>
        /// Mapping of Kendo DataSource filtering operators to Dynamic Linq
        /// </summary>
        private static readonly IDictionary<string, string> Operators = new Dictionary<string, string>
        {
            {"eq", "="},
            {"neq", "!="},
            {"lt", "<"},
            {"lte", "<="},
            {"gt", ">"},
            {"gte", ">="},
            {"startswith", "StartsWith"},
            {"endswith", "EndsWith"},
            {"contains", "Contains"},
            {"doesnotcontain", "Contains"},
            {"isnull", "="},
            {"isnotnull", "!="},
            {"isempty", "="},
            {"isnotempty", "!="},
            {"isnullorempty", ""},
            {"isnotnullorempty", "!"}
        };

        /// <summary>
        /// Get a flattened list of all child filter expressions.
        /// </summary>
        public IList<Filter> All()
        {
            var filters = new List<Filter>();
            Collect(filters);
            
            return filters;
        }

        private void Collect(IList<Filter> filters)
        {
            if (Filters != null && Filters.Any())
            {
                foreach (var filter in Filters)
                {
                    filter.Collect(filters);
                }
            }
            else
            {
                filters.Add(this);
            }
        }

        /// <summary>
        /// Converts the filter expression to a predicate suitable for Dynamic Linq e.g. "Field1 = @1 and Field2.Contains(@2)"
        /// </summary>
        /// <param name="filters">A list of flattened filters.</param>
        public string ToExpression(Type type, IList<Filter> filters)
        {
            if (Filters != null && Filters.Any())
            {
                return "(" + String.Join(" " + Logic + " ", Filters.Select(filter => filter.ToExpression(type,filters)).ToArray()) + ")";
            }

            int index = filters.IndexOf(this);
            var comparison = Operators[Operator];

            var typeProperties = type.GetRuntimeProperties();
            var currentPropertyType = typeProperties.FirstOrDefault(f=>f.Name.Equals(Field,StringComparison.OrdinalIgnoreCase))?.PropertyType;

            if (Operator == "doesnotcontain")
            {
                if(currentPropertyType == typeof(System.String))
                    return String.Format("!{0}.{1}(@{2})", Field, comparison, index);
                else    
                    return String.Format("({0} != null && !{0}.ToString().{1}(@{2}))", Field, comparison, index);        
            }

            if (comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains")
            {
                if(currentPropertyType == typeof(System.String))
                    return String.Format("{0}.{1}(@{2})", Field, comparison, index);
                else    
                    return String.Format("({0} != null && {0}.ToString().{1}(@{2}))", Field, comparison, index);                
            }

            if (Operator == "isnull" || Operator == "isnotnull")
            {
                return String.Format("{0} {1} null", Field, comparison);
            }

            if (Operator == "isempty" || Operator == "isnotempty")
            {
                if(currentPropertyType == typeof(System.String))
                    return String.Format("{0} {1} String.Empty", Field, comparison);
                else
                    throw new NotSupportedException(String.Format("Operator {0} not support non-string type", Operator));
            }

            if (Operator == "isnullorempty" || Operator == "isnotnullorempty")
            {
                if(currentPropertyType == typeof(System.String))
                    return String.Format("{0}String.IsNullOrEmpty({1})", comparison, Field);
                else
                    throw new NotSupportedException(String.Format("Operator {0} not support non-string type", Operator));
            }

            return String.Format("{0} {1} @{2}", Field, comparison, index);
        }
    }
}
