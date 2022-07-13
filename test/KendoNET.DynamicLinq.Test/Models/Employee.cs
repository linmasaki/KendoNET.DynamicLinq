using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KendoNET.DynamicLinq.Test.Models
{
    public class Employee
    {
        [Key]
        public long Number { get; set; }

        public string Name { get; set; }

        public Guid? Identification { get; set; }

        public Gender Gender { get; set; }

        public string Introduce { get; set; }

        public DateTime Birthday { get; set; }

        public decimal? Salary { get; set; }

        public double Weight { get; set; }

        public Guid? CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; }
    }
}