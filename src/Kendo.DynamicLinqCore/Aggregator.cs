using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Kendo.DynamicLinqCore
{
    /// <summary>
    /// Represents a aggregate expression of Kendo DataSource.
    /// </summary>
    [DataContract(Name = "aggregate")]
    public class Aggregator
    {
        /// <summary>
        /// Gets or sets the name of the aggregated field (property).
        /// </summary>
        [DataMember(Name = "field")]
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the aggregate.
        /// </summary>
        [DataMember(Name = "aggregate")]
        public string Aggregate { get; set; }

        /// <summary>
        /// Get MethodInfo.
        /// </summary>
        /// <param name="type">Specifies the type of querable data.</param>
        /// <returns>A MethodInfo for field.</returns>
        public MethodInfo MethodInfo(Type type)
        {
            var proptype = type.GetProperty(Field).PropertyType;
            switch (Aggregate)
            {
                case "max":
                case "min":
                    return GetMethod(ConvertTitleCase(Aggregate), MinMaxFunc().GetMethodInfo(), 2).MakeGenericMethod(type, proptype);
                case "average":
                case "sum":
                    return GetMethod(ConvertTitleCase(Aggregate),
                           ((Func<Type, Type[]>)GetType().GetMethod("SumAvgFunc", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(proptype).Invoke(null, null))
                           .GetMethodInfo(), 1).MakeGenericMethod(type);
                case "count":
                    return GetMethod(ConvertTitleCase(Aggregate),
                           Nullable.GetUnderlyingType(proptype) != null ? CountNullableFunc().GetMethodInfo() : CountFunc().GetMethodInfo(), 1).MakeGenericMethod(type);
            }

            return null;
        }

        private static string ConvertTitleCase(string str)
        {
            var tokens = str.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                tokens[i] = token.Substring(0, 1).ToUpper() + token.Substring(1);
            }

            return string.Join(" ", tokens);
        }

        private static MethodInfo GetMethod(string methodName, MethodInfo methodTypes, int genericArgumentsCount)
        {
            var methods = from method in typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                          let parameters = method.GetParameters()
                          let genericArguments = method.GetGenericArguments()
                          where method.Name == methodName &&
                          genericArguments.Length == genericArgumentsCount &&
                          parameters.Select(p => p.ParameterType).SequenceEqual((Type[])methodTypes.Invoke(null, genericArguments))
                          select method;
            return methods.FirstOrDefault();
        }

        private static Func<Type, Type[]> CountNullableFunc()
        {
            return CountNullableDelegate;
        }

        private static Type[] CountNullableDelegate(Type t)
        {
            return new[]
            {
                typeof(IQueryable<>).MakeGenericType(t),
                typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(t, typeof(bool)))
            };
        }

        private static Func<Type, Type[]> CountFunc()
        {
            return CountDelegate;
        }

        private static Type[] CountDelegate(Type t)
        {
            return new []
            {
                typeof(IQueryable<>).MakeGenericType(t)
            };
        }

        private static Func<Type, Type, Type[]> MinMaxFunc()
        {
            return MinMaxDelegate;
        }

        private static Type[] MinMaxDelegate(Type a, Type b)
        {
            return new[]
            {
                typeof(IQueryable<>).MakeGenericType(a),
                typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(a, b))
            };
        }

        private static Func<Type, Type[]> SumAvgFunc<TU>()
        {
            return SumAvgDelegate<TU>;
        }

        private static Type[] SumAvgDelegate<TU>(Type t)
        {
            return new[]
            {
                typeof (IQueryable<>).MakeGenericType(t),
                typeof (Expression<>).MakeGenericType(typeof (Func<,>).MakeGenericType(t, typeof(TU)))
            };
        }

    }
}