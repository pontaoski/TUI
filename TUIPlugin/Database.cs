﻿using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using TerrariaUI;
using TShockAPI;
using TShockAPI.DB;

namespace TUIPlugin
{
    public static class Database
    {
        #region Data

        public const string UserTableName = "Users";
        public const string KeyValueTableName = "TUIKeyValue";
        public const string UserKeyValueTableName = "TUIUserKeyValue";
        public const string UserNumberTableName = "TUIUserNumber";
        public static bool IsMySql => db.GetSqlType() == SqlType.Mysql;

        public static IDbConnection db;

        #endregion

        #region ConnectDB

        /// <summary>
        /// Connects the mysql/sqlite database for the plugin, creating one if the database doesn't already exist.
        /// </summary>
        public static void ConnectDB()
        {
            if (TShock.Config.StorageType.ToLower() == "sqlite")
                db = new SqliteConnection(string.Format("uri=file://{0},Version=3",
                    Path.Combine(TShock.SavePath, "tshock.sqlite")));
            else if (TShock.Config.StorageType.ToLower() == "mysql")
            {
                try
                {
                    var host = TShock.Config.MySqlHost.Split(':');
                    db = new MySqlConnection
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
                            host[0],
                            host.Length == 1 ? "3306" : host[1],
                            TShock.Config.MySqlDbName,
                            TShock.Config.MySqlUsername,
                            TShock.Config.MySqlPassword)
                    };
                }
                catch (MySqlException e)
                {
                    TUI.HandleException(e);
                    throw new Exception("MySQL not setup correctly.");
                }
            }
            else
                throw new Exception("Invalid storage type.");

            //var sqlCreator = new SqlTableCreator(db,
            //    IsMySql
            //        ? (IQueryBuilder)new MysqlQueryCreator()
            //        : new SqliteQueryCreator());

            //sqlCreator.EnsureTableStructure(new SqlTable("TUIKeyValue",
            //    new SqlColumn("Key", MySqlDbType.TinyText) { Primary=true, Unique=true },
            //    new SqlColumn("Value", MySqlDbType.Text)));

            //sqlCreator.EnsureTableStructure(new SqlTable("TUIKeyValue",
                //new SqlColumn("Key", MySqlDbType.TinyText) { Primary = true, Unique = true },
                //new SqlColumn("Value", MySqlDbType.Binary)));

            Query($@"CREATE TABLE IF NOT EXISTS TUIKeyValue(
                        `Key` TEXT UNIQUE NOT NULL,
                        `Value` BINARY NOT NULL);
                     CREATE TABLE IF NOT EXISTS TUIUserNumber(
                        `User` INTEGER NOT NULL,
                        `Key` TEXT NOT NULL,
                        `Number` INTEGER NOT NULL,
                        UNIQUE{(IsMySql ? " KEY" : "")} (`User`, `Key`));
                     CREATE TABLE IF NOT EXISTS TUIUserKeyValue(
                        `User` INTEGER NOT NULL,
                        `Key` TEXT NOT NULL,
                        `Value` BINARY NOT NULL,
                        UNIQUE{(IsMySql ? " KEY" : "")} (`User`, `Key`))");

            /*sqlCreator.EnsureTableStructure(new SqlTable("TUIUserKeyValue",
                new SqlColumn("Key", MySqlDbType.TinyText) { Primary = true },
                new SqlColumn("Key", MySqlDbType.TinyText) { Primary = true },
                new SqlColumn("Value", MySqlDbType.Text)));*/
        }

        #endregion
        #region Query

        public static bool Query(string query)
        {
            bool success = true;
            db.Open();
            try
            {
                using (var conn = db.CreateCommand())
                {
                    conn.CommandText = query;
                    conn.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                TUI.HandleException(e);
                success = false;
            }
            finally
            {
                db.Close();
            }

            return success;
        }

        #endregion

        #region GetData(string key)

        public static byte[] GetData(string key)
        {
            db.Open();
            try
            {
                using (IDbCommand cmd = db.CreateCommand())
                {
                    cmd.CommandText = "SELECT Value FROM {0} WHERE Key='{1}'".SFormat(KeyValueTableName, key);
                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return (byte[])reader["Value"];
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                TUI.HandleException(new Exception($"TUI.Database.GetData(key:{key})", e));
            }
            finally
            {
                db.Close();
            }

            return null;
        }

        #endregion
        #region SetData(string key, byte[] data)

        public static void SetData(string key, byte[] data)
        {
            db.Open();
            try
            {
                using (var conn = db.CreateCommand())
                {
                    conn.CommandText = "REPLACE INTO {0} (Key, Value) VALUES ({1})".SFormat(KeyValueTableName, $"'{key}', @data");
                    conn.AddParameter("@data", data);
                    conn.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                TUI.HandleException(new Exception($"TUI.Database.SetData(key:{key}, ...)", e));
            }
            finally
            {
                db.Close();
            }
        }

        #endregion
        #region RemoveKey(string key)

        public static void RemoveKey(string key) =>
            Query("DELETE FROM {0} WHERE Key='{1}'".SFormat(KeyValueTableName, key));

        #endregion

        #region GetData(int user, string key)

        public static byte[] GetData(int user, string key)
        {
            db.Open();
            try
            {
                using (IDbCommand cmd = db.CreateCommand())
                {
                    cmd.CommandText = "SELECT Value FROM {0} WHERE User={1} AND Key='{2}'".SFormat(UserKeyValueTableName, user, key);
                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return (byte[])reader["Value"];
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                TUI.HandleException(new Exception(
                    $"TUI.Database.GetData(user:{user}, key:{key}, ...)", e));
            }
            finally
            {
                db.Close();
            }

            return null;
        }

        #endregion
        #region SetData(int user, string key, byte[] data)

        public static void SetData(int user, string key, byte[] data)
        {
            db.Open();
            try
            {
                using (var conn = db.CreateCommand())
                {
                    conn.CommandText = "REPLACE INTO {0} (User, Key, Value) VALUES ({1})".SFormat(UserKeyValueTableName, $"{user}, '{key}', @data");
                    conn.AddParameter("@data", data);
                    conn.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                TUI.HandleException(new Exception(
                    $"TUI.Database.SetData(user:{user}, key:{key}, ...)", e));
            }
            finally
            {
                db.Close();
            }
        }

        #endregion
        #region RemoveKey(int user, string key)

        public static void RemoveKey(int user, string key) =>
            Query("DELETE FROM {0} WHERE User={1} AND Key='{2}'".SFormat(UserKeyValueTableName, user, key));

        #endregion

        #region GetNumber

        public static int? GetNumber(int user, string key)
        {
            db.Open();
            try
            {
                using (IDbCommand cmd = db.CreateCommand())
                {
                    cmd.CommandText = "SELECT Number FROM {0} WHERE User={1} AND Key='{2}'".SFormat(UserNumberTableName, user, key);
                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return (int)reader["Number"];
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                TUI.HandleException(new Exception(
                    $"TUI.Database.GetData(user:{user}, key:{key}, ...)", e));
            }
            finally
            {
                db.Close();
            }

            return null;
        }

        #endregion
        #region SetNumber

        public static void SetNumber(int user, string key, int number)
        {
            db.Open();
            try
            {
                using (var conn = db.CreateCommand())
                {
                    conn.CommandText = "REPLACE INTO {0} (User, Key, Number) VALUES ({1})".SFormat(UserNumberTableName, $"{user}, '{key}', @number");
                    conn.AddParameter("@number", number);
                    conn.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                TUI.HandleException(new Exception(
                    $"TUI.Database.SetData(user:{user}, key:{key}, ...)", e));
            }
            finally
            {
                db.Close();
            }
        }

        #endregion
        #region RemoveNumber

        public static void RemoveNumber(int user, string key) =>
            Query("DELETE FROM {0} WHERE User={1} AND Key='{2}'".SFormat(UserNumberTableName, user, key));

        #endregion
        #region SelectNumbers

        public static List<(int User, int Number, string Username)> SelectNumbers(string key, bool ascending, int count, int offset, bool requestNames)
        {
            List<(int, int, string)> result = new List<(int, int, string)>();
            try
            {
                string query = requestNames ?
$@"SELECT number.User, number.Number, user.Username
	FROM {UserNumberTableName} AS number
    JOIN {UserTableName} as user ON number.User = user.ID
    WHERE Key=@0
    ORDER BY Number {(ascending ? "ASC" : "DESC")}
    LIMIT @1
    OFFSET @2"
: $@"SELECT User, Number
    FROM {UserNumberTableName}
    WHERE Key=@0
    ORDER BY Number {(ascending ? "ASC" : "DESC")}
    LIMIT @1
    OFFSET @2";
                using (QueryResult reader = db.QueryReader(query, key, count, offset))
                {
                    if (reader.Read())
                    {
                        int user = reader.Get<int>("User");
                        int number = reader.Get<int>("Number");
                        string username = requestNames ? reader.Get<string>("Username") : null;
                        result.Add((user, number, username));
                    }
                }
            }
            catch (Exception e)
            {
                TUI.HandleException(new Exception(
                    $"TUI.Database.SelectNumbers(key:{key}, ...)", e));
            }
            return result;
        }

        #endregion
    }
}
