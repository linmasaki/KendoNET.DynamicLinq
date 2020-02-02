using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Kendo.DynamicLinqCore.Sample
{
    class Program
    {
        private static Person[] people = new Person [] 
        { 
            new Person { 
                Identification = Guid.Parse("F057D609-F1F3-4E5C-BC09-0AC0BBE1007D"),
                Name = "Monie",
                Introduce = "I'm Monie",
                Salary = 1000,
                EmployeeNumber = 10,
                Birthday = new DateTime(2000,5,5) 
            }, 
            new Person { 
                Identification = Guid.Parse("F586A608-4095-4E8E-8F21-AEFC0DFDB61F"),
                Name = "CoCo",
                Introduce = "I'm CoCo",
                Salary = 2500,
                EmployeeNumber = 77,
                Birthday = new DateTime(1986,10,10) 
            },
            new Person { 
                Identification = Guid.Parse("F4FFE20C-4DE5-4DC5-9686-955FB74EE05E"),
                Name = "Kirin",
                Salary = 3000,
                EmployeeNumber = 66,
                Birthday = new DateTime(1984,7,8) 
            },
            new Person { 
                Identification = Guid.Parse("CCAB16DB-070B-4A93-846A-81AEEFDD42EE"),
                Name = "Rock",
                Introduce = "",
                Salary = 1750,
                EmployeeNumber = 35,
                Birthday = new DateTime(1976,11,6) 
            },
            new Person { 
                Identification = null,
                Name = "Pikachu",
                Introduce = "Pika~ Pika~",
                Salary = 6600,
                EmployeeNumber = 18,
                Birthday = new DateTime(2005,3,16) 
            }
        };

        static void Main(string[] args)
        {    
            #if NETCOREAPP1_0 || NETCOREAPP1_1
                Console.WriteLine("/---------- Net Core App 1.x ----------/");
            #else
                Console.WriteLine("/---------- Net Core App 2.x ----------/");
            #endif

            Console.WriteLine("----------------------------------------");

            /* Test 1 */
            var result = people.AsQueryable().ToDataSourceResult(1, 2, null, null, new[]
            {
                new Aggregator
                {
                    Aggregate = "sum",
                    Field = "Salary"
                },
                new Aggregator
                {
                    Aggregate = "average",
                    Field = "Salary"
                }
            }, null);

            Console.WriteLine("\r\n/********** Test 1 **********/");
            Console.WriteLine(result.Aggregates);   // { Salary = { sum = 14850, average = 2970 } }


            /* Test 2 */
            result = people.AsQueryable().ToDataSourceResult(10, 0, new [] { 
                new Sort{
                    Field ="Name",
                    Dir = "asc"
                } 
            },
            new Filter {
                Field ="Birthday",
                Value = "1985-06-28T16:00:00.000Z",
                Operator = "lte",
                Logic = "and"
            }, null, null);
            
            Console.WriteLine("\r\n/********** Test 2 **********/");
            foreach (var p in result.Data)
            {
                Console.WriteLine((p as Person).Name);  // Kirin, Rock
            }
            

            /* Test 3 */
            result = people.AsQueryable().ToDataSourceResult(10, 0, new [] 
            { 
                new Sort
                {
                    Field ="Name",
                    Dir = "asc"
                } 
            }, 
            new Filter 
            {
                Filters = new [] 
                { 
                    new Filter
                    {
                        Field ="Identification",
                        Operator = "startswith",
                        Value = "f"
                    },
                    new Filter
                    {
                        Field ="Identification",
                        Operator = "doesnotcontain",
                        Value = "4d"
                    }                    
                },
                Logic = "and"
            }, null, null);

            Console.WriteLine("\r\n/********** Test 3 **********/");
            foreach (var p in result.Data)
            {
                Console.WriteLine((p as Person).Name);  // CoCo, Monie
            }

            Console.ReadKey();
        }
    }

    [KnownType(typeof(Person))]
    public class Person
    {
        public Guid? Identification { get; set; }

        public string Name { get; set; }

        public string Introduce { get; set; }

        public int EmployeeNumber { get; set; }

        public DateTime Birthday {get; set;}

        public Decimal Salary {get; set;}
    }
}
