using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataDockingHelper
{ 
    /// <summary>
    /// 在导出表时候，需要传递的配置参数
    /// </summary>
    public class ExportDataConfig
    {
        /// <summary>
        /// 1.xml文件被保存的路径
        /// </summary>
        public string SaveFilePath { get; set; }

        /// <summary>
        /// 2.将要被导出的表模型集合
        /// </summary>
        public IEnumerable<ExportTableModel> ToBeExportedTableModelList { get; set; }
    }

    /// <summary>
    /// 需要被导出的表有哪些
    /// </summary>
    public class ExportTableModel
    {
        /// <summary>
        /// 1.表名
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 2.需要被过滤掉的字段集合
        /// 提示：当没有需要被过滤的字段的时候，可以设置为null，或者空的集合
        /// </summary>
        public IEnumerable<string> NeedFilterFieldList { get; set; }
    }

}
