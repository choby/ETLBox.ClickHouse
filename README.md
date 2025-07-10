# ETLBox.ClickHouse <a href="https://www.nuget.org/packages/ETLBox.ClickHouse"><img src="http://img.shields.io/nuget/v/ETLBox.ClickHouse.svg?style=flat-square" alt=NuGet version height=18></a>

----

### This project uses Octonica.ClickHouseClient as the database driver, enabling ETLBox to perform batch writes to ClickHouse.

> ⚠️⚠️⚠️ This project relies on <a href=https://www.etlbox.net/>ETLBox</a> and only adds the ability to write data to Clickhouse. It does not license ETLBox commercially

----
### Pre-requirements

* [ETLBox 3.7.0+](https://www.etlbox.net/)
* [Octonica.ClickHouseClient](https://github.com/Octonica/ClickHouseClient)
* [.Net6.0+ SDK](https://dotnet.microsoft.com/download/dotnet)


### Add package
```shell
dotnet package add ETLBox.ClickHouse
```
### Example
```c#
var clickHouseConnectionString = ; //your clickhouse connection string 
var clickHouseConnectionManager = new ClickHouseConnectionManager(clickHouseConnectionString);
var dbDestination = new DbDestination<YourDbEntity>(clickHouseConnectionManager, your_table_name)
{
      BatchSize = 10000
};
// your_source.LinkTo(dbDestination);
await Network.ExecuteAsync(cancellationToken, dbDestination);
```

### Data type map, Database entities are defined according to the following relationships

  | clickhouse field type | clr type                                   |
  |--------|--------------------------------------------|
  | bit  | System.Boolean                             |
  | boolean  | System.Boolean                             |
  |  tinyint | System.UInt16                              |
  | smallint  | System.Int16                               |
  | int2  | System.Int16                               |
  | int  | System.Int32                               |
  |  int4 | System.Int32                               |
  | int8  | System.Int32                               |
  | integer  | System.Int32                               |
  | bigint  | System.Int64                               |
  | decimal  | System.Decimal                             |
  |  number | System.Decimal                             |
  | money  | System.Decimal                             |
  |  smallmoney | System.Decimal                             |
  | numeric  | System.Decimal                             |
  | real  | System.Double                              |
  |  float | System.Double                              |
  | float4  | System.Double                              |
  | float8  | System.Double                              |
  | double  | System.Double                              |
  | double precision  | System.Double                              |
  | date  | System.DateTime  <br/>     System.DateOnly |
  |  datetime | System.DateTime<br/>System.DateTimeOffset  |
  | smalldatetime  | System.DateTime                            |
  |  datetime2 | System.DateTime                            |
  | time  | System.DateTime                            |
  | timetz  | System.DateTime                            |
  | timestamp  | System.DateTime                            |
  |  timestamptz | System.DateTime                            |
  |  uniqueidentifier | System.Guid                                |
  | uuid  | System.Guid                                |

### Additional resources

You can see the following resources to learn more about ETLBox and Octonica.ClickHouseClient:

* [ETLBox Connection Manager Turoaial](https://www.etlbox.net/docs/relational-databases/connection-manager/)
* [Octonica.ClickHouseClient bulk insert Tutorial](https://github.com/Octonica/ClickHouseClient?tab=readme-ov-file#bulk-insert)