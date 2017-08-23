using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataDockingHelper
{
    /// <summary>
    /// DataTableHelper dt的帮助类
    /// </summary>
    public class DataTableHelper
    {
        /// <summary>
        /// 创建dt
        /// </summary>
        /// <param name="columnDic">datatable中的列和对应的字段类型 【key:name,value:System.String】</param>
        /// <param name="lists"></param>
        /// <param name="tableName">DataTable的表名</param>
        /// <returns></returns>
        public DataTable GenerateDataTable(Dictionary<string, string> columnDic, List<Dictionary<string, object>> lists, string tableName)
        {
            var dt = GenerateDataTable(columnDic, tableName);
            CreateDataTableRows(dt, lists);
            return dt;
        }

        private DataTable GenerateDataTable(Dictionary<string, string> columnDic, string tableName)
        {
            //创建一个空表
            DataTable dt = new DataTable(tableName);

            foreach (var column in columnDic)
            {
                //2.创建带列名和类型名的列(两种方式任选其一)
                dt.Columns.Add(column.Key, System.Type.GetType(column.Value, true, true));
            }
            return dt;
        }

        private void CreateDataTableRows(DataTable dt, List<Dictionary<string, object>> lists)
        {
            foreach (var list in lists)
            {
                DataRow dr = dt.NewRow();
                foreach (var data in list)
                {
                    dr[data.Key] = data.Value;
                }
                dt.Rows.Add(dr);

            }
        }
    }
}
