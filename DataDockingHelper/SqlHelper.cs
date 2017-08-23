using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace DataDockingHelper
{ 
    public class SqlHelper
    {
        public static readonly string connstr = ConfigurationManager.ConnectionStrings["GBXRDB_GY"].ConnectionString;

        public static SqlConnection GetConnection()
        {
            SqlConnection conn = new SqlConnection(connstr);
            conn.Open();
            return conn;
        }

        public static int ExecuteNonQuery(string cmdText,
            params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public static object ExecuteScalar(string cmdText,
            params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }

        public static DataTable ExecuteDataTable(string cmdText,
            params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    cmd.Parameters.AddRange(parameters);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public static DataSet ExecuteDataSet(string cmdText,
            params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    cmd.Parameters.AddRange(parameters);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        return ds;
                    }
                }
            }
        }

        public static SqlDataReader ExecuteDataReader(string cmdText,
            params SqlParameter[] parameters)
        {
            SqlConnection conn = new SqlConnection(connstr);
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = cmdText;
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
        }

        public static int ExecuteStoredProcedure(string procName,
            params SqlParameter[] parameters)
        {
            SqlConnection conn = new SqlConnection(connstr);
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = procName;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }


        public static DataTable GetTableByProc(string procName, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(procName, conn))
                {
                    DataSet ds = new DataSet();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddRange(parameters);
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = cmd;
                    try
                    {
                        adapter.Fill(ds, "table");
                        cmd.Dispose();
                        conn.Close();
                    }
                    finally
                    {
                        cmd.Dispose();
                        conn.Close();
                    }
                    return ds.Tables[0];
                }
            }
        }

        /// <summary>
        /// 修改人员的身份证
        /// </summary>
        /// <param name="oldA0184">旧的身份证</param>
        /// <param name="newA0184">新的身份证</param>
        /// <returns></returns>
        public static int ChangeA0184(string oldA0184, string newA0184)
        {
            //判定身份证时候存在
            if (ExecuteDataTable("select A0184 from A01 where A0184 = '" + newA0184 + "'").Rows.Count > 0)
            {
                throw new Exception("身份证[" + newA0184 + "]已经存在");
            }
            const string sql = "select TableName from [ExTemplete]";
            var data = ExecuteDataTable(sql);
            var tableList = (from DataRow row in data.Rows select row["TableName"].ToString()).ToList();
            tableList.Add("A01");
            tableList.Add("A02");
            tableList.Add("T_FamilyInfo");
            tableList.Add("LostField");
            tableList = tableList.Distinct().ToList();
            var updateSql = "";
            foreach (var table in tableList)
            {
                if (ExecuteDataTable("select * from sysobjects where id = object_id('" + table + "')").Rows.Count > 0)
                {
                    updateSql += "update [" + table + "] set A0184 = '" + newA0184 + "' where A0184 = '" + oldA0184 + "';";
                }
            };

            return ExecuteNonQuery(updateSql);
        }

        #region 获取一个表的所有的字段

        /// <summary>
        /// 获取一个表中的所有的字段
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllFieldNameListByTableName(string tableName)
        {
            string sql = string.Format("select column_name from information_schema.columns where table_name = '{0}' ORDER by ordinal_position", tableName);
            using (var conn = GetConnection())
            {
                return conn.Query<string>(sql);
            }
        }

        #endregion

        #region 获取一个表中的所有的字段模型集合

        /// <summary>
        /// 获取一个表中的所有的字段模型集合
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static IEnumerable<TableField> GetAllFieldModelListByTableName(string tableName)
        {
            string sql = string.Format("select column_name as FieldName,DATA_TYPE as FieldType  from information_schema.columns where table_name = '{0}' ORDER by ordinal_position", tableName);
            using (var conn = GetConnection())
            {
                return conn.Query<TableField>(sql);
            }
        }

        #endregion

        public static void SqlBulkCopyByDataTable(string tableName, DataTable dt, string connectionString = "")
        {
            var conn = !string.IsNullOrEmpty(connectionString) ? new SqlConnection(connectionString) : GetConnection();
            connectionString = string.IsNullOrEmpty(connectionString) ? connstr : connectionString;
            using (conn)
            {
                using (SqlBulkCopy sqlbulkcopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.UseInternalTransaction))
                {
                    try
                    {
                        sqlbulkcopy.DestinationTableName = tableName;
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            sqlbulkcopy.ColumnMappings.Add(dt.Columns[i].ColumnName, dt.Columns[i].ColumnName);
                        }
                        sqlbulkcopy.WriteToServer(dt);
                    }
                    catch (System.Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }

        public static void SqlBulkCopyByMutiDataTable(List<DataTable> lists, string connectionString = "")
        {
            var conn = !string.IsNullOrEmpty(connectionString) ? new SqlConnection(connectionString) : GetConnection();
            connectionString = string.IsNullOrEmpty(connectionString) ? connstr : connectionString;
            using (conn)
            {
                try
                {
                    foreach (var dt in lists)
                    {
                        using (SqlBulkCopy sqlbulkcopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.UseInternalTransaction))
                        {
                            sqlbulkcopy.DestinationTableName = dt.TableName;
                            for (int j = 0; j < dt.Columns.Count; j++)
                            {
                                sqlbulkcopy.ColumnMappings.Add(dt.Columns[j].ColumnName, dt.Columns[j].ColumnName);
                            }
                            sqlbulkcopy.WriteToServer(dt);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    throw ex;
                }

            }
        }
    }
}
