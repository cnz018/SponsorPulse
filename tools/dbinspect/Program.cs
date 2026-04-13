using System;
using Microsoft.Data.Sqlite;

var dbPath = @"C:\Projets\Git\sponsorpulse\sponsorpulse\SponsorPulse\SponsorPulse.db";
if (!System.IO.File.Exists(dbPath))
{
    Console.WriteLine($"Database not found: {dbPath}");
    return 1;
}

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

static int ExecNonQuery(SqliteConnection conn, string sql)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    return cmd.ExecuteNonQuery();
}

static object? ExecScalar(SqliteConnection conn, string sql)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    return cmd.ExecuteScalar();
}


bool TableExists(string name)
{
    var v = ExecScalar(conn, $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{name}';");
    return Convert.ToInt32(v) > 0;
}

bool ColumnExists(string table, string column)
{
    var v = ExecScalar(conn, $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{column}';");
    return Convert.ToInt32(v) > 0;
}

System.Collections.Generic.List<string> GetColumns(string table)
{
    var cols = new System.Collections.Generic.List<string>();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"PRAGMA table_info('{table}');";
    using var rdr = cmd.ExecuteReader();
    while (rdr.Read()) cols.Add(rdr.GetString(1));
    return cols;
}

void PrintTableInfo(string table)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"PRAGMA table_info('{table}');";
    using var reader = cmd.ExecuteReader();
    if (!reader.HasRows)
    {
        Console.WriteLine($"Table '{table}' not found or has no columns.");
        return;
    }
    Console.WriteLine($"Columns for table {table}:");
    while (reader.Read())
    {
        var cid = reader.GetInt32(0);
        var name = reader.GetString(1);
        var type = reader.GetString(2);
        var notnull = reader.GetInt32(3);
        var dflt = reader.IsDBNull(4) ? "NULL" : reader.GetString(4);
        var pk = reader.GetInt32(5);
        Console.WriteLine($"  {cid}: {name} {type} NotNull={notnull} PK={pk} Default={dflt}");
    }
}

if (args.Length > 0 && args[0].Equals("prepare-rename", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Preparing rename if Events table has no OwnerId column...");
    if (!TableExists("Events"))
    {
        Console.WriteLine("No Events table found — nothing to do.");
        return 0;
    }
    if (ColumnExists("Events", "OwnerId"))
    {
        Console.WriteLine("Events table already has OwnerId — nothing to do.");
        return 0;
    }
    if (TableExists("Events_backup"))
    {
        Console.WriteLine("Backup table Events_backup already exists — aborting to avoid overwrite.");
        return 2;
    }
    Console.WriteLine("Renaming Events -> Events_backup");
    ExecNonQuery(conn, "ALTER TABLE Events RENAME TO Events_backup;");
    Console.WriteLine("Rename complete.");
    return 0;
}

if (args.Length > 0 && args[0].Equals("restore-data", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Restoring data from Events_backup into new Events table...");
    if (!TableExists("Events_backup"))
    {
        Console.WriteLine("No Events_backup table found — nothing to restore.");
        return 2;
    }
    if (!TableExists("Events"))
    {
        Console.WriteLine("Target Events table not found — cannot restore.");
        return 3;
    }
    if (!ColumnExists("Events", "OwnerId"))
    {
        Console.WriteLine("Target Events table missing OwnerId column — aborting restore.");
        return 4;
    }

    var copySql = @"
INSERT INTO Events (Id, Name, Description, Date, StreamPlatform, ChannelId, Slug, OwnerId, Status, ViewerCount, PeakViewers, StreamDuration, StartedAt, GameName, TwitterHashtags, TwitterAnalytics)
SELECT Id, Name, Description, Date, StreamPlatform, ChannelId, Slug, NULL as OwnerId, Status, ViewerCount, PeakViewers, StreamDuration, StartedAt, GameName, TwitterHashtags, TwitterAnalytics
FROM Events_backup eb
WHERE NOT EXISTS (SELECT 1 FROM Events e WHERE e.Id = eb.Id);
";

    var inserted = ExecNonQuery(conn, copySql);
    Console.WriteLine($"Rows inserted (skipped existing): {inserted}");

    Console.WriteLine("Dropping Events_backup table");
    ExecNonQuery(conn, "DROP TABLE IF EXISTS Events_backup;");
    Console.WriteLine("Restore complete.");
    return 0;
}

if (args.Length > 0 && args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT name, type FROM sqlite_master WHERE type='table' ORDER BY name;";
    using var rdr = cmd.ExecuteReader();
    Console.WriteLine("Tables:");
    while (rdr.Read()) Console.WriteLine($"  {rdr.GetString(0)}");
    return 0;
}

if (args.Length > 0 && args[0].Equals("info", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
{
    PrintTableInfo(args[1]);
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"SELECT sql FROM sqlite_master WHERE type='table' AND name='{args[1]}';";
    var sql = cmd.ExecuteScalar() as string ?? "<none>";
    Console.WriteLine("\nCREATE TABLE SQL:");
    Console.WriteLine(sql);
    return 0;
}

if (args.Length > 0 && args[0].Equals("rename", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
{
    var tbl = args[1];
    var backup = tbl + "_backup";
    if (!TableExists(tbl))
    {
        Console.WriteLine($"Table {tbl} not found — nothing to rename.");
        return 0;
    }
    if (TableExists(backup))
    {
        Console.WriteLine($"Backup table {backup} already exists — aborting to avoid overwrite.");
        return 2;
    }
    Console.WriteLine($"Renaming {tbl} -> {backup}");
    ExecNonQuery(conn, $"ALTER TABLE \"{tbl}\" RENAME TO \"{backup}\";");
    Console.WriteLine("Rename complete.");
    return 0;
}

if (args.Length > 0 && args[0].Equals("restore", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
{
    var tbl = args[1];
    var backup = tbl + "_backup";
    Console.WriteLine($"Restoring data from {backup} into {tbl}...");
    if (!TableExists(backup))
    {
        Console.WriteLine($"No {backup} found — nothing to restore.");
        return 2;
    }
    if (!TableExists(tbl))
    {
        Console.WriteLine($"Target table {tbl} not found — cannot restore.");
        return 3;
    }
    var backupCols = GetColumns(backup);
    var newCols = GetColumns(tbl);
    var common = backupCols.Intersect(newCols).ToList();
    if (!common.Contains("Id"))
    {
        Console.WriteLine("No Id column found in common columns — aborting.");
        return 4;
    }
    var colsList = string.Join(", ", common.Select(c => $"\"{c}\""));
    var selectList = string.Join(", ", common.Select(c => $"b.\"{c}\""));
    var sql = $@"INSERT INTO ""{tbl}"" ({colsList}) SELECT {selectList} FROM ""{backup}"" b WHERE NOT EXISTS (SELECT 1 FROM ""{tbl}"" n WHERE n.Id = b.Id);";
    var inserted = ExecNonQuery(conn, sql);
    Console.WriteLine($"Rows inserted (skipped existing): {inserted}");
    Console.WriteLine($"Dropping {backup}");
    ExecNonQuery(conn, $"DROP TABLE IF EXISTS \"{backup}\";");
    Console.WriteLine("Restore complete.");
    return 0;
}

if (args.Length > 0 && args[0].Equals("drop-index", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
{
    var idx = args[1];
    Console.WriteLine($"Dropping index {idx}");
    ExecNonQuery(conn, $"DROP INDEX IF EXISTS \"{idx}\";");
    Console.WriteLine("Dropped.");
    return 0;
}

if (args.Length > 0 && args[0].Equals("list-indexes", StringComparison.OrdinalIgnoreCase))
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT name, tbl_name, sql FROM sqlite_master WHERE type='index' ORDER BY tbl_name, name;";
    using var rdr = cmd.ExecuteReader();
    Console.WriteLine("Indexes:");
    while (rdr.Read()) Console.WriteLine($"  {rdr.GetString(0)} (table: {rdr.GetString(1)}) sql: { (rdr.IsDBNull(2) ? "<no sql>" : rdr.GetString(2)) }");
    return 0;
}

if (args.Length > 0 && args[0].Equals("selectall", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
{
    var tbl = args[1];
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"SELECT * FROM \"{tbl}\" LIMIT 100;";
    using var rdr = cmd.ExecuteReader();
    var cols = new System.Collections.Generic.List<string>();
    for (int i = 0; i < rdr.FieldCount; i++) cols.Add(rdr.GetName(i));
    Console.WriteLine(string.Join(" | ", cols));
    while (rdr.Read())
    {
        var values = new System.Collections.Generic.List<string>();
        for (int i = 0; i < cols.Count; i++) values.Add(rdr.IsDBNull(i) ? "NULL" : rdr.GetValue(i).ToString());
        Console.WriteLine(string.Join(" | ", values));
    }
    return 0;
}

// default: inspect
PrintTableInfo("Events");

using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='Events';";
    var sql = cmd.ExecuteScalar() as string ?? "<none>";
    Console.WriteLine("\nCREATE TABLE SQL for Events:");
    Console.WriteLine(sql);
}

using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT name, sql FROM sqlite_master WHERE type='index' AND tbl_name='Events';";
    using var rdr = cmd.ExecuteReader();
    Console.WriteLine("\nIndexes for Events:");
    var any = false;
    while (rdr.Read())
    {
        any = true;
        var name = rdr.GetString(0);
        var sql = rdr.IsDBNull(1) ? "<no sql>" : rdr.GetString(1);
        Console.WriteLine($"  {name}: {sql}");
    }
    if (!any) Console.WriteLine("  <none>");
}

return 0;