using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace KendoNET.DynamicLinq
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
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the aggregate.
        /// </summary>
        [DataMember(Name = "aggregate")]
        public string Aggregate { get; set; } = string.Empty;

        /// <summary>
        /// Get MethodInfo.
        /// </summary>
        /// <param name="type">Specifies the type of querable data.</param>
        /// <returns>A MethodInfo for field.</returns>
        /// <exception cref="AmbiguousMatchException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="MemberAccessException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="TargetException"></exception>
        /// <exception cref="TargetInvocationException"></exception>
        /// <exception cref="TargetParameterCountException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        public MethodInfo? MethodInfo(Type type)
        {
            var proptype = type.GetProperty(Field)?.PropertyType ?? throw new ArgumentException($"Property '{Field}' not found in type '{type.Name}'.");
            switch (Aggregate)
            {
                case "max":
                case "min":
                    return GetMethod(ConvertTitleCase(Aggregate), MinMaxFunc().GetMethodInfo(), 2)?.MakeGenericMethod(type, proptype);
                case "average":
                case "sum":
                    return GetMethod(ConvertTitleCase(Aggregate), GetSumAvg(GetType(), proptype).GetMethodInfo(), 1)?.MakeGenericMethod(type);
                case "count":
                    return GetMethod(ConvertTitleCase(Aggregate),
                        Nullable.GetUnderlyingType(proptype) != null ? CountNullableFunc().GetMethodInfo() : CountFunc().GetMethodInfo(), 1)?.MakeGenericMethod(type);
            }

            return null;
        }

        /// <summary>
        /// Converts the aggregate name to title case.
        /// </summary>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        private static string ConvertTitleCase(string str)
        {
            var tokens = str.Split([" "], StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                tokens[i] = $"{token.Substring(0, 1).ToUpperInvariant()}{token.Substring(1)}";
            }

            return string.Join(" ", tokens);
        }

        /// <summary>
        /// Get MethodInfo from Queryable methods.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="TargetException"></exception>
        /// <exception cref="TargetInvocationException"></exception>
        /// <exception cref="TargetParameterCountException"></exception>
        /// <exception cref="MethodAccessException"></exception>
        /// <exception cref="OverflowException"></exception>
        private static MethodInfo? GetMethod(string methodName, MethodInfo? methodTypes, int genericArgumentsCount)
        {
            if (methodTypes == null)
            {
                throw new ArgumentNullException(nameof(methodTypes), "Method types cannot be null.");
            }

            var methods = from method in typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                          let parameters = method.GetParameters()
                          let genericArguments = method.GetGenericArguments()
                          where method.Name == methodName &&
                                genericArguments.Length == genericArgumentsCount &&
                                parameters.Select(p => p.ParameterType).SequenceEqual((Type[])(methodTypes.Invoke(null, genericArguments) ?? Array.Empty<Type>()))
                          select method;
            return methods.FirstOrDefault();
        }

        private static Func<Type, Type[]> CountNullableFunc()
        {
            return CountNullableDelegate;
        }

        /// <summary>
        /// Count delegate type for nullable types.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        private static Type[] CountNullableDelegate(Type t)
        {
            return [typeof(IQueryable<>).MakeGenericType(t), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(t, typeof(bool)))];
        }

        private static Func<Type, Type[]> CountFunc()
        {
            return CountDelegate;
        }

        /// <summary>
        /// Returns the type for count delegate.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        private static Type[] CountDelegate(Type t)
        {
            return [typeof(IQueryable<>).MakeGenericType(t)];
        }

        /// <summary>
        /// Gthe Sum or Average delegate type.
        /// </summary>
        /// <exception cref="AmbiguousMatchException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="TargetException"></exception>
        /// <exception cref="TargetInvocationException"></exception>
        /// <exception cref="TargetParameterCountException"></exception>
        /// <exception cref="MethodAccessException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private static Func<Type, Type[]> GetSumAvg(Type t, Type proptype)
        {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            return (Func<Type, Type[]>)(t.GetMethod(nameof(SumAvgFunc), BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(proptype).Invoke(null, null)
                ?? throw new ArgumentException("Unable to invoke SumAvgFunc."));
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        }

        private static Func<Type, Type, Type[]> MinMaxFunc()
        {
            return MinMaxDelegate;
        }
        /// <summary>
        /// Minor Max delegate type.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        private static Type[] MinMaxDelegate(Type a, Type b)
        {
            return [typeof(IQueryable<>).MakeGenericType(a), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(a, b))];
        }

        private static Func<Type, Type[]> SumAvgFunc<TU>()
        {
            return SumAvgDelegate<TU>;
        }

        /// <summary>
        /// Sum or Average delegate type.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        private static Type[] SumAvgDelegate<TU>(Type t)
        {
            return [typeof(IQueryable<>).MakeGenericType(t), typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(t, typeof(TU)))];
        }
    }
}