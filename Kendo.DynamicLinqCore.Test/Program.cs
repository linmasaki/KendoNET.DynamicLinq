using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Kendo.DynamicLinqCore.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var peoples = new[] { new Person { Age = 50 }, new Person { Age = 20 } };
            var result = peoples.AsQueryable().ToDataSourceResult(1, 2, null, null, new[]
            {
                new Aggregator
                {
                    Aggregate = "sum",
                    Field = "Age"
                }
            }, null);

            Console.WriteLine(result.Aggregates);
            Console.ReadKey();
        }
    }

    [KnownType(typeof(Person))]
    public class Person
    {
        public int Age { get; set; }
    }
}
