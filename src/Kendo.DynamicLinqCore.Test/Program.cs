using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Kendo.DynamicLinqCore.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var peoples = new[] 
            { 
                new Person { 
                    Name = "Monie",
                    Salary = 1000000,
                    EmployeeNumber = 10,
                    Birthday = new DateTime(2000,5,5) 
                }, 
                new Person { 
                    Name = "CoCo",
                    Salary = 2500000,
                    EmployeeNumber = 77,
                    Birthday = new DateTime(1986,10,10) 
                },
                new Person { 
                    Name = "Kirin",
                    Salary = 3000000,
                    EmployeeNumber = 66,
                    Birthday = new DateTime(1984,7,8) 
                },
            };
            
            /* Test 1 */
            var result = peoples.AsQueryable().ToDataSourceResult(1, 2, null, null, new[]
            {
                new Aggregator
                {
                    Aggregate = "sum",
                    Field = "Salary"
                }
            }, null);

            Console.WriteLine("/********** Test 1 **********/");
            Console.WriteLine(result.Aggregates);

            /* Test 2 */
            result = peoples.AsQueryable().ToDataSourceResult(10, 0, new [] { 
                new Sort{
                    Field ="Name",
                    Dir = "asc"
                } 
            }, new Filter {
                Field ="Birthday",
                Value = "1985-06-28T16:00:00.000Z",
                Operator = "gte",
                Logic = "and"
            } , null, null);

            Console.WriteLine("/********** Test 2 **********/");
            foreach (var p in result.Data)
            {
                Console.WriteLine((p as Person).Name);
            }
            
            
            Console.ReadKey();
        }
    }

    [KnownType(typeof(Person))]
    public class Person
    {
        public string Name { get; set; }

        public int EmployeeNumber { get; set; }

        public DateTime Birthday {get; set;}

        public Decimal Salary {get; set;}

    }
}
