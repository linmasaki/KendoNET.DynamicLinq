# KendoNET.DynamicLinq

[![Version](https://img.shields.io/nuget/vpre/KendoNET.DynamicLinq.svg)](https://www.nuget.org/packages/KendoNET.DynamicLinq)
[![Downloads](https://img.shields.io/nuget/dt/KendoNET.DynamicLinq.svg)](https://www.nuget.org/packages/KendoNET.DynamicLinq)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](#)

## Description

KendoNET.DynamicLinq implements server paging, filtering, sorting, grouping, and aggregating to Kendo UI via Dynamic Linq for .NET 10.

## Prerequisites

- .NET 10. Consumers on .NET Standard, .NET Framework, .NET Core 1.x ~ 3.x, or .NET 5 ~ 9 should use the 3.x releases instead.
- You must add custom `ObjectToInferredTypesConverter` to your `JsonSerializerOptions` since `System.Text.Json` doesn't deserialize inferred types to object properties, see
  the [sample code](https://github.com/linmasaki/KendoNET.DynamicLinq/blob/main/test/KendoNET.DynamicLinq.Test/CustomJsonSerializerOptions.cs)
  and [reference](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to#deserialize-inferred-types-to-object-properties).

## Usage

1. Add the KendoNET.DynamicLinq NuGet package to your project.
2. Configure your Kendo DataSource to send its options as JSON.

```javascript
parameterMap: function(options, type) {
    return kendo.stringify(options);
}
```

3. Configure the `schema` of the dataSource.

```javascript
schema: {
    data: "Data",
    total: "Total",
    aggregates: "Aggregates",
    groups: "Groups",
    errors: "Errors"
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
        groups: "Groups",
        errors: "Errors",
        ...
    },
    transport: {
        read: {
            url: 'read url',
            dataType: 'json',
            contentType: 'application/json; charset=utf-8',
            type: 'POST'
        },
        create: {
            url: 'create url',
            dataType: "json",
            contentType: 'application/json; charset=utf-8',
            type: 'POST'
        },
        parameterMap: function (data, operation) {
            return kendo.stringify(data);
        }
    },
    error: function(e) {
        console.log(e.errors); // Your error information
        e.sender.cancelChanges();
    },
    pageSize: 20,
    serverPaging: true,
    serverFiltering: true,
    serverSorting: true,
    ...
}

..... Other kendo grid code .....
```

5. Import the KendoNET.DynamicLinq namespace.
6. Use the `ToDataSourceResult` extension method to apply paging, sorting, filtering, grouping and aggregating.

```c#
using KendoNET.DynamicLinq

[WebMethod]
public static DataSourceResult Products(int take, int skip, IEnumerable<Sort> sort, Filter filter, IEnumerable<Aggregator> aggregates, IEnumerable<Group> groups)
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
               .ToDataSourceResult(take, skip, sort, filter, aggregates, groups);
    }
}
```

or from Kendo UI request

```c#
using KendoNET.DynamicLinq

[HttpPost]
public IActionResult Products([FromBody] DataSourceRequest requestModel)
{
    using (var northwind = new Northwind())
    {
        return northwind.Products
               .Select(p => new ProductViewModel // Use a view model to avoid serializing internal Entity Framework properties as JSON
               {
                   ProductID = p.ProductID,
                   ProductName = p.ProductName,
                   UnitPrice = p.UnitPrice,
                   UnitsInStock = p.UnitsInStock,
                   Discontinued = p.Discontinued
               })
               .ToDataSourceResult(requestModel.Take, requestModel.Skip, requestModel.Sort, requestModel.Filter, requestModel.Aggregate, requestModel.Group);
    }
}
```

## Date and Time Handling

Filter values are read as instants in UTC. The server's own time zone is never consulted, so the same request returns the same rows wherever the application is deployed.

| Value in the request        | Read as                                          |
|-----------------------------|--------------------------------------------------|
| `2025-11-29T16:00:00.000Z`  | `2025-11-29 16:00` UTC                           |
| `2025-11-30T00:00:00+08:00` | `2025-11-29 16:00` UTC                           |
| `2025-11-29`                | `2025-11-29 00:00` UTC, since no zone was stated |

`DateTime`, `DateTime?`, `DateTimeOffset` and `DateTimeOffset?` columns are all supported.

Store UTC in the database and let the client convert for display. The Kendo DataSource already sends UTC, since `JSON.stringify` serializes a JavaScript `Date` to a UTC ISO string.

### `eq` matches an exact instant

An `eq` filter matches only the instant supplied as the filter value. To match everything recorded on one day, filter with a `gte`/`lt` range instead. There are two ways to produce that range:

1. **Let the user set it.** The Grid's filter menu offers a second condition by default, so a user can choose the start and end of the day themselves. No code required.

2. **Expand a single date before the request is sent.** The server cannot do it for you: `2025-11-30` picked in UTC+8 arrives as `2025-11-29T16:00:00.000Z`, and nothing in the request says which zone that value came from, so the day the user meant cannot be recovered. Only the client knows the zone; see *Filter a Whole Day on a Date Column* below.

## Additional Configuration

The following configurations are optional. They can be omitted when the default Grid and server behavior is sufficient.

### ▸ Forward Column-Level `ignoreCase` to the Server

Use `Filter.IgnoreCase` to enable case-insensitive matching for `eq`, `neq`, `contains`, `doesnotcontain`, `startswith`, and `endswith` on string fields; leave it unset to keep the existing case-sensitive behavior. If you build `DataSourceRequest`/`Filter` manually instead of going through the Grid, just set this property directly.

When `serverFiltering` is enabled, the Grid column setting [`filterable.ignoreCase`](https://www.telerik.com/kendo-jquery-ui/documentation/api/javascript/ui/grid/configuration/columns.filterable.ignorecase) is not automatically included in the request sent to the server. Use `parameterMap` to copy the ignoreCase value from the column whose field matches the filter descriptor's field.

For example:

```javascript
..... Other kendo grid code .....

dataSource: {
    schema: {...},
    transport: {
        read: {
            url: 'read url',
            dataType: 'json',
            contentType: 'application/json; charset=utf-8',
            type: 'POST'
        },
        parameterMap: function (data, operation) {

            /* Add the following code to forward column-level ignoreCase to the server */
            var grid = $("#grid-id").data("kendoGrid");     // replace "grid-id" with your grid's id
            var columns = grid ? grid.columns : [];
            var pendingFilters = [];

            if (data.filter) { pendingFilters.push(data.filter); }
            while (pendingFilters.length > 0) {
                var filter = pendingFilters.pop();
                if (!filter) { continue; }
                if (filter.filters) {
                    for (var i = 0; i < filter.filters.length; i++) { pendingFilters.push(filter.filters[i]); }
                    continue;
                }

                var column = columns.find(function (col) { return col.field === filter.field; });
                if (column && column.filterable && typeof column.filterable === "object" && column.filterable.ignoreCase !== undefined)
                {
                    filter.ignoreCase = column.filterable.ignoreCase;
                }
            }

            // ... other parameterMap code ...

            return kendo.stringify(data);
        }
    },
    error: function(e) {...},
    pageSize: 20,
    serverPaging: true,
    serverFiltering: true,
    serverSorting: true,
    ...
}

..... Other kendo grid code .....
```

### ▸ Filter a Whole Day on a Date Column

Because `eq` matches an exact instant, a date picker's selection finds only rows stored at exactly that moment. To match everything on the day the user picked, expand the `eq` into a `gte`/`lt` pair in `parameterMap`, where the browser's time zone is available. This is the approach Telerik documents in [Filter by Date Only](https://www.telerik.com/kendo-jquery-ui/documentation/knowledge-base/filter-by-date).

```javascript
parameterMap: function (data, operation) {

    /* Add the following code to expand an "eq" on a date column into the user's local day */
    var dateFields = ["CreatedOn"];     // replace with your own date fields

    function expand(node) {
        if (!node || !node.filters) { return; }
        for (var i = 0; i < node.filters.length; i++) {
            var child = node.filters[i];
            if (!child) { continue; }
            if (child.filters) { expand(child); continue; }
            if (child.operator !== "eq" || dateFields.indexOf(child.field) < 0) { continue; }

            var picked = new Date(child.value);
            node.filters[i] = {
                logic: "and",
                filters: [
                    { field: child.field, operator: "gte", value: new Date(picked.getFullYear(), picked.getMonth(), picked.getDate()) },
                    { field: child.field, operator: "lt", value: new Date(picked.getFullYear(), picked.getMonth(), picked.getDate() + 1) }
                ]
            };
        }
    }

    if (data.filter) {
        var root = { filters: [data.filter] };
        expand(root);
        data.filter = root.filters[0];
    }

    // ... other parameterMap code ...

    return kendo.stringify(data);
}
```

For a user in UTC+8 picking 2025-11-30, this sends `gte 2025-11-29T16:00:00.000Z` and `lt 2025-11-30T16:00:00.000Z`, which is the whole of that day in their own time zone.

## Known Issues

When server-side filterable options are enabled and apply a query with filter condition that contains `DateTime` type column, then EntityFramework Core would throw an
exception  `System.Data.SqlClient.SqlException (0x80131904): Conversion failed when converting date and/or time from character string`. The error is caused by a known issue in some old EntityFramework
Core versions. The workaround is adding `datetime` value to the related column in DbContext. e.g.

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
2. Switch to project root directory(src\KendoNET.DynamicLinq).
3. Run "dotnet restore"
4. Run "dotnet pack --configuration Release"
5. Add `<repository type="git" url="https://github.com/linmasaki/KendoNET.DynamicLinq.git" />` to package metadata of nupkg to show repository URL at Nuget

## Note

1. KendoNET.DynamicLinq is a reference to [Ali Sarkis's](https://github.com/mshtawythug/dlinq-helpers) Kendo.DynamicLinq.
2. This package was previously published as `Kendo.DynamicLinqCore`. Due to a trademark concern, and following coordination with the trademark holder, the project was renamed to `KendoNET.DynamicLinq`. The old package has since been delisted from NuGet; please switch to the new package ID above.

## Kendo UI Documentation

The following links are Kendo UI online docs(related to this package) and you can refer to.

- [Kendo UI Grid](https://docs.telerik.com/kendo-ui/api/javascript/ui/grid)
- [Kendo DataSource](https://docs.telerik.com/kendo-ui/api/javascript/data/datasource)

More Kendo UI configuration can refer to [here](https://demos.telerik.com/kendo-ui/)
