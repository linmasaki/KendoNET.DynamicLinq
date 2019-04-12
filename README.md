# Kendo.DynamicLinqCore

[![Version](https://img.shields.io/nuget/vpre/Kendo.DynamicLinqCore.svg)](https://www.nuget.org/packages/Kendo.DynamicLinqCore)
[![Downloads](https://img.shields.io/nuget/dt/Kendo.DynamicLinqCore.svg)](https://www.nuget.org/packages/Kendo.DynamicLinqCore)

## Description
Kendo.DynamicLinqCore implements server paging, filtering, sorting and aggregating to Kendo UI via Dynamic Linq for .Net Core App(1.x ~ 2.x).

## Usage
1. Add the Kendo.DynamicLinqCore NuGet package to your project.
2. Configure your Kendo DataSource to send its options as JSON.

```javascript
parameterMap: function(options, type) {
    return JSON.stringify(options);
}
```       
3. Configure the `schema` of the dataSource.

```javascript
schema: {
    data: "Data",
    total: "Total",
    aggregates: "Aggregates",
    groups: "Group"
}
```  
4. The completed code like below.

```javascript
..... Other kendo grid code .....

dataSource: {
    schema:
    {
        data: "Data",
        total: "Total",
        aggregates: "Aggregates",
        groups: "Group",
        ...
    },
    transport: {
        read: {
            url: 'your read url',
            dataType: 'json',
            contentType: 'application/json; charset=utf-8',
            type: 'POST'
        },
        create: {
            url: 'your create url',
            dataType: "json",
            contentType: 'application/json; charset=utf-8',
            type: 'POST'
        },
        parameterMap: function (data, operation) {
            return JSON.stringify(data);
        }
    },
    pageSize: 20,
    serverPaging: true,
    serverFiltering: true,
    serverSorting: true,
    ...
}

..... Other kendo grid code .....
```
5. Import the Kendo.DynamicLinqCore namespace.
6. Use the `ToDataSourceResult` extension method to apply paging, sorting and filtering.
```c#
using Kendo.DynamicLinqCore

[WebMethod]
public static DataSourceResult Products(int take, int skip, IEnumerable<Sort> sort, Filter filter, IEnumerable<Aggregator> aggregates, IEnumerable<Sort> group)
{
    using (var northwind = new Northwind())
    {
        return northwind.Products
            .OrderBy(p => p.ProductID) // EF requires ordering for paging                    
            .Select(p => new ProductViewModel // Use a view model to avoid serializing internal Entity Framework properties as JSON
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                UnitPrice = p.UnitPrice,
                UnitsInStock = p.UnitsInStock,
                Discontinued = p.Discontinued
            })
           .ToDataSourceResult(take, skip, sort, filter, aggregates, group);
    }
}
```

## Known Issues
When server side filterable options is enabled and apply a query with filter condition that contains datetime type column, then EntityFramework Core would throw a exception  `System.Data.SqlClient.SqlException (0x80131904): Conversion failed when converting date and/or time from character string`. The error is caused by a known issue in some old EntityFramework Core versions. The workaround is adding `datetime` value to related column in DbContext. e.g.

```c#
public class MyContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ..........

        modelBuilder.Entity<Member>().Property(x => x.UpdateTime).HasColumnType("datetime");

        ..........
    }
}
```

## How To Build NuGet Package
1. Open command line console
2. Switch to project root directory.(src\Kendo.DynamicLinqCore)
3. Run "dotnet restore"
4. Run "dotnet pack --configuration release"

## Note
Kendo.DynamicLinqCore is referred to Kendo.DynamicLinq by [kendo-labs](https://github.com/kendo-labs/dlinq-helpers). Related notes can refer it.

## Examples
The following examples use Kendo.DynamicLinq(Not Kendo.DynamicLinqCore, but similar) and you can consult.

- [ASP.NET MVC](https://github.com/telerik/kendo-examples-asp-net-mvc/tree/master/grid-crud)
- [ASP.NET Web Forms and Page Methods](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-page-methods-crud)
- [ASP.NET Web Forms and WCF](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-wcf-crud)
- [ASP.NET Web Forms and Web Services](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-web-service-crud)
- [ASP.NET Web Forms and Web API](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-webapi-crud)

More Kendo UI Grid configuration can refer to [here](https://demos.telerik.com/kendo-ui/) 


