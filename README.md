#Kendo.DynamicLinqCore

# Note
Kendo.DynamicLinqCore is referred to Kendo.DynamicLinq by [kendo-labs](https://github.com/kendo-labs/dlinq-helpers). 
Related notes can refer it.

## Description
Kendo.DynamicLinqCore implements server paging, filtering, sorting and aggregating via Dynamic Linq for Net Core.

## Build NuGet package
1. Open command line console
2. Switch to project root directory.(src\Kendo.DynamicLinqCore)
3. Run "dotnet restore"
4. Run "dotnet pack --configuration release" 

## Usage
1. Add the Kendo.DynamicLinqCore NuGet package to your project.
2. Configure your Kendo DataSource to send its options as JSON.

        parameterMap: function(options, type) {
            return JSON.stringify(options);
        }
3. Configure the `schema` of the DataSource.

        schema: {
            data: "Data",
            total: "Total",
            aggregates: "Aggregates",
            groups: "Group"
        }
4. Import the Kendo.DynamicLinqCore namespace.
5. Use the `ToDataSourceResult` extension method to apply paging, sorting and filtering.

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

## Examples

The following examples use Kendo.DynamicLinq(Not Kendo.DynamicLinqCore, but similar) and you can consult.

- [ASP.NET MVC](https://github.com/telerik/kendo-examples-asp-net-mvc/tree/master/grid-crud)
- [ASP.NET Web Forms and Page Methods](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-page-methods-crud)
- [ASP.NET Web Forms and WCF](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-wcf-crud)
- [ASP.NET Web Forms and Web Services](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-web-service-crud)
- [ASP.NET Web Forms and Web API](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-webapi-crud)
