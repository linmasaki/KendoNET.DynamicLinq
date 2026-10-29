# Contributing

## Running the Tests

```bash
dotnet test test/KendoNET.DynamicLinq.Test/KendoNET.DynamicLinq.Test.csproj
```

### 🍃 MongoDB

The MongoDB tests are skipped unless `KENDONET_MONGODB` holds a connection string. To run them, start a MongoDB, e.g. with [Docker](https://docs.docker.com/get-started/get-docker/), and set the variable:

```bash
docker run -d -p 27017:27017 mongo:8
export KENDONET_MONGODB=mongodb://localhost:27017   # PowerShell: $env:KENDONET_MONGODB = "mongodb://localhost:27017"
```

The tests drop and recreate the `Employee` collection in the `Kendo` database, so don't point them at a database you care about.

## Building the NuGet Package

```bash
dotnet pack src/KendoNET.DynamicLinq.csproj --configuration Release
```
