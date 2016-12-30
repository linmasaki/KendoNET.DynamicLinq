using System;
using System.Linq;
using System.Runtime.Serialization;
using Kendo.DynamicLinqCore;

namespace Kendo.DynamicLinq.ConsoleTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var people = new[] { new Person { Age = 50 }, new Person { Age = 20 } };
            var result = people.AsQueryable().ToDataSourceResult(1, 2, null, null, new[]
            {
                new Aggregator
                {
                    Aggregate = "sum",
                    Field = "Age"
                }
            }, null);

            Console.Write(result.Aggregates);
            Console.ReadKey();
        }
    }

    [KnownType(typeof(Person))]
    public class Person
    {
        public int Age { get; set; }
    }
}
