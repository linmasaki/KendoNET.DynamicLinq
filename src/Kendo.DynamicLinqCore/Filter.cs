using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        /// Gets or sets the name of the sorted field (property). Set to null if the Filters property is set.
        /// </summary>
        [DataMember(Name = "field")]
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the filtering operator. Set to null if the Filters property is set.
        /// </summary>
        [DataMember(Name = "operator")]
        public string Operator { get; set; }

        /// <summary>
        /// Gets or sets the filtering value. Set to null if the Filters property is set.
        /// </summary>
        [DataMember(Name = "value")]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the filtering logic. Can be set to "or" or "and". Set to null unless Filters is set.
        /// </summary>
        [DataMember(Name = "logic")]
        public string Logic { get; set; }

        /// <summary>
        /// Gets or sets the child filter expressions. Set to null if there are no child expressions.
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
        /// These operators only for string type.
        /// </summary>
        private static readonly string[] StringOperators = new []
        {
            "startswith",
            "endswith",
            "contains",
            "doesnotcontain",
            "isempty",
            "isnotempty",
            "isnullorempty",
            "isnotnullorempty"
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
            if (Filters?.Any() == true)
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
            if (Filters?.Any() == true)
            {
                return "(" + String.Join(" " + Logic + " ", Filters.Select(filter => filter.ToExpression(type,filters)).ToArray()) + ")";
            }

            var currentPropertyType = GetLastPropertyType(type, Field);
            if(currentPropertyType != typeof(String) && StringOperators.Contains(Operator))
            {
                throw new NotSupportedException(string.Format("Operator {0} not support non-string type", Operator));
            }

            int index = filters.IndexOf(this);
            var comparison = Operators[Operator];

            //switch(Operator)
            //{
            //    case "doesnotcontain":
            //        return String.Format("{0} != null && !{0}.{1}(@{2})", Field, comparison, index);
            //    case "isnull":   
            //    case "isnotnull":   
            //        return String.Format("{0} {1} null", Field, comparison);                    
            //    case "isempty":   
            //    case "isnotempty":  
            //        return String.Format("{0} {1} String.Empty", Field, comparison);
            //    case "isnullorempty":   
            //    case "isnotnullorempty": 
            //        return String.Format("{0}String.IsNullOrEmpty({1})", comparison, Field);
            //}

            if (Operator == "doesnotcontain")
            {
                return String.Format("{0} != null && !{0}.{1}(@{2})", Field, comparison, index);
            }

            if (Operator == "isnull" || Operator == "isnotnull")
            {
                return String.Format("{0} {1} null", Field, comparison);
            }

            if (Operator == "isempty" || Operator == "isnotempty")
            {
                return String.Format("{0} {1} String.Empty", Field, comparison);
            }

            if (Operator == "isnullorempty" || Operator == "isnotnullorempty")
            {
                return String.Format("{0}String.IsNullOrEmpty({1})", comparison, Field);
            }

            if (comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains")
            {
                return String.Format("{0} != null && {0}.{1}(@{2})", Field, comparison, index);
            }

            return String.Format("{0} {1} @{2}", Field, comparison, index);
        }

        /// <summary>
        /// Converts the filter to a lambda expression suitable for IQueryable e.g. "(p.Field1.Name.Contains("AnyString")) AndAlso (p.Field2 > 100)"
        /// </summary>
        /// <param name="parameter">Parameter expression</param>
        /// <param name="filters">A list of flattened filters.</param>
        public Expression ToLambdaExpression<T>(ParameterExpression parameter, IList<Filter> filters)
        {
            if (Filters?.Any() == true)
            {
                Expression compositeExpression = null;
                if(Logic == "and")
                {
                    foreach (var exp in Filters.Select(filter => filter.ToLambdaExpression<T>(parameter, filters)).ToArray())
                    {
                        if(compositeExpression == null) compositeExpression = exp;
                        else compositeExpression = Expression.AndAlso(compositeExpression, exp);
                    }
                }

                if(Logic == "or")
                {
                    foreach (var exp in Filters.Select(filter => filter.ToLambdaExpression<T>(parameter, filters)).ToArray())
                    {
                        if(compositeExpression == null) compositeExpression = exp;
                        else compositeExpression = Expression.OrElse(compositeExpression,exp);
                    }
                }

                return compositeExpression;
            }

            var currentPropertyType = GetLastPropertyType(typeof(T), Field);
            if(currentPropertyType != typeof(String) && StringOperators.Contains(Operator))
            {
                throw new NotSupportedException(string.Format("Operator {0} not support non-string type", Operator));
            }

            var propertyChains = Field.Split('.');
            Expression left = null;
            foreach (var f in propertyChains)
            {
                if(left == null) Expression.PropertyOrField(parameter, f);
                else Expression.PropertyOrField(left, f);
            }
            Expression right = Expression.Constant(Value, currentPropertyType);

            Expression resultExpression;
            switch(Operator)
            {
                case "contains":
                case "doesnotcontain":
                case "startswith":
                case "endswith":
                case "isnull":
                case "isnotnull":
                    var nullCheckExpression = Expression.Equal(left, Expression.Constant(null, currentPropertyType));

                    if (Operator == "contains" || Operator == "doesnotcontain")
                    {
                        var containsMethod = typeof(String).GetMethod("Contains", new[] { typeof(String) });
                        var containsExpression = Expression.Call(left, containsMethod, right);
                        if (Operator == "contains")
                            resultExpression = Expression.AndAlso(Expression.Not(nullCheckExpression), containsExpression);
                        else
                            resultExpression = Expression.AndAlso(Expression.Not(nullCheckExpression), Expression.Not(containsExpression));
                    }
                    else if (Operator == "startswith")
                    {
                        var startswithMethod = typeof(String).GetMethod("StartsWith", new[] { typeof(String) });
                        var startswithExpression = Expression.Call(left, startswithMethod, right);
                        resultExpression = Expression.AndAlso(Expression.Not(nullCheckExpression), startswithExpression);
                    }
                    else if (Operator == "endswith")
                    {
                        var endswithMethod = typeof(String).GetMethod("EndsWith", new[] { typeof(String) });
                        var endswithExpression = Expression.Call(left, endswithMethod, right);
                        resultExpression = Expression.AndAlso(Expression.Not(nullCheckExpression), endswithExpression);
                    }
                    else if (Operator == "isnull")
                    {
                        resultExpression = nullCheckExpression;
                    }
                    else // Operator == "isnotnull"
                    {
                        resultExpression = Expression.Not(nullCheckExpression);
                    }

                    break;

                case "isempty":
                case "isnotempty":
                    var emptyCheckExpression = Expression.Equal(left, Expression.Constant(String.Empty, currentPropertyType));
                    if(Operator == "isempty")
                        resultExpression = emptyCheckExpression;
                    else
                        resultExpression = Expression.Not(emptyCheckExpression);
                    break;

                case "isnullorempty":
                case "isnotnullorempty":
                    var nullOrEmptyMethod = typeof(String).GetMethod("IsNullOrEmpty", new[] { typeof(String) });
                    var nullOrEmptyExpression = Expression.Call(left, nullOrEmptyMethod, right);
                    if(Operator == "isnullorempty")
                        resultExpression = nullOrEmptyExpression;
                    else
                        resultExpression = Expression.Not(nullOrEmptyExpression);
                    break;

                case "eq":
                case "neq":
                    var equalCheckExpression = Expression.Equal(left, right);
                    if(Operator == "eq")
                        resultExpression = equalCheckExpression;
                    else
                        resultExpression = Expression.Not(equalCheckExpression);
                    break;

                case "lt":
                    resultExpression = Expression.LessThan(left, right);
                    break;

                case "lte":
                    resultExpression = Expression.LessThanOrEqual(left, right);
                    break;

                case "gt":
                    resultExpression = Expression.GreaterThan(left, right);
                    break;

                case "gte":
                    resultExpression = Expression.GreaterThanOrEqual(left, right);
                    break;

                default:
                    throw new NotSupportedException(string.Format("Not support Operator {0}!", Operator));
            }

            return resultExpression;
        }

        internal static Type GetLastPropertyType(Type type, string path)
        {
            Type currentType = type;

            /* Searches for the public property with the specified name */
            /* Used in versions above 3.1.0 */
            foreach (string propertyName in path.Split('.'))
            {
                PropertyInfo property = currentType.GetProperty(propertyName);
                currentType = property.PropertyType;
            }

            /* Retrieves all properties defined on the specified type, including inherited, non-public, instance, and static properties */
            /* Used in versions under 2.2.2 */
            //foreach (string propertyName in path.Split('.'))
            //{
            //    var typeProperties = currentType.GetRuntimeProperties();  
            //    currentType = typeProperties.FirstOrDefault(f => f.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))?.PropertyType;
            //}

            return currentType;
        }
    }
}
