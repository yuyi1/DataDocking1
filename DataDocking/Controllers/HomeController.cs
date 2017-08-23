using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DataDockingHelper;

namespace DataDocking.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            #region 1.导出数据库中的数据到XML
            ExportDataToXMLHelper exportDataHelper = new ExportDataToXMLHelper();

            ExportDataConfig exportDataConfig = new ExportDataConfig
            {
                SaveFilePath = @"d:\caoyi.xml",
                ToBeExportedTableModelList = new List<ExportTableModel>()
                {
                    new ExportTableModel() {TableName = "A1"},
                    new ExportTableModel() {TableName = "Table_1"}
                }
            };

            exportDataHelper.ExportDataToXml(exportDataConfig);
            #endregion

            return View();
        }

        public ActionResult Index2()
        {
            #region 导入xml的数据到数据库
            ImportDataFromXMLHelper helper = new ImportDataFromXMLHelper();

            ImportDataConfig importConfigModel = new ImportDataConfig()
            {
                SaveFilePath = @"d:\caoyi.xml"
            };
            var dtList = helper.GenerateDataTableFromXML(importConfigModel);
            SqlHelper.SqlBulkCopyByMutiDataTable(dtList);
            #endregion

            return View();
        }
    }
}
