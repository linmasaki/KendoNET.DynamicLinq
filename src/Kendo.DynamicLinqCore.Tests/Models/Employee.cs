using System;
using System.ComponentModel.DataAnnotations;

namespace Kendo.DynamicLinqCore.Tests.Models
{
    public class Employee
    {
        [Key]
        public long Number { get; set; }

        public string Name { get; set; }

        public Guid? Identification { get; set; }

        public string Introduce { get; set; }

        public DateTime Birthday {get; set;}

        public Decimal Salary {get; set;}
    }
}