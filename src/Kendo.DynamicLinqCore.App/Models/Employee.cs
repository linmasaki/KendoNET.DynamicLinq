using System;
using System.Runtime.Serialization;

namespace Kendo.DynamicLinqCore.App.Models
{
    [KnownType(typeof(Employee))]
    public class Employee
    {
        public int Number { get; set; }

        public Guid? Identification { get; set; }

        public string Name { get; set; }

        public string Introduce { get; set; }

        public DateTime Birthday {get; set;}

        public decimal Salary {get; set;}

        public double Height {get; set;}

        public float Weight {get; set;}
    }
}