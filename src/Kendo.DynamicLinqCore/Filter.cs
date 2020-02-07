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

            //switch(Operator)
            //{
            //    case "doesnotcontain":
            //        if(currentPropertyType == typeof(System.String))
            //            return String.Format("!{0}.{1}(@{2})", Field, comparison, index);
            //        else    
            //            return String.Format("({0} != null && !{0}.ToString().{1}(@{2}))", Field, comparison, index);  
            //    case "isnull":   
            //    case "isnotnull":   
            //        return String.Format("{0} {1} null", Field, comparison);                    
            //    case "isempty":   
            //    case "isnotempty":  
            //        if(currentPropertyType == typeof(System.String))
            //            return String.Format("{0} {1} String.Empty", Field, comparison);
            //        else
            //            throw new NotSupportedException(String.Format("Operator {0} not support non-string type", Operator));
            //    case "isnullorempty":   
            //    case "isnotnullorempty": 
            //        if(currentPropertyType == typeof(System.String))
            //            return String.Format("{0}String.IsNullOrEmpty({1})", comparison, Field);
            //        else
            //            throw new NotSupportedException(String.Format("Operator {0} not support non-string type", Operator));
            //}

            if (Operator == "doesnotcontain")
            {
                if(currentPropertyType == typeof(String))
                    return String.Format("!{0}.{1}(@{2})", Field, comparison, index);
                else    
                    return String.Format("({0} != null && !{0}.ToString().{1}(@{2}))", Field, comparison, index);        
            }           

            if (Operator == "isnull" || Operator == "isnotnull")
            {
                return String.Format("{0} {1} null", Field, comparison);
            }

            if (Operator == "isempty" || Operator == "isnotempty")
            {
                if(currentPropertyType == typeof(String))
                    return String.Format("{0} {1} String.Empty", Field, comparison);
                else
                    throw new NotSupportedException(String.Format("Operator {0} not support non-string type", Operator));
            }

            if (Operator == "isnullorempty" || Operator == "isnotnullorempty")
            {
                if(currentPropertyType == typeof(String))
                    return String.Format("{0}String.IsNullOrEmpty({1})", comparison, Field);
                else
                    throw new NotSupportedException(String.Format("Operator {0} not support non-string type", Operator));
            }

            if (comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains")
            {
                if(currentPropertyType == typeof(String))
                    return String.Format("{0}.{1}(@{2})", Field, comparison, index);
                else    
                    return String.Format("({0} != null && {0}.ToString().{1}(@{2}))", Field, comparison, index);                
            }

            return String.Format("{0} {1} @{2}", Field, comparison, index);
        }

        /// <summary>
        /// Converts the filter expression to a predicate suitable for Dynamic Linq e.g. "Field1 = @1 and Field2.Contains(@2)"
        /// </summary>
        /// <param name="filters">A list of flattened filters.</param>
        public Expression ToLambdaExpression<T>(ParameterExpression parameter, IList<Filter> filters)
        {
            if (Filters != null && Filters.Any())
            {
                Expression compositeExpression = null;
                if(Logic == "and")
                {
                    foreach (var exp in Filters.Select(filter => filter.ToLambdaExpression<T>(parameter, filters)).ToArray())
                    {
                        if(compositeExpression == null) 
                            compositeExpression = exp;
                        else 
                            compositeExpression = Expression.AndAlso(compositeExpression, exp);
                    }
                    return compositeExpression;
                }
                
                if(Logic == "or")
                {
                    foreach (var exp in Filters.Select(filter => filter.ToLambdaExpression<T>(parameter, filters)).ToArray())
                    {
                        if(compositeExpression == null) compositeExpression = exp;
                        else compositeExpression = Expression.OrElse(compositeExpression,exp);
                    }
                    return compositeExpression;
                }
            }

            var currentPropertyType = GetLastPropertyType(typeof(T), Field);
            if(currentPropertyType != typeof(String) && StringOperators.Contains(Operator))
            {
                throw new NotSupportedException(string.Format("Operator {0} not support non-string type", Operator));
            }

            var propertyChains = Field.Split('.');
            Expression left = parameter;
            foreach (var f in propertyChains)
            {
                left = Expression.PropertyOrField(left, f);
            }

            Expression resultExpression; 
            switch(Operator)
            {
                case "contains":
                case "doesnotcontain":
                    var containsMethod = typeof(String).GetMethod("Contains", new[] { typeof(String) });
                    var containsExpression = Expression.Call(left, containsMethod, Expression.Constant(Value, typeof(String)));                    
                    if(Operator == "contains") 
                        resultExpression = containsExpression;
                    else 
                        resultExpression = Expression.Not(containsExpression);
                    break;    
                    //return Operator == "contains" 
                    //       ? Expression.Lambda<Func<T, bool>>(containsExpression, parameter)
                    //       : Expression.Lambda<Func<T, bool>>(Expression.Not(containsExpression), parameter);

                case "isnull":   
                case "isnotnull":   
                    var nullCheckExpression = Expression.Equal(left, Expression.Constant(null, currentPropertyType));
                    if(Operator == "isnull") 
                        resultExpression = nullCheckExpression;
                    else 
                        resultExpression = Expression.Not(nullCheckExpression);
                    break;   

                    //return Operator == "isnull" 
                    //       ? Expression.Lambda<Func<T, bool>>(nullCheckExpression, parameter)
                    //       : Expression.Lambda<Func<T, bool>>(Expression.Not(nullCheckExpression), parameter);         

                case "isempty":   
                case "isnotempty":
                    var emptyCheckExpression = Expression.Equal(left, Expression.Constant(String.Empty, currentPropertyType));
                    if(Operator == "isempty") 
                        resultExpression = emptyCheckExpression;
                    else 
                        resultExpression = Expression.Not(emptyCheckExpression);
                    break;

                    //return Operator == "isempty" 
                    //   ? Expression.Lambda<Func<T, bool>>(emptyCheckExpression, parameter)
                    //   : Expression.Lambda<Func<T, bool>>(Expression.Not(emptyCheckExpression), parameter);
        
                case "isnullorempty":   
                case "isnotnullorempty":
                    var nullOrEmptyMethod = typeof(String).GetMethod("IsNullOrEmpty", new[] { typeof(String) });
                    var nullOrEmptyExpression = Expression.Call(left, nullOrEmptyMethod, Expression.Constant(Value, typeof(String)));
                    if(Operator == "isnullorempty") 
                        resultExpression = nullOrEmptyExpression;
                    else 
                        resultExpression = Expression.Not(nullOrEmptyExpression);
                    break; 
                    
                    //return Operator == "isnullorempty"
                    //        ? Expression.Lambda<Func<T, bool>>(nullOrEmptyExpression, parameter)
                    //        : Expression.Lambda<Func<T, bool>>(Expression.Not(nullOrEmptyExpression), parameter);

                case "startswith": 
                    var startswithMethod = typeof(String).GetMethod("StartsWith", new[] { typeof(String) });                        
                    var startswithExpression = Expression.Call(left, startswithMethod, Expression.Constant(Value, typeof(String)));
                    resultExpression = startswithExpression;
                    break; 
                    //return Expression.Lambda<Func<T, bool>>(startswithExpression, parameter);
                
                case "endswith": 
                    var endswithMethod = typeof(String).GetMethod("EndsWith", new[] { typeof(String) });                        
                    var endswithExpression = Expression.Call(left, endswithMethod, Expression.Constant(Value, typeof(String)));
                    resultExpression = endswithExpression;
                    break; 
                    //return Expression.Lambda<Func<T, bool>>(endswithExpression, parameter);

                case "eq":
                case "neq":
                    var equalCheckExpression = Expression.Equal(left, Expression.Constant(Value, currentPropertyType));
                    if(Operator == "eq") 
                        resultExpression = equalCheckExpression;
                    else 
                        resultExpression = Expression.Not(equalCheckExpression);
                    break;

                case "lt": 
                    var lessThanCheckExpression = Expression.LessThan(left, Expression.Constant(Value, currentPropertyType));
                    resultExpression = lessThanCheckExpression;
                    break;

                case "lte": 
                    var lessThanEqualCheckExpression = Expression.LessThanOrEqual(left, Expression.Constant(Value, currentPropertyType));
                    resultExpression = lessThanEqualCheckExpression;
                    break;

                case "gt":  
                    var greaterThanCheckExpression = Expression.GreaterThan(left, Expression.Constant(Value, currentPropertyType));
                    resultExpression = greaterThanCheckExpression;
                    break;

                case "gte":   
                    var greaterThanEqualCheckExpression = Expression.GreaterThanOrEqual(left, Expression.Constant(Value, currentPropertyType));
                    resultExpression = greaterThanEqualCheckExpression;
                    break;

                default:
                    throw new NotSupportedException(string.Format("Not support Operator {0}!", Operator));
            }

            return resultExpression;
        }

        private static Type GetLastPropertyType(Type type, string path)
        {
            Type currentType = type;

            foreach (string propertyName in path.Split('.'))
            {
                PropertyInfo property = currentType.GetProperty(propertyName);
                currentType = property.PropertyType;
            }

            return currentType;
        }
    }
}
