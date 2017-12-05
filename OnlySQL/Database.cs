using CSScriptLibrary;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlySQL
{
    public sealed class Database : IDisposable
    {
        private MySqlConnection _connection;
        private MySqlTransaction _transaction;

        public static string Address { get; set; } 
        public static string Port { get; set; }
        public static string User { get; set; } 
        public static string Password { get; set; } 

        public Database(string database = "main")
        {
            _connection = new MySqlConnection($"Server={Address};Port={Port};Uid={User};Pwd='{Password}';SslMode=none;Compress=true;ConvertZeroDateTime=true;" + (string.IsNullOrWhiteSpace(database) ? "" : $"Database={database}"));
            //var Settings = _connection.GetType().GetProperty("Settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_connection);            
            _connection.Open();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var driver = _connection.GetType().GetField("driver", flags).GetValue(_connection);
            if (driver != null)
            {
                driver.GetType().GetMethod("Reset").Invoke(driver, null);
            }
        }

        public void BeginTransaction()
        {
            if (_transaction == null)
            {
                _transaction = _connection.BeginTransaction();
            }
        }

        /// <summary>
        /// Executes a query inside a MySqlTransaction object and returns the value in position [0,0]
        /// </summary>
        /// <param name="query">The Query you wish to execute</param>
        /// <param name="parameters">The MySqlParameter you wish to attach to the query</param>
        /// <returns>The value in position [0,0]</returns>
        public T GetDataSingleResult<T>(string query, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = CheckConnectionValid();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                return (T)cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Executes a query inside a MySqlTransaction object and returns the value in position [0,0]
        /// </summary>
        /// <param name="query">The Query you wish to execute</param>
        /// <param name="parameters">The MySqlParameter you wish to attach to the query</param>
        /// <returns>The value in position [0,0]</returns>
        public bool TryGetDataSingleResult<T>(string query, out T value, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = CheckConnectionValid();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                var obj = cmd.ExecuteScalar();
                if (obj == null)
                {
                    value = default(T);
                    return false;
                }
                else if (obj == DBNull.Value)
                {
                    value = default(T);
                    return true;
                }
                else
                {
                    if (obj.GetType() == typeof(T))
                    {
                        value = (T)obj;
                    }
                    else
                    {
                        value = (T)Convert.ChangeType(obj, typeof(T));
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Executes a query inside a MySqlTransaction object and returns a datatable of the results
        /// </summary>
        /// <param name="query">The Query you wish to execute</param>
        /// <param name="parameters">The MySqlParameter you wish to attach to the query</param>
        /// <returns>A DataTable containing the results view</returns>
        public DataTable GetDataTable(string query, params MySqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            MySqlConnection conn = CheckConnectionValid();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    DataSet ds = new DataSet();
                    ds.Tables.Add(dt);
                    ds.EnforceConstraints = false;
                    dt.Load(dr);
                }
            }
            return dt;
        }

        public List<dynamic> ReadData(string query, params MySqlParameter[] parameters)
        {
            List<dynamic> data = new List<dynamic>();
            MySqlConnection conn = CheckConnectionValid();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                cmd.CommandText = query;
                using (var r = cmd.ExecuteReader())
                {
                    var func = GetFunction(r);

                    while (r.Read())
                        data.Add(func(r));                        
                }
            }
            return data;
        }

        public static List<FuncCache> ReaderCached = new List<FuncCache>();

        public class FuncCache
        {
            public List<ArgCache> ArgCache = new List<ArgCache>();
            public Func<IDataRecord, dynamic> Func = null;
        }

        public class ArgCache
        {
            public string Name;
            public Type Type;
        }

        private static Func<IDataRecord, dynamic> GetFunction(MySqlDataReader reader)
        {
            var activeFuncCach = new FuncCache();            
            var fieldCount = reader.FieldCount;
            for (int i = 0; i < fieldCount; i++)
            {
                var arg = new ArgCache();

                arg.Name = reader.GetName(i);
                arg.Type = reader.GetFieldType(i);

                activeFuncCach.ArgCache.Add(arg);
            }

            int length = ReaderCached.Count;
            
            for (int i = 0; i < length; i++)
            {
                if(ReaderCached[i].ArgCache.Count == fieldCount)
                {
                    bool found = true;
                    for (int k = 0; k < fieldCount; k++)
                    {
                        if(activeFuncCach.ArgCache[k].Type != ReaderCached[i].ArgCache[k].Type)
                        {
                            found = false;
                            break;
                        }
                        if (string.CompareOrdinal(activeFuncCach.ArgCache[k].Name,
                            ReaderCached[i].ArgCache[k].Name) != 0)
                        {
                            found = false;
                            break;
                        }
                    }

                    if(found)
                    {
                        return ReaderCached[i].Func;
                    }
                }
            }

            ReaderCached.Add(activeFuncCach);

            var builder = new StringBuilder();

            for (int i = 0; i < fieldCount; i++)
            {
                var arg = activeFuncCach.ArgCache[i];
                
                string setString = "";
                if(arg.Type == typeof(string))
                {

                    setString = "idata.IsDBNull(" + i.ToString() + ") ? null : idata.GetString(" + i.ToString() + ")";
                }else if (arg.Type == typeof(int))
                {                    
                    setString = "idata.IsDBNull(" + i.ToString() + ") ? 0 : idata.GetInt32(" + i.ToString() + ")";
                }
                else if (arg.Type == typeof(byte))
                {                    
                    setString = "idata.IsDBNull(" + i.ToString() + ") ? 0 : idata.GetByte(" + i.ToString() + ")";
                }
                else if (arg.Type == typeof(DateTime))
                {                    
                    setString = "idata.IsDBNull(" + i.ToString() + ") ? DateTime.MinValue : idata.GetDateTime(" + i.ToString() + ")";
                }
                else if (arg.Type == typeof(bool))
                {                    
                    setString = "idata.IsDBNull(" + i.ToString() + ") ? false : idata.GetBoolean(" + i.ToString() + ")";
                }
                else if (arg.Type == typeof(short))
                {                    
                    setString = "idata.IsDBNull(" + i.ToString() + ") ? 0 : idata.GetInt16(" + i.ToString() + ")";
                }
                else if (arg.Type == typeof(long))
                {                    
                    setString = "idata.IsDBNull(" + i.ToString() + ") ? 0 : idata.GetInt64(" + i.ToString() + ")";
                }
                else if (arg.Type == typeof(decimal))
                {                    
                    setString = "idata.IsDBNull(" + i.ToString() + ") ? 0 : idata.GetDecimal(" + i.ToString() + ")";
                }
                else if (arg.Type == typeof(float))
                {
                    setString = "idata.IsDBNull(" + i.ToString() + ") ? 0 : idata.GetFloat(" + i.ToString() + ")";
                }
                else if (arg.Type == typeof(byte[]))
                {
                    
                    setString =
@"if(idata.IsDBNull(" + i.ToString() + @"))
{
    dynm." + arg.Name + @" = null;
}else
{
    var file_" + i.ToString() + @" = idata.GetInt32(" + i.ToString() + @");
    var data_" + i.ToString() + @" = new byte[file_" + i.ToString() + @"];

    idata.GetBytes(" + i.ToString() + @", 0, data_" + i.ToString() + @", 0, file_" + i.ToString() + @");
    dynm." + arg.Name + @" = data_" + i.ToString() + @";
}";

                    builder.AppendLine(setString);
                    continue;
                }
                
                builder.AppendLine("dynm." + arg.Name + " = " + setString + ";");                
            }
            
            activeFuncCach.Func = CSScript.Evaluator
                                      .LoadDelegate<Func<IDataRecord, dynamic>>(
@"

using System.Data;

dynamic funcPrivate(System.Data.IDataRecord idata)
{
    dynamic dynm = new System.Dynamic.ExpandoObject();    
" + builder.ToString() + @"    
    return dynm;
}");
           
            return activeFuncCach.Func;
        }

        public long SetDataReturnLastInsertId(string query, params MySqlParameter[] parameters)
        {
            using (MySqlCommand cmd = SetData(query, parameters))
            {
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }

        public List<T> ReadTransactionSeq<T>(string query, Func<IDataRecord, T> selector, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = CheckConnectionValid();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                cmd.CommandText = query;
                using (var r = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    var items = new List<T>();
                    while (r.Read())
                        items.Add(selector(r));
                    return items;
                }
            }
        }

        public List<T> ReadTransaction<T>(string query, Func<IDataRecord, T> selector, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = CheckConnectionValid();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                cmd.CommandText = query;
                using (var r = cmd.ExecuteReader())
                {
                    var items = new List<T>();
                    while (r.Read())
                        items.Add(selector(r));
                    return items;
                }
            }
        }

        public IEnumerable<T> ReadTransactionEnumerable<T>(string query, Func<IDataRecord, T> selector, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = CheckConnectionValid();
            using (MySqlCommand cmd = CreateCommand(conn, query, parameters))
            {
                cmd.CommandText = query;
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        yield return selector(r);
                }
            }
        }

        public int SetDataReturnRowCount(string query, params MySqlParameter[] parameters)
        {
            using (MySqlCommand cmd = SetData(query, parameters))
            {
                int value = cmd.ExecuteNonQuery();
                return value;
            }
        }

        public bool SetDataReturnNone(string query, params MySqlParameter[] parameters)
        {
            using (MySqlCommand cmd = SetData(query, parameters))
            {
                cmd.ExecuteNonQuery();
                return true;
            }
        }

        /// <summary>
        /// Executes a query inside a MySqlTransaction object and returns meta information about the query
        /// </summary>
        /// <param name="query">The Query you wish to execute</param>
        /// <param name="parameters">The MySqlParameter you wish to attach to the query</param>
        /// <returns>A long value containing info in relation to the returnInfo parameter</returns>
        public MySqlCommand SetData(string query, params MySqlParameter[] parameters)
        {
            MySqlConnection conn = CheckConnectionValid();
            if (_transaction == null)
            {
                BeginTransaction();
            }
            return CreateCommand(conn, query, parameters);
        }
        private bool _commited = false;
        /// <summary>
        /// Commits the contents of the MySqlTransaction Object to the database.
        /// </summary>
        public bool TransactionCommit()
        {
            try
            {
                if (_transaction != null)
                {
                    _transaction.Commit();
                    _commited = true;
                }

                return true;
            }
            catch (Exception)
            {
                if (_transaction != null)
                {
                    _transaction.Rollback();
                    _transaction = null;
                }
                return false;
            }
        }

        public void TransactionRollback()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction = null;
            }

        }

        public MySqlConnection CheckConnectionValid()
        {
            MySqlConnection conn = _connection;
            if (conn.State != ConnectionState.Closed)
            {
                return conn;
            }
            conn.Open();
            return conn;
        }

        public MySqlCommand CreateCommand(MySqlConnection conn, string query, params MySqlParameter[] parameters)
        {
            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = query,
                Connection = conn
            };

            if (_transaction != null)
                cmd.Transaction = _transaction;

            cmd.Parameters.AddRange(parameters);
            return cmd;
        }

        //public static MySqlConnection OpenConnection(MySqlConnection conn)
        //{
        //    var driver = conn.GetType().GetField("driver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_connection);

        //    conn.Open();
        //    return conn;
        //}

        public void Dispose()
        {
            // get rid of managed resources, call Dispose on member variables...
            var driver = _connection.GetType().GetField("driver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_connection);

            if (_transaction != null)
            {

                if (!_commited)
                {
                    _transaction.Rollback();
                }

                _transaction.Dispose();
            }

            if (_connection != null)
            {
                _connection.Dispose();
            }
        }
    }
}
