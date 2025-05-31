using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace KendoNET.DynamicLinq
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
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the filtering operator. Set to null if the Filters property is set.
        /// </summary>
        [DataMember(Name = "operator")]
        public string Operator { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the filtering value. Set to null if the Filters property is set.
        /// </summary>
        [DataMember(Name = "value")]
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the filtering logic. Can be set to "or" or "and". Set to null unless Filters is set.
        /// </summary>
        [DataMember(Name = "logic")]
        public string Logic { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the child filter expressions. Set to null if there are no child expressions.
        /// </summary>
        [DataMember(Name = "filters")]
        public IEnumerable<Filter>? Filters { get; set; }

        /// <summary>
        /// Mapping of Kendo DataSource filtering operators to Dynamic Linq
        /// </summary>
        private static readonly Dictionary<string, string> Operators = new Dictionary<string, string>
        {
            { "eq", "=" },
            { "neq", "!=" },
            { "lt", "<" },
            { "lte", "<=" },
            { "gt", ">" },
            { "gte", ">=" },
            { "startswith", "StartsWith" },
            { "endswith", "EndsWith" },
            { "contains", "Contains" },
            { "doesnotcontain", "Contains" },
            { "isnull", "=" },
            { "isnotnull", "!=" },
            { "isempty", "=" },
            { "isnotempty", "!=" },
            { "isnullorempty", "" },
            { "isnotnullorempty", "!" }
        };

        /// <summary>
        /// These operators only for string type.
        /// </summary>
        private static readonly string[] StringOperators = ["startswith", "endswith", "contains", "doesnotcontain", "isempty", "isnotempty", "isnullorempty", "isnotnullorempty"];

        /// <summary>
        /// Get a flattened list of all child filter expressions.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public IList<Filter> All()
        {
            var filters = new List<Filter>();
            Collect(filters);

            return filters;
        }
        /// <summary>
        /// Collects the filter expressions into a flat list.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
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
        /// <param name="type"></param>
        /// <param name="filters">A list of flattened filters.</param>
        /// <exception cref="OutOfMemoryException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="AmbiguousMatchException"></exception>
        public string ToExpression(Type type, IList<Filter> filters)
        {
            if (Filters?.Any() == true)
            {
                return "(" + string.Join(" " + Logic + " ", Filters.Select(filter => filter.ToExpression(type, filters)).ToArray()) + ")";
            }

            var currentPropertyType = GetLastPropertyType(type, Field);
            if (currentPropertyType != typeof(string) && StringOperators.Contains(Operator))
            {
                throw new NotSupportedException($"Operator {Operator} not support non-string type");
            }

            var index = filters.IndexOf(this);
            var comparison = Operators[Operator];

            if (Operator == "doesnotcontain")
            {
                return $"{Field} != null && !{Field}.{comparison}(@{index})";
            }

            if (Operator == "isnull" || Operator == "isnotnull")
            {
                return $"{Field} {comparison} null";
            }

            if (Operator == "isempty" || Operator == "isnotempty")
            {
                return $"{Field} {comparison} String.Empty";
            }

            if (Operator == "isnullorempty" || Operator == "isnotnullorempty")
            {
                return $"{comparison}String.IsNullOrEmpty({Field})";
            }

            if (comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains")
            {
                return $"{Field} != null && {Field}.{comparison}(@{index})";
            }

            return $"{Field} {comparison} @{index}";
        }

        /// <summary>
        /// Converts the filter to a lambda expression suitable for IQueryable e.g. "(p.Field1.Name.Contains("AnyString")) AndAlso (p.Field2 > 100)"
        /// </summary>
        /// <param name="parameter">Parameter expression</param>
        /// <param name="filters">A list of flattened filters.</param>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="AmbiguousMatchException"></exception>
        public Expression ToLambdaExpression<T>(ParameterExpression parameter, IList<Filter> filters)
        {
            if (Filters?.Any() == true)
            {
                Expression? compositeExpression = null;
                if (Logic == "and")
                {
                    foreach (var exp in Filters.Select(filter => filter.ToLambdaExpression<T>(parameter, filters)).ToArray())
                    {
                        if (compositeExpression == null) compositeExpression = exp;
                        else compositeExpression = Expression.AndAlso(compositeExpression, exp);
                    }
                }

                if (Logic == "or")
                {
                    foreach (var exp in Filters.Select(filter => filter.ToLambdaExpression<T>(parameter, filters)).ToArray())
                    {
                        if (compositeExpression == null) compositeExpression = exp;
                        else compositeExpression = Expression.OrElse(compositeExpression, exp);
                    }
                }

                return compositeExpression ?? Expression.Empty();
            }

            var currentPropertyType = GetLastPropertyType(typeof(T), Field);
            if (currentPropertyType != typeof(string) && StringOperators.Contains(Operator))
            {
                throw new NotSupportedException($"Operator {Operator} not support non-string type");
            }

            var propertyChains = Field.Split('.');
            Expression? left = null;
            foreach (var f in propertyChains)
            {
                left = Expression.PropertyOrField(parameter, f);
            }
            if (left == null)
            {
                throw new ArgumentException($"Field '{Field}' not found in type '{typeof(T).Name}'");
            }

            Expression right = Expression.Constant(Value, currentPropertyType);

            Expression resultExpression;
            switch (Operator)
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
                        var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]) ?? throw new InvalidOperationException("String.Contains method not found. Ensure the type is string or compatible.");
                        var containsExpression = Expression.Call(left, containsMethod, right);
                        if (Operator == "contains")
                            resultExpression = Expression.AndAlso(Expression.Not(nullCheckExpression), containsExpression);
                        else
                            resultExpression = Expression.AndAlso(Expression.Not(nullCheckExpression), Expression.Not(containsExpression));
                    }
                    else if (Operator == "startswith")
                    {
                        var startswithMethod = typeof(string).GetMethod("StartsWith", [typeof(string)]) ?? throw new InvalidOperationException("String.StartsWith method not found. Ensure the type is string or compatible.");
                        var startswithExpression = Expression.Call(left, startswithMethod, right);
                        resultExpression = Expression.AndAlso(Expression.Not(nullCheckExpression), startswithExpression);
                    }
                    else if (Operator == "endswith")
                    {
                        var endswithMethod = typeof(string).GetMethod("EndsWith", [typeof(string)]) ?? throw new InvalidOperationException("String.EndsWith method not found. Ensure the type is string or compatible.");
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
                    var emptyCheckExpression = Expression.Equal(left, Expression.Constant(string.Empty, currentPropertyType));
                    if (Operator == "isempty")
                        resultExpression = emptyCheckExpression;
                    else
                        resultExpression = Expression.Not(emptyCheckExpression);
                    break;

                case "isnullorempty":
                case "isnotnullorempty":
                    var nullOrEmptyMethod = typeof(string).GetMethod("IsNullOrEmpty", [typeof(string)]) ?? throw new InvalidOperationException("String.IsNullOrEmpty method not found. Ensure the type is string or compatible.");
                    var nullOrEmptyExpression = Expression.Call(left, nullOrEmptyMethod, right);
                    if (Operator == "isnullorempty")
                        resultExpression = nullOrEmptyExpression;
                    else
                        resultExpression = Expression.Not(nullOrEmptyExpression);
                    break;

                case "eq":
                case "neq":
                    var equalCheckExpression = Expression.Equal(left, right);
                    if (Operator == "eq")
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
                    throw new NotSupportedException($"Not support Operator {Operator}!");
            }

            return resultExpression;
        }
        /// <summary>
        /// GEt last property type from the path.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="AmbiguousMatchException"></exception>
        internal static Type GetLastPropertyType(Type type, string path)
        {
            var currentType = type;

            /* Searches for the public property with the specified name */
            /* Used in versions above 3.1.0 */
            foreach (var propertyName in path.Split('.'))
            {
                var property = currentType.GetProperty(propertyName) ?? throw new ArgumentException($"Property '{propertyName}' not found in type '{currentType.Name}'");
                currentType = property.PropertyType;
            }

            return currentType;
        }
    }
}