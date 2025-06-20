# ETLBox.ClickHouse <a href="https://www.nuget.org/packages/ETLBox.ClickHouse/"><img src="http://img.shields.io/nuget/v/ETLBox.ClickHouse.svg?style=flat-square" alt="NuGet version" height="18"></a>

----

### This project uses Octonica.ClickHouseClient as the database driver, enabling ETLBox to perform batch writes to ClickHouse.

> ⚠️⚠️⚠️ This project relies on <a href="https://www.etlbox.net/">ETLBox</a> and only adds the ability to write data to Clickhouse. It does not license ETLBox commercially

----
- add package
```shell
dotnet package add ETLBox.ClickHouse
```
- example
```c#
var clickHouseConnectionString = ""; //your clickhouse connection string 
var clickHouseConnectionManager = new ClickHouseConnectionManager(clickHouseConnectionString);
var dbDestination = new DbDestination<YourDbEntity>(clickHouseConnectionManager, "your_table_name")
{
      BatchSize = 10000
};
// your_source.LinkTo(dbDestination);
await Network.ExecuteAsync(cancellationToken, dbDestination);
```