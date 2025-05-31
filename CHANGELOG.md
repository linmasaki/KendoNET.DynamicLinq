# Change Log

### V3.1.2 (2022/07/14)

-[#22](https://github.com/luizfbicalho/KendoNET.DynamicLinq/discussions/22) Rename this repository.

### V3.1.1 (2020/11/05)

- [#13](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/13) Fix the issue that filter will throw exception if decimal property is optional.
- [#6](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/6) Add asynchronous method of retrieving data(This feature is still in the experimental stage, not recommend using it on your product).

### V3.1.0 (2020/02/11)

- [#10](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/10) Fix the issue that the LINQ query with sub-property can't be translated and will be evaluated locally.
- [#12](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/12) Amend the issue that the method `ToDataSourceResult<T>(this IQueryable<T> queryable, DataSourceRequest request)` would ignore the
  aggregator parameter.

### V2.2.2 (2019/09/17)

- Remove `Append` and `Prepend` method of IEnumeration extension(.NET Standard built-in).
- Remove the duplicate `Aggregates` method.

### V2.2.0 (2019/07/05)

- Change the property `Group` of DataSourceResult to `Groups`.
- [#5](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/5) Add new property `Aggregate` to DataSourceRequest.
- [#5](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/5) Fixed getting wrong grouping data in the request using aggregates in grouping configuration.

### V2.1.0 (2019/05/16)

- [#3](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/3) Support new filtering operators of `is null`, `is not null`, `is empty`, `is not empty`, `has value`, and `has no value` in grid.
- [#3](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/3) Filtering operators of `is empty`, `is not empty`, `has value`, and `has no value` doesn't support non-string types.

### V2.0.2 (2019/04/12)

- Fix the problem that when query with conditions for a large amount of data will throw exception.
- Fix the query result will be unexpected when filter conditions are too much.
- Fix the issue that using aggregate count will throw an exception when a framework is .NET Core 1.0.
- Optimize performance and remove unnecessary properties.
- When the filter condition has a DateTime type value, the time will automatically change to the server's local time.

### V2.0.0 (2018/09/10)

- [#2](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/2) Support .Net Standard 2.0.

### V1.0.3 (2017/02/069)

- [#1](https://github.com/luizfbicalho/KendoNET.DynamicLinq/issues/1) Add `Errors` property in **`DataSourceResult`** class.
