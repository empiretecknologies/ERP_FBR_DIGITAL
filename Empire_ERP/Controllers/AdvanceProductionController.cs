using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Core.Services;
using Empire_ERP.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Reporting.NETCore;
using System.Text;

namespace Empire_ERP.Controllers
{
    [CheckSession]
    [ExtractMenuCode]
    public class AdvanceProductionController : BaseController
    {
        public IAdvanceProductionService _AdvanceProductionService { get; set; }
        private readonly IWebHostEnvironment _hostingEnvironment;
        public IPeriodService _periodService { get; set; }
        public AdvanceProductionController(IAdvanceProductionService AdvanceProductionService, IMenuService menuService, IWebHostEnvironment hostingEnvironment, IBaseService baseService, IPeriodService periodService) : base(menuService, baseService)
        {
            _AdvanceProductionService = AdvanceProductionService;
            _hostingEnvironment = hostingEnvironment;
            _periodService = periodService;
        }

        public IActionResult Index()
        {
            var common = CommonHelper.GetValues(HttpContext);
            //ViewBag.Items = DropdownService.RawItemsDropdown();
            ViewBag.Items = DropdownService.ItemMasterDropdownWithUnits();
            ViewBag.Units = DropdownService.UnitDropdown();
            ViewBag.WastageItems = DropdownService.WastageItems();
            //ViewBag.FinishItems = DropdownService.FinishItemsDropdown();
            ViewBag.FinishItems = DropdownService.ItemMasterDropdownWithUnits();
            ViewBag.Processes = DropdownService.ProcessesDropdown();
            ViewBag.Permissions = common.RoleType == "A"
                ? "Admin"
                : CommonHelper.GetPermissionByMenueID(common.RoleID, common.MenuID);

            int compCond = 2;
            string compCondQuery = "SELECT CON_QTY FROM TBL_MENU_BUILDER WHERE DLT = 'T' AND ID = '" + common.MenuID + "'";
            using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
            {
                SqlCommand command = new SqlCommand(compCondQuery, connection);
                connection.Open();
                object result = command.ExecuteScalar();
                compCond = Convert.ToInt32(result);
            }
            ViewBag.CompCond = compCond;
            ViewBag.Warehouse = DropdownService.SubWareHouseDropdown();
            ViewBag.Lots = DropdownService.LotRegistrationDropdown();

            return View();
        }

        [HttpGet]
        public JsonResult GetCurrentStock()
        {
            try
            {
                var common = CommonHelper.GetValues(HttpContext);
                var periodInfo = _periodService.GetPeriodById(Convert.ToInt32(common.Period));
                var sdate = ((Period)periodInfo.data).START_D.Value.ToString("yyyy-MM-dd");
                var eDate = ((Period)periodInfo.data).START_E.Value.ToString("yyyy-MM-dd");
                var data = DropdownService.GetCurrentStock(sdate, eDate, common.Branch, common.Period);
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { msg = _catchMessage, msgType = 2 });
            }
        }


        [HttpGet]
        public JsonResult GetProcesses()
        {
            try
            {
                var data = DropdownService.ProcessesDropdown();
                return Json(new { data = data, msgType = 1 });
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { data = _catchMessage, msgType = 2 });
            }
        }

        [HttpGet]
        public JsonResult GetAdvanceProductions()
        {
            try
            {
                var data = _AdvanceProductionService.QuickSearch(CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { msg = _catchMessage, msgType = 2 });
            }
        }

        [HttpGet]
        public JsonResult GetReportTypes()
        {
            try
            {
                var data = _menuService.GetMenuDetails(CommonHelper.GetValues(HttpContext).MenuID);
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { data = _catchMessage, msgType = 2 });
            }
        }

        [HttpPost]
        public JsonResult Save(CustomAdvanceProduction modelRecord)
        {
            try
            {
                var data = _AdvanceProductionService.Save(modelRecord, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { msg = _catchMessage, msgType = 2 });
            }
        }

        [HttpPost]
        public JsonResult CopyRecord(CopyRecord record)
        {
            try
            {
                var data = _AdvanceProductionService.CopyRecord(record, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { msg = _catchMessage, msgType = 2 });
            }
        }

        [HttpGet]
        public JsonResult GetAdvanceProductionByCode(int code)
        {
            try
            {
                var data = _AdvanceProductionService.GetAdvanceProductionByCode(code, CommonHelper.GetValues(HttpContext));
                var detailData = _AdvanceProductionService.GetAdvanceProductionDetailByCode(code, CommonHelper.GetValues(HttpContext));
                return Json(new { Master = data, Detail = detailData });
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { msg = _catchMessage, msgType = 2 });
            }
        }

        [HttpGet]
        public JsonResult GetAdvanceProductionDetailByCode(int code)
        {
            try
            {
                var data = _AdvanceProductionService.GetAdvanceProductionDetailByCode(code, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { msg = _catchMessage, msgType = 2 });
            }
        }

        [HttpPost]
        public JsonResult Delete(int code)
        {
            try
            {
                var data = _AdvanceProductionService.Delete(code, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { msg = _catchMessage, msgType = 2 });
            }
        }

        [HttpPost]
        public JsonResult DeleteAdvanceProductionDetailByCode(int code)
        {
            try
            {
                var data = _AdvanceProductionService.DeleteAdvanceProductionDetailByCode(code, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(new { msg = _catchMessage, msgType = 2 });
            }
        }
        [HttpPost]
        public JsonResult GetPrintReport(AdvanceProductionRDLCReport model)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var filePath = GenerateReport(model);
                if (!String.IsNullOrEmpty(filePath))
                {
                    response.data = filePath;
                    response.msg = "";
                    response.msgType = 1;
                }
                else
                {
                    response.msg = "Unable to generate report. Please try again later.";
                    response.msgType = 2;
                }
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return Json(response);
        }
        private string GenerateReport(AdvanceProductionRDLCReport model)
        {
            var filePath = "";
            try
            {
                if (model != null && model.TRAN_ID > 0 && model.MD_ID > 0 && model.REPORT_NAME != null)
                {
                    Reports.Datasets.BarcodeReportDataset.AdvanceProductionDataTable reportDetails = new Reports.Datasets.BarcodeReportDataset.AdvanceProductionDataTable();
                    var responseMessage = _AdvanceProductionService.GetDataForReport(model, reportDetails, CommonHelper.GetValues(HttpContext));
                    if (responseMessage.msgType != 1)
                    {
                        return "";
                    }
                    var reportData = (CustomAdvanceProductionForPrintReport)responseMessage.data;

                    using (LocalReport report = new LocalReport())
                    {
                        var path = Path.Combine(_hostingEnvironment.ContentRootPath, @$"Reports\{reportData.Master?.REPORT_NAME}.rdlc");
                        using (var stReader = new StreamReader(path))
                        {
                            string stringreader = stReader.ReadToEnd();
                            byte[] byteArray = Encoding.UTF8.GetBytes(stringreader);
                            using (var stream = new MemoryStream(byteArray))
                            {
                                report.EnableExternalImages = true;
                                report.LoadReportDefinition(stream);
                                report.DataSources.Clear();
                                if (reportData.Master?.REPORT_NAME == "AdvanceProduction")
                                {
                                    var companyLogoPath = Path.Combine(_hostingEnvironment.WebRootPath, @$"Client\Company\{reportData.Master?.COMPANY_LOGO}");
                                    bool? showCompanyLogo = true;
                                    if (!System.IO.File.Exists(companyLogoPath))
                                    {
                                        showCompanyLogo = false;
                                    }
                                    ReportParameter parameter1 = new ReportParameter("Header", reportData.Master?.HEADER_NAME);
                                    ReportParameter parameter2 = new ReportParameter("InvoiceNumber", reportData.Master?.INVOICE_NUMBER);
                                    ReportParameter parameter3 = new ReportParameter("Date", reportData.Master?.DATE);
                                    ReportParameter parameter4 = new ReportParameter("Comment", reportData.Master?.REMARKS);
                                    ReportParameter parameter5 = new ReportParameter("Status", reportData.Master?.STATUS);
                                    ReportParameter parameter6 = new ReportParameter("Sig1", reportData.Master?.SIG1);
                                    ReportParameter parameter7 = new ReportParameter("Sig2", reportData.Master?.SIG2);
                                    ReportParameter parameter8 = new ReportParameter("Sig3", reportData.Master?.SIG3);
                                    ReportParameter parameter9 = new ReportParameter("Sig4", reportData.Master?.SIG4);
                                    ReportParameter parameter10 = new ReportParameter("CompanyName", reportData.Master?.COMPANY_NAME);
                                    ReportParameter parameter11 = new ReportParameter("CompanyAddress", reportData.Master?.COMPANY_ADDRESS);
                                    ReportParameter parameter12 = new ReportParameter("CompanyPhone", reportData.Master?.COMPANY_PHONE);
                                    ReportParameter parameter13 = new ReportParameter("User", reportData.Master?.USER);
                                    ReportParameter parameter14 = new ReportParameter("BranchName", reportData.Master?.B_NAME);
                                    ReportParameter parameter15 = new ReportParameter("BranchTerms", reportData.Master?.B_TERMS);
                                    ReportParameter parameter16 = new ReportParameter("BranchWebsite", reportData.Master?.B_WEBSITE);
                                    ReportParameter parameter17 = new ReportParameter("BranchEmail", reportData.Master?.EMAIL);
                                    ReportParameter parameter18 = new ReportParameter("BranchGST", reportData.Master?.B_GST);
                                    ReportParameter parameter19 = new ReportParameter("BranchNTN", reportData.Master?.B_NTN);
                                    ReportParameter parameter20 = new ReportParameter("MenuTerms", reportData.Master?.MENU_TERMS);
                                    ReportParameter parameter21 = new ReportParameter("ShowCompanyLogo", Convert.ToString(showCompanyLogo));
                                    ReportParameter parameter22 = new ReportParameter("CompanyLogo", new Uri(Path.Combine(_hostingEnvironment.WebRootPath, @$"Client\Company\{reportData.Master?.COMPANY_LOGO}")).AbsoluteUri);
                                    ReportParameter parameter23 = new ReportParameter("Ref", reportData.Master?.REF);
                                    ReportParameter parameter24 = new ReportParameter("RawItem", reportData.Master?.RAW_ITEM);
                                    ReportParameter parameter25 = new ReportParameter("Warehouse", reportData.Master?.WAREHOUSE);
                                    ReportParameter parameter26 = new ReportParameter("Lot", reportData.Master?.LOT);
                                    ReportParameter parameter27 = new ReportParameter("Qty", reportData.Master?.BQTY);
                                    ReportParameter parameter28 = new ReportParameter("Wastage", Convert.ToString(reportData.Master?.WASTAGE));
                                    ReportParameter parameter29 = new ReportParameter("WastageQty", Convert.ToString(reportData.Master?.WASTAGE_QTY));
                                    ReportParameter parameter30 = new ReportParameter("WastageItem", reportData.Master?.WASTAGE_ITEM);



                                    report.SetParameters(new ReportParameter[] {  parameter1, parameter2, parameter3, parameter4, parameter5, 
                                        parameter6, parameter7, parameter8, parameter9, parameter10, 
                                        parameter11, parameter12, parameter13, parameter14, parameter15, 
                                        parameter16, parameter17, parameter18, parameter19, parameter20, 
                                        parameter21, parameter22, parameter23, parameter24, parameter25, 
                                        parameter26, parameter27, parameter28, parameter29, parameter30 });
                                    report.Refresh();
                                }


                                report.DataSources.Add(new ReportDataSource() { Name = "AdvanceProduction", Value = reportData.Detail });

                                byte[] file;
                                string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, @"Client\AdvanceProduction");
                                if (!Directory.Exists(uploadsFolder))
                                {
                                    Directory.CreateDirectory(uploadsFolder);
                                }

                                if (report.IsReadyForRendering)
                                {
                                    string input = reportData.Master?.INVOICE_NUMBER;
                                    string[] parts = input.Split('/');
                                    string prefix = string.Empty;
                                    string voucherNumber = string.Empty;
                                    if (parts.Length >= 3)
                                    {
                                        prefix = parts[1];
                                        voucherNumber = parts[^1];
                                    }
                                    file = report.Render("PDF");
                                    filePath = $"{prefix} - {(reportData.Master?.RAW_ITEM).Replace(" / ", " - ")} - {voucherNumber}" + ".pdf";

                                    stReader.Close();
                                    stReader.Dispose();
                                    stream.Flush();
                                    stream.Close();
                                    stream.Dispose();
                                    report.Dispose();
                                    string reportPath = Path.Combine(uploadsFolder, filePath);
                                    System.IO.File.WriteAllBytes(reportPath, file);
                                    filePath = $"/Client/AdvanceProduction/{filePath}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
            }
            return filePath;
        }
    }
}