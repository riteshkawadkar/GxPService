using GxPService.Dto;
using log4net;
using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace GxPService.Helpers
{
    public class DatabaseHelper
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string _dbFilePath;

        public DatabaseHelper()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string dbDirectory = Path.Combine(appDataPath, GlobalConstants.DatabasePath);
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
                log.Info($"Created directory: {dbDirectory}");
            }
            else
            {
                log.Info($"Directory already exists: {dbDirectory}");
            }
            _dbFilePath = Path.Combine(dbDirectory, GlobalConstants.DatabaseFileName);

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            string connectionString = $"Data Source={_dbFilePath};Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    log.Info("Database connection opened successfully.");

                    string createPolicyLogsTableSql = @"
                    CREATE TABLE IF NOT EXISTS PolicyLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        Path TEXT,
                        Entry TEXT,
                        OldValue TEXT,
                        NewValue TEXT,
                        RegType TEXT,
                        WindowsOperatingSystem TEXT,
                        State TEXT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        ServerTimestamp TEXT
                    )";

                    string createIOLogsTableSql = @"
                    CREATE TABLE IF NOT EXISTS IOLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        Value TEXT,
                        State TEXT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        ServerTimestamp TEXT
                    )";

                    using (SQLiteCommand createPolicyLogsTableCommand = new SQLiteCommand(createPolicyLogsTableSql, connection))
                    {
                        createPolicyLogsTableCommand.ExecuteNonQuery();
                        log.Info("PolicyLogs table created or already exists.");
                    }

                    using (SQLiteCommand createIOLogsTableCommand = new SQLiteCommand(createIOLogsTableSql, connection))
                    {
                        createIOLogsTableCommand.ExecuteNonQuery();
                        log.Info("IOLogs table created or already exists.");
                    }
                }
                catch (Exception ex)
                {
                    log.Error("An error occurred while initializing the database.", ex);
                }
            }
        }

        public void WriteToDatabase(PolicyLog policyLog)
        {
            string connectionString = $"Data Source={_dbFilePath};Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string insertSql = @"
                    INSERT INTO PolicyLogs (
                        Name, Path, Entry, OldValue, NewValue, RegType, 
                        WindowsOperatingSystem, State, Timestamp, ServerTimestamp
                    ) VALUES (
                        @Name, @Path, @Entry, @OldValue, @NewValue, @RegType, 
                        @WindowsOperatingSystem, @State, @Timestamp, @ServerTimestamp
                    )";

                    using (SQLiteCommand insertCommand = new SQLiteCommand(insertSql, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Name", policyLog.Name);
                        insertCommand.Parameters.AddWithValue("@Path", policyLog.Path);
                        insertCommand.Parameters.AddWithValue("@Entry", policyLog.Entry);
                        insertCommand.Parameters.AddWithValue("@OldValue", policyLog.OldValue);
                        insertCommand.Parameters.AddWithValue("@NewValue", policyLog.NewValue);
                        insertCommand.Parameters.AddWithValue("@RegType", policyLog.RegType);
                        insertCommand.Parameters.AddWithValue("@WindowsOperatingSystem", policyLog.WindowsOperatingSystem);
                        insertCommand.Parameters.AddWithValue("@State", policyLog.State);
                        insertCommand.Parameters.AddWithValue("@Timestamp", policyLog.Timestamp);
                        insertCommand.Parameters.AddWithValue("@ServerTimestamp", policyLog.ServerTimestamp);

                        insertCommand.ExecuteNonQuery();
                        log.Info($"PolicyLog for {policyLog.Name} written to database successfully.");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"An error occurred while writing PolicyLog for {policyLog.Name} to the database.", ex);
                }
            }
        }

        public void WriteToDatabase(IOLog ioLog)
        {
            string connectionString = $"Data Source={_dbFilePath};Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string insertSql = @"
                    INSERT INTO IOLogs (
                        Name, Value, State, Timestamp, ServerTimestamp
                    ) VALUES (
                        @Name, @Value, @State, @Timestamp, @ServerTimestamp
                    )";

                    using (SQLiteCommand insertCommand = new SQLiteCommand(insertSql, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Name", ioLog.Name);
                        insertCommand.Parameters.AddWithValue("@Value", ioLog.Value);
                        insertCommand.Parameters.AddWithValue("@State", ioLog.State);
                        insertCommand.Parameters.AddWithValue("@Timestamp", ioLog.Timestamp);
                        insertCommand.Parameters.AddWithValue("@ServerTimestamp", ioLog.ServerTimestamp);

                        insertCommand.ExecuteNonQuery();
                        //log.Info($"IOLog for {ioLog.Name} written to database successfully.");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"An error occurred while writing IOLog for {ioLog.Name} to the database.", ex);
                }
            }
        }

        public DateTime? GetLastServerTimestamp(string policyName)
        {
            string connectionString = $"Data Source={_dbFilePath};Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                    SELECT ServerTimestamp
                    FROM PolicyLogs
                    WHERE Name = @Name
                    ORDER BY Timestamp DESC
                    LIMIT 1";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", policyName);
                        var result = command.ExecuteScalar();
                        if (result != null && DateTime.TryParse(result.ToString(), out DateTime timestamp))
                        {
                            log.Info($"Last ServerTimestamp {timestamp} fetched successfully for {policyName}.");
                            return timestamp;
                        }
                        else
                        {
                            log.Warn($"No ServerTimestamp found for {policyName}.");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"An error occurred while fetching the last ServerTimestamp for {policyName}.", ex);
                    return null;
                }
            }
        }

        public DateTime? GetLastServerTimestamp()
        {
            string connectionString = $"Data Source={_dbFilePath};Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                    SELECT ServerTimestamp
                    FROM PolicyLogs
                    ORDER BY Timestamp DESC
                    LIMIT 1";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        if (result != null && DateTime.TryParse(result.ToString(), out DateTime timestamp))
                        {
                            log.Info($"Last ServerTimestamp {timestamp} fetched successfully.");
                            return timestamp;
                        }
                        else
                        {
                            log.Warn($"No ServerTimestamp found.");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"An error occurred while fetching the last ServerTimestamp.", ex);
                    return null;
                }
            }
        }

        public DateTime? GetLastServerTimestampForIOPolicy(string policyName)
        {
            string connectionString = $"Data Source={_dbFilePath};Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                    SELECT Timestamp
                    FROM IOLogs
                    WHERE Name = @Name
                    ORDER BY Timestamp DESC
                    LIMIT 1";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", policyName);
                        var result = command.ExecuteScalar();
                        if (result != null && DateTime.TryParse(result.ToString(), out DateTime timestamp))
                        {
                            log.Info($"Last ServerTimestamp {timestamp} for {policyName} fetched successfully.");
                            return timestamp;
                        }
                        else
                        {
                            log.Warn($"No ServerTimestamp found for {policyName}.");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"An error occurred while fetching the last ServerTimestamp for {policyName}.", ex);
                    return null;
                }
            }
        }
    }
}
