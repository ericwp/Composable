﻿using System.Data;

namespace Composable.Testing.Databases
{
    static class DatabaseHelpers
    {
        static string DropAllObjectsStatement = @"select id=IDENTITY (int, 1,1), stmt 
into #statements
FROM (SELECT CASE WHEN type = 'AF'                           THEN 'DROP AGGREGATE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type IN('C', 'F', 'UQ')               THEN 'ALTER TABLE ' + QUOTENAME(object_schema_name(parent_object_id)) + '.' + QUOTENAME(object_name(parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(name)
                                                              WHEN type IN('FN', 'FS', 'FT', 'IF', 'TF') THEN 'DROP FUNCTION ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type IN('P', 'PC')                    THEN 'DROP PROCEDURE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type = 'R'                            THEN 'DROP RULE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type = 'SN'                           THEN 'DROP SYNONYM ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type = 'U'                            THEN 'DROP TABLE ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type IN('TA', 'TR')                   THEN 'DROP TRIGGER ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                              WHEN type = 'V'                            THEN 'DROP VIEW ' + QUOTENAME(schema_name(schema_id)) + '.' + QUOTENAME(name)
                                                         END stmt, type
                                                    FROM sys.objects) x
                                 WHERE stmt IS NOT NULL
                                 ORDER BY CASE 
											WHEN type IN('F') THEN 1
											WHEN type IN('C', 'TA', 'TR') THEN 2 
											WHEN type IN('UQ') THEN 3
											ELSE 4
										END


DECLARE @statement nvarchar(500)
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT stmt from #statements order by id
OPEN cur

FETCH NEXT FROM cur INTO @statement

WHILE @@FETCH_STATUS = 0 BEGIN
    execute sp_executesql @statement
    FETCH NEXT FROM cur INTO @statement
END

CLOSE cur    
DEALLOCATE cur
";

        internal static readonly string SetReadCommittedSnapshotOnStatment = @"
declare @databaseName varchar(1000)
select @databaseName = DB_NAME()
declare @sql nvarchar(500)
set @sql = 'ALTER DATABASE [' + @databaseName +  '] SET READ_COMMITTED_SNAPSHOT ON'
exec sp_executesql @sql";

        internal static void DropAllObjects(this IDbConnection connection) {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = DropAllObjectsStatement;
                cmd.ExecuteNonQuery();
            }
        }

        internal static void DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel(this IDbConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = DropAllObjectsStatement + SetReadCommittedSnapshotOnStatment;
                cmd.ExecuteNonQuery();
            }
        }


    }
}
