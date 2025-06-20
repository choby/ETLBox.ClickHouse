### This project uses Octonica.ClickHouseClient as the database driver, enabling ETLBox to perform batch writes to ClickHouse.

----
- add package
```shell
dotnet package add ETLBox.ClickHouse
```
- example
```c#
var clickHouseConnectionString = ""; //your clickhouse connection string 
var clickHouseConnectionManager = new ClickHouseConnectionManager(clickHouseConnectionString);
var dbDestination = new ClickHouseDbDestination<YourDbEntity>(clickHouseConnectionManager, "your_table_name")
{
      BatchSize = 10000
};
// your_source.LinkTo(combineBrandLookupTrans);
await Network.ExecuteAsync(cancellationToken, dbDestination);
```