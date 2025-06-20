using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using ETLBox;
using ETLBox.ControlFlow;
using ETLBox.Helper;
using ETLBox.ClickHouse.ConnectionStrings;
using Masuit.Tools.Reflection;
using Octonica.ClickHouseClient;
using Octonica.ClickHouseClient.Types;

namespace ETLBox.ClickHouse.ConnectionManager
{
    public class ClickHouseConnectionManager : DbConnectionManager<ClickHouseConnection, DbTransaction, ClickHouseParameter>
    {
        private Dictionary<string, TableColumn> DestinationColumns { get; set; } = null!;
        private static readonly string[] trueArray = new[] { "yes", "true", "on", "1", "да" };
        public override string QB { get; protected set; } = @"";
        public override string QE { get; protected set; } = @"";

        public ClickHouseConnectionManager(ClickHouseConnectionString connectionString)
            : base((IDbConnectionString)connectionString)
        {
        }

        public ClickHouseConnectionManager(string connectionString)
            : base((IDbConnectionString)new ClickHouseConnectionString(connectionString))
        {
        }

        private TableDefinition DestTableDef { get; set; } = null!;

        public override void PrepareBulkInsert(string tableName)
        {
            if (DestinationColumns == null)
                ReadTableDefinition(tableName);
        }

        private void ReadTableDefinition(string tableName)
        {
            DestTableDef = TableDefinition.GetSchema(this, tableName);
            DestinationColumns = new Dictionary<string, TableColumn>();
            foreach (var colDef in DestTableDef.Columns)
            {
                DestinationColumns.Add(colDef.Name, colDef);
            }
        }


        public override void BulkInsert(ITableData data)
        {
            var destColumnNames = data.ColumnMapping
                .Cast<IColumnMapping>()
                .Select(cm => cm.DataSetColumn)
                .ToList();

            var dataDic = new Dictionary<string, object>();

            while (data.Read())
            {
                foreach (var destColumn in DestinationColumns.Keys)
                {
                    TableColumn colDef = DestinationColumns[destColumn];
                    object? val;
                    if (destColumnNames.Contains(colDef.Name))
                    {
                        var ordinal = data.GetOrdinal(destColumn);
                        val = data.GetValue(ordinal);
                    }
                    else
                    {
                        val = null;
                    }

                    if (!dataDic.ContainsKey(colDef.Name))
                    {
                        dataDic.Add(colDef.Name, new List<object>());
                    }
                    (dataDic[colDef.Name] as List<object?>)?.Add(val);
                }

            }

            if (DbConnection.State != ConnectionState.Open)
            {
                DbConnection.Open();
            }
            
            var columns = new ReadOnlyDictionary<string, object>(dataDic);
            var rowCount = (dataDic.First().Value as List<object?>).Count();
           
            using var writer = DbConnection.CreateColumnWriter($"INSERT INTO {QB}{data.DestinationTableName}{QE} VALUES");
            writer.WriteTable(columns, rowCount);
        }

        public override void CleanUpBulkInsert(string tableName)
        {
            // using var cmd = DbConnection.CreateCommand();
            // cmd.CommandText = $@"OPTIMIZE TABLE {QB}{tableName}{QE} FINAL";
            //
            // cmd.ExecuteNonQuery();
        }

        public override void BulkDelete(ITableData data)
        {
            throw new NotImplementedException();
        }

        public override void BulkUpdate(ITableData data, ICollection<string> setColumnNames, ICollection<string> joinColumnNames)
        {
            throw new NotImplementedException();
        }


        public override ConnectionType ConnectionType { get; protected set; }


        public override IConnectionManager Clone()
        {
            var clone = new ClickHouseConnectionManager(
                (ClickHouseConnectionString)ConnectionString
            )
            {
                MaxLoginAttempts = MaxLoginAttempts
            };
            return clone;
        }

        private static object? GetValue(object? r, TableColumn col)
        {
            var dataType = col.DataType.ToUpper();
            return r switch
            {
                null => "",
                DateTime when dataType is "DATE" or "NULLABLE(DATE)" => $"{r:yyyy-MM-dd}",
                DateTime => $"{r:yyyy-MM-dd HH:mm:ss}",
                bool b => b ? "1" : "0",
                decimal
                    or int
                    or long
                    or double
                    or float
                    or UInt64
                    or UInt16
                    or UInt32
                    or uint
                    or short
                    => Convert.ToString(r, CultureInfo.InvariantCulture),
                _ => ConvertToValueType(r, dataType)
            };
        }

        private static string? ConvertToValueType(object r, string dataType)
        {
            return !DataTypeConverter.IsCharTypeDefinition(dataType) && !dataType.Contains("STR")
                ? ConvertStringToNonStringType(r, dataType)
                : $@"""{r.ToString()!.Replace(@"""", @"""""").Replace("@", @"\@")}""";
        }

        private static string? ConvertStringToNonStringType(object r, string dataType)
        {
            if (dataType.Contains("DECIMAL"))
            {
                return Convert.ToDecimal(r).ToString(CultureInfo.InvariantCulture);
            }

            if (dataType.Contains("INT"))
            {
                return Convert.ToInt64(r, CultureInfo.InvariantCulture).ToString();
            }

            if (dataType.Contains("DATETIME"))
            {
                return Convert.ToDateTime(r).ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (dataType.Contains("DATE"))
            {
                return Convert.ToDateTime(r).ToString("yyyy-MM-dd");
            }

            if (dataType.Contains("BOOL") || dataType.Contains("BIT"))
            {
                return Convert.ToBoolean(r, CultureInfo.InvariantCulture).ToString();
            }

            return r.ToString();
        }

        public override TableDefinition GetSchema(string tableName)
        {
            var result = new TableDefinition(tableName);
            TableColumn curCol = null;

            var readMetaSql = new SqlTask(
                $@"
                SELECT
                     c.name
                    ,c.type as type
                    ,c.is_in_primary_key as primary_key
                    ,c.default_expression as default_value
                    ,ic.is_nullable
                    ,c.comment
                FROM system.columns c
                LEFT JOIN information_schema.columns as ic
                    ON ic.table_name = c.table
                    AND ic.column_name = c.name
                    AND ic.table_schema = c.database
                WHERE c.database = currentDatabase()
                  AND table = '{tableName}'",
                () => { curCol = new TableColumn(); },
                () => { result.Columns.Add(curCol); },
                name => curCol.Name = name.ToString(),
                type =>
                {
                    curCol.DataType = type.ToString();
                    var typeInfo = ClickHouseTypeInfoProvider.Instance.GetTypeInfo(type.ToString());
                    curCol.SetProperty("ClrType", typeInfo.GetFieldType());
                },
                primaryKey => curCol.IsPrimaryKey = ParseBoolean(primaryKey),
                defaultValue => curCol.DefaultValue = defaultValue?.ToString(),
                is_nullable => curCol.AllowNulls = ParseBoolean(is_nullable),
                comment => curCol.Comment = comment?.ToString()
            )
            {
                DisableLogging = true,
                ConnectionManager = this
            };
            readMetaSql.ExecuteReader();
            return result;
        }

        private static bool ParseBoolean(object value)
        {
            if (value is null)
            {
                return false;
            }

            return trueArray.Contains(value.ToString().ToLower());
        }
    }
}