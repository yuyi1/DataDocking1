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
    public class ImportDataFromXMLHelper
    {
        #region 1. +GenerateDataTableFromXML(ImportDataConfig importConfigModel) 【通过xml生成DataTable】
        /// <summary>
        /// 通过xml生成DataTable
        /// </summary>
        /// <param name="importConfigModel">导入时候的配置参数</param>
        /// <returns>DataTable集合</returns>
        public List<DataTable> GenerateDataTableFromXML(ImportDataConfig importConfigModel)
        {
            List<DataTable> resList = new List<DataTable>();
            XDocument xDoc = XDocument.Load(importConfigModel.SaveFilePath);
            //获取根节点
            XElement xRoot = xDoc.Root;
            if (xRoot == null)
            {
                ThrowException("根节点不能为空");
            }

            foreach (var xTable in xRoot.Elements("Table"))
            {
                if (xTable == null)
                {
                    ThrowException("Table节点不能为空");
                }
                var tableName = xTable.Attribute("tableName").Value;
                var xFields = xTable.Element("Fields");
                var xConfig = xTable.Element("Config");
                var xDatas = xTable.Element("Datas");

                var columnNameAndTypeDic = GetColumnNameAndTypeDic(xFields);
                DataTableHelper dtHelper = new DataTableHelper();

                var orderColumnNameList = GetOrderColumnNameList(xConfig);
                var datas = GetDataFromXMLNode(xDatas, orderColumnNameList);
                resList.Add(dtHelper.GenerateDataTable(columnNameAndTypeDic, datas, tableName));
            }
            return resList;
        }
        #endregion

        #region 2. -Dictionary<string, string> GetColumnNameAndTypeDic(XElement xFields) 【获取列名和对应的字段类型 字典】
        /// <summary>
        /// 获取列名和对应的字段类型 字典
        /// </summary>
        /// <param name="xFields">字段xmlNode</param>
        /// <returns>字典</returns>
        private Dictionary<string, string> GetColumnNameAndTypeDic(XElement xFields)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            var xFieldList = xFields.Elements("Field");

            foreach (var item in xFieldList)
            {
                var value = item.Value;
                var typeAttr = item.Attribute("type");
                var typeAttrVal = typeAttr != null ? typeAttr.Value : string.Empty;
                typeAttrVal = GetRelationCsharpTypeFromDbType(typeAttrVal);
                dic.Add(value, typeAttrVal);
            }
            return dic;
        }
        #endregion

        #region 3. -GetDataFromXMLNode(XElement xDatas, IEnumerable<string> orderColumnNameList) 【从xml中读取需要被插入到数据库的数据】
        /// <summary>
        /// 从xml中读取需要被插入到数据库的数据
        /// </summary>
        /// <param name="xDatas">数据节点</param>
        /// <param name="orderColumnNameList">排序的列名集合</param>
        /// <returns>一行行的数据 【key:列名 value:是对应的值】</returns>
        private List<Dictionary<string, object>> GetDataFromXMLNode(XElement xDatas, IEnumerable<string> orderColumnNameList)
        {
            List<Dictionary<string, object>> resList = new List<Dictionary<string, object>>();
            var rowList = xDatas.Elements("Row");

            foreach (var row in rowList)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                var columnDataList = row.Elements("ColumnData");

                for (int i = 0; i < columnDataList.Count(); i++)
                {
                    var orderColumnName = orderColumnNameList.ElementAt(i);
                    var tempColumnCell = columnDataList.ElementAt(i);
                    var objVal = tempColumnCell.Value;
                    //如果某个节点是空的话，那么需要判断是null或者""
                    if (string.IsNullOrEmpty(objVal))
                    {
                        var isNullAttr = tempColumnCell.Attribute("IsNull");
                        if (isNullAttr == null)
                        {
                            dic.Add(orderColumnName, string.Empty);
                        }
                        else if (isNullAttr.Value.ToLower() == "true")//有“IsNull”这个属性，那么导入的就是null
                        {
                            dic.Add(orderColumnName, DBNull.Value);
                        }
                        else
                        {
                            dic.Add(orderColumnName, string.Empty);
                        }
                    }
                    else
                    {
                        dic.Add(orderColumnName, objVal);
                    }
                }
                resList.Add(dic);
            }
            return resList;

        }
        #endregion

        #region 4. -GetOrderColumnNameList(XElement xConfig) 【从xml中读取被插入到数据时候字段的顺序】
        /// <summary>
        /// 从xml中读取被插入到数据时候字段的顺序
        /// </summary>
        /// <param name="xConfig">配置文件节点</param>
        /// <returns>字段顺序集合</returns>
        private IEnumerable<string> GetOrderColumnNameList(XElement xConfig)
        {
            var xFieldOrder = xConfig.Element("FieldOrder");
            if (xFieldOrder == null)
            {
                ThrowException("FieldOrder不能为空");
            }
            var columnNameList = xFieldOrder.Value;
            return columnNameList.Split(',');

        }
        #endregion

        #region 5. -GetRelationCsharpTypeFromDbType(string dbTypeVal) 【【数据库中数据类型】 对应到 【CSharp中的字段类型】】
        /// <summary>
        /// 【数据库中数据类型】 对应到 【CSharp中的字段类型】
        /// </summary>
        /// <param name="dbTypeVal">数据中的字段类型</param>
        /// <returns>CSharp中的字段类型</returns>
        private string GetRelationCsharpTypeFromDbType(string dbTypeVal)
        {
            string res = string.Empty;
            switch (dbTypeVal)
            {
                #region 类型对应
                case "bigint":
                    res = "System.Int64";
                    break;
                case "int":
                    res = "System.Int32";
                    break;
                case "smallint":
                    res = "System.Int16";
                    break;
                case "tinyint":
                    res = "System.Byte";
                    break;
                case "bit":
                    res = "System.Boolean";
                    break;
                case "decimal":
                    res = "System.Decimal";
                    break;
                case "numeric":
                    res = "System.Decimal";
                    break;
                case "money":
                    res = "System.Decimal";
                    break;
                case "smallmoney":
                    res = "System.Decimal";
                    break;
                case "float":
                    res = "System.Double";
                    break;

                case "datetime":
                    res = "System.DateTime";
                    break;
                case "char":
                    res = "System.String";
                    break;
                case "varchar":
                    res = "System.String";
                    break;
                case "text":
                    res = "System.String";
                    break;

                case "nchar":
                    res = "System.String";
                    break;
                case "nvarchar":
                    res = "System.String";
                    break;
                case "ntext":
                    res = "System.String";
                    break;
                case "binary":
                    res = "System.Byte[]";
                    break;
                case "varbinary":
                    res = "System.Byte[]";
                    break;
                case "image":
                    res = "System.Byte[]";
                    break;
                case "timestamp":
                    res = "System.DateTime";
                    break;
                case "uniqueidentifier":
                    res = "System.Guid";
                    break;
                #endregion
            }
            return res;
        }
        #endregion

        #region 6. -ThrowException(string exceptionMsg) 【抛出异常】
        /// <summary>
        /// 抛出异常
        /// </summary>
        /// <param name="exceptionMsg">被抛出的异常的提示信息</param>
        private void ThrowException(string exceptionMsg)
        {
            throw new Exception(exceptionMsg);
        }
        #endregion
    }
}
