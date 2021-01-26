using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Kendo.DynamicLinqCore.Tests.Models
{
    public class Company
    {
        [Key]
        public Guid Id { get; set; }

        public string Name { get; set; }

        public virtual ICollection<Employee> Employees { get; set; }
    }
}