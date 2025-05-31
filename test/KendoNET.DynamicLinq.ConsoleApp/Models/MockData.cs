using System;

namespace KendoNET.DynamicLinq.ConsoleApp.Models
{
    public static class MockData
    {
        public static readonly Employee[] Employees =
        [
            new Employee
            {
                Number = 10,
                Identification = Guid.Parse("F057D609-F1F3-4E5C-BC09-0AC0BBE1007D"),
                Name = "Monie",
                Introduce = "I'm Monie",
                Salary = 1000,
                Height = 165.6,
                Weight = 48.5F,
                Birthday = new DateTime(2000, 5, 5)
            },
            new Employee
            {
                Number = 77,
                Identification = Guid.Parse("F586A608-4095-4E8E-8F21-AEFC0DFDB61F"),
                Name = "CoCo",
                Introduce = "I'm CoCo",
                Salary = 2500,
                Height = 180.2,
                Weight = 69.6F,
                Birthday = new DateTime(1986, 10, 10)
            },
            new Employee
            {
                Number = 66,
                Identification = Guid.Parse("F4FFE20C-4DE5-4DC5-9686-955FB74EE05E"),
                Name = "Kirin",
                Salary = 3000,
                Height = 174.9,
                Weight = 73.8F,
                Birthday = new DateTime(1984, 7, 8)
            },
            new Employee
            {
                Number = 35,
                Identification = Guid.Parse("CCAB16DB-070B-4A93-846A-81AEEFDD42EE"),
                Name = "Rock",
                Introduce = "I'm Rock",
                Salary = 1750,
                Weight = 82.8F,
                Height = 185,
                Birthday = new DateTime(1976, 11, 6)
            },
            new Employee
            {
                Number = 18,
                Identification = null,
                Name = "Pikachu",
                Introduce = "Pika~ Pika~ Pika~",
                Salary = 6600,
                Weight = 52.9F,
                Height = 40,
                Birthday = new DateTime(2005, 3, 16, 16, 0, 0)
            },
            new Employee
            {
                Number = 150,
                Identification = null,
                Name = "Memtwo",
                Introduce = "Strike!!!",
                Salary = 9900,
                Height = 200.66,
                Weight = 99.8F,
                Birthday = new DateTime(2005, 3, 16, 8, 0, 0)
            }
        ];
    }
}