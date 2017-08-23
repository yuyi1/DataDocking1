using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataDockingHelper
{
    /// <summary>
    /// 导出数据库中的数据到 XML 的帮助类
    /// </summary>
    public class ExportDataToXMLHelper
    {
        #region 0.ExportXML 导出xml

        public void ExportDataToXml(ExportDataConfig exportDataConfig)
        {
            GenetateXML(exportDataConfig);
        }

        public void ExportDataToXml(string tableName, IEnumerable<string> needFilterFieldList)
        {
            GenetateXML(tableName, needFilterFieldList);
        }

        #endregion

        #region 1.准备需要导出的数据为DataTable 【-GetDataTable(string tableName, IEnumerable<string> needFilterFieldList)】
        /// <summary>
        /// 1.准备需要导出的数据为DataTable
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="needFilterFieldList">被过滤的字段集合【1.没有需要过滤掉的字段，就直接设置为null，或者为空集合】</param>
        /// <returns></returns>
        private DataTable GetDataTable(string tableName, IEnumerable<string> needFilterFieldList, out IEnumerable<string> needQueryFieldList)
        {
            needQueryFieldList = GetNeedQueryFieldList(tableName, needFilterFieldList);
            string queryFieldStr = string.Join(",", needQueryFieldList);
            if (string.IsNullOrEmpty(queryFieldStr))
            {
                throw new Exception("字段是空");
            }
            string sql = string.Format("select {0} from {1}", queryFieldStr, tableName);
            return SqlHelper.ExecuteDataTable(sql);
        }
        #endregion

        #region 2.组织XML

        private void GenetateXML(string tableName, IEnumerable<string> needFilterFieldList)
        {
            var needQueryFieldList = GetNeedQueryFieldList(tableName, needFilterFieldList);
            XDocument xDoc = new XDocument();
            //创建一个根节点，定义节点名字DataBase
            XElement xRoot = new XElement("DataBase");
            //加载到XML文档
            xDoc.Add(xRoot);

            //添加节点
            XElement XEleTable = new XElement("Table");
            xRoot.Add(XEleTable);
            //添加一个属性值，
            XAttribute xTableName = new XAttribute("tableName", tableName);
            XEleTable.Add(xTableName);

            XElement XEleFields = new XElement("Fields");
            XEleTable.Add(XEleFields);

            AddFieldNodeListToFieldsNode(XEleFields, tableName, needFilterFieldList);

            XElement XEleConfig = new XElement("Config");
            XEleTable.Add(XEleConfig);
            XElement XEleFieldOrder = new XElement("FieldOrder", string.Join(",", needQueryFieldList));
            XEleConfig.Add(XEleFieldOrder);

            #region 增加数据行节点

            AddDataRowListToXMLNode(XEleTable, tableName, needFilterFieldList);
            #endregion

            //保存文档
            xDoc.Save(@"d:\caoyi.xml");
        }

        private void GenetateXML(ExportDataConfig exportDataConfig)
        {
            using (Stream stream = File.Open(exportDataConfig.SaveFilePath, FileMode.OpenOrCreate))
            {
                XDocument xDoc = new XDocument();
                //创建一个根节点，定义节点名字DataBase
                XElement xRoot = new XElement("DataBase");
                //加载到XML文档
                xDoc.Add(xRoot);

                foreach (var item in exportDataConfig.ToBeExportedTableModelList)
                {
                    string tableName = item.TableName;
                    var needFilterFieldList = item.NeedFilterFieldList;

                    var needQueryFieldList = GetNeedQueryFieldList(tableName, needFilterFieldList);

                    //添加节点
                    XElement XEleTable = new XElement("Table");
                    xRoot.Add(XEleTable);
                    //添加一个属性值，
                    XAttribute xTableName = new XAttribute("tableName", tableName);
                    XEleTable.Add(xTableName);

                    XElement XEleFields = new XElement("Fields");
                    XEleTable.Add(XEleFields);

                    AddFieldNodeListToFieldsNode(XEleFields, tableName, needFilterFieldList);

                    XElement XEleConfig = new XElement("Config");
                    XEleTable.Add(XEleConfig);
                    XElement XEleFieldOrder = new XElement("FieldOrder", string.Join(",", needQueryFieldList));
                    XEleConfig.Add(XEleFieldOrder);

                    #region 增加数据行节点

                    AddDataRowListToXMLNode(XEleTable, tableName, needFilterFieldList);
                    #endregion
                }
                //保存文档
                xDoc.Save(stream);
            }


        }

        #endregion

        #region 2.1 增加field node 集合到
        /// <summary>
        /// 增加field node 集合到
        /// </summary>
        /// <param name="XEleFields"></param>
        /// <param name="tableName"></param>
        /// <param name="needFilterFieldList"></param>
        private void AddFieldNodeListToFieldsNode(XElement XEleFields, string tableName, IEnumerable<string> needFilterFieldList)
        {
            IEnumerable<TableField> lists = GetTableFieldList(tableName, needFilterFieldList);

            foreach (var item in lists)
            {
                XElement XEleField = new XElement("Field", item.FieldName);
                //添加一个属性值，
                XAttribute xTableName = new XAttribute("type", item.FieldType.ToLower());
                XEleField.Add(xTableName);
                XEleFields.Add(XEleField);
            }
        }

        #endregion

        #region 2.2 增加数据行节点

        private void AddDataRowListToXMLNode(XElement XEleTable, string tableName, IEnumerable<string> needFilterFieldList)
        {
            IEnumerable<string> needQueryFieldList;
            DataTable dt = GetDataTable(tableName, needFilterFieldList, out needQueryFieldList);
            XElement XDatas = new XElement("Datas");
            XEleTable.Add(XDatas);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                XElement XEleRow = new XElement("Row");
                XDatas.Add(XEleRow);
                foreach (var fieldItem in needQueryFieldList)
                {
                    var row = dt.Rows[i];
                    if (row.IsNull(fieldItem))
                    {
                        XElement XEleColumnData = new XElement("ColumnData", string.Empty);
                        XAttribute isNullAttr = new XAttribute("IsNull", true);
                        XEleColumnData.Add(isNullAttr);

                        XEleRow.Add(XEleColumnData);
                    }
                    else
                    {
                        //处理到了 xml中特殊字符的问题
                        XEleRow.Add(new XElement("ColumnData", System.Security.SecurityElement.Escape(row[fieldItem].ToString())));
                    }

                }
            }
        }

        #endregion

        #region 2.3 获取某张表中被查询的字段集合
        /// <summary>
        /// 获取某张表中被查询的字段集合
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="needFilterFieldList">需要被过滤的字段集合</param>
        /// <returns>获取某张表中被查询的字段集合</returns>
        private IEnumerable<string> GetNeedQueryFieldList(string tableName, IEnumerable<string> needFilterFieldList)
        {
            var needFilterFieldListLower = new List<string>();
            if (needFilterFieldList != null && needFilterFieldList.Any())
            {
                foreach (var item in needFilterFieldList)
                {
                    needFilterFieldListLower.Add(item.ToLower());
                }
            }

            var tableNameFieldList = SqlHelper.GetAllFieldNameListByTableName(tableName);
            var needQueryFieldList = tableNameFieldList.Where(c => !needFilterFieldListLower.Contains(c.ToLower()));
            return needQueryFieldList;
        }
        #endregion

        #region 2.4 获取某张表中被查询的字段和字段类型集合
        /// <summary>
        /// 2.4 获取某张表中被查询的字段和字段类型集合
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="needFilterFieldList">需要被过滤掉的字段结婚</param>
        /// <returns></returns>
        private IEnumerable<TableField> GetTableFieldList(string tableName, IEnumerable<string> needFilterFieldList)
        {
            var toQueryFieldList = GetNeedQueryFieldList(tableName, needFilterFieldList);
            var allTableFieldModelList = SqlHelper.GetAllFieldModelListByTableName(tableName);
            return allTableFieldModelList.Where(c => toQueryFieldList.Contains(c.FieldName));
        }
        #endregion
    }
}
