using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Core.Services;
using Empire_ERP.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Reporting.NETCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static Azure.Core.HttpHeader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Empire_ERP.Controllers
{
    [CheckSession]
    [ExtractMenuCode]
    public class PurchaseBillController : BaseController
    {
        public IPurchaseBillService _purchaseBillService { get; set; }
        private readonly IWebHostEnvironment _hostingEnvironment;
        public IPeriodService _periodService { get; set; }

        public PurchaseBillController(IPeriodService periodService, IPurchaseBillService PurchaseBillService, IMenuService menuService, IWebHostEnvironment hostingEnvironment, IBaseService baseService) : base(menuService, baseService)
        {
            _purchaseBillService = PurchaseBillService;
            _hostingEnvironment = hostingEnvironment;
            _periodService = periodService;
        }

        public IActionResult Index()
        {
            var common = CommonHelper.GetValues(HttpContext);
            ViewBag.DateTime = CommonService.GetDateTime("Pakistan Standard Time");
            var BranchID = HttpContext.Session.GetString("Branch");
            var CompanyID = HttpContext.Session.GetString("Company");
            string nextId = "";
            string formType = "";
            int compCond = 2;
            string dcType = "";
            string prifix = "";

            var Menu = _menuService.GetMenu(common.MenuID);
            int? pType = 0; string? itemType = string.Empty;
            if (Menu.data != null)
            {
                var menu = (Menu)Menu.data;
                pType = menu.PTYPE;
                itemType = menu.ITEM_TYPE;
                dcType = menu.DCTYPE;
                prifix = menu.PERFIX;
            }
            var period = common.Period;
            var periodInfo = _periodService.GetPeriodById(Convert.ToInt32(period));

            var StartDate = ((Period)periodInfo.data).START_D.Value.ToString("yyyy-MM-dd");
            var EndDate = ((Period)periodInfo.data).START_E.Value.ToString("yyyy-MM-dd");

            ViewBag.Items = DropdownService.ItemMasterDropdownWithPrice(common.RoleID, common.RoleType, dcType, itemType);
            ViewBag.dcType = dcType;
            ViewBag.prifix = prifix;
            ViewBag.Units = DropdownService.UnitDropdown();
            ViewBag.Warehouse = DropdownService.WareHouseDropdownWthSubWithGr();
            ViewBag.Lots = DropdownService.LotRegistrationDropdown();
            ViewBag.AllCharges = DropdownService.AllCharges();
            ViewBag.DefaultCharges = DropdownService.DefaultCharges(common.MenuID);
            ViewBag.fbrType = DropdownService.FBRTypeDropdown();
            ViewBag.PartyType = DropdownService.PartyTypeWithpType(StartDate, EndDate, common);
            ViewBag.Permissions = common.RoleType == "A"
                ? "Admin"
                : CommonHelper.GetPermissionByMenueID(common.RoleID, common.MenuID);
            ViewBag.Limit = CommonHelper.GetLimitByMenueID(common.MenuID);
            var response = _menuService.GetMenu(common.MenuID);
            if (response.msgType == 1)
            {
                ViewBag.DATA_CLEAR = ((Menu)response.data).DATA_CLEAR;
                ViewBag.stkStatus = ((Menu)response.data).STK_STATUS;
                ViewBag.FormType = ((Menu)response.data).PICK_TYPE;
                ViewBag.Type = ((Menu)response.data).B_I;
                ViewBag.CompCond = null;
            }
            return View();
        }

        [HttpGet]
        public JsonResult GetPartyCurrentBalance(string vDate)
        {
            try
            {
                var common = CommonHelper.GetValues(HttpContext);
                var periodInfo = _periodService.GetPeriodById(Convert.ToInt32(common.Period));
                var StartDate = ((Period)periodInfo.data).START_D.Value.ToString("yyyy-MM-dd");
                var data = DropdownService.PartyTypeWithpType(StartDate, vDate, common);
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
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
        public JsonResult GetParties()
        {
            try
            {
                var common = CommonHelper.GetValues(HttpContext);
                var data = common.RoleType == "A"
                    ? DropdownService.PartyTypeDropdownForInvoice(0, common.Branch, 0)
                    : DropdownService.PartyTypeDropdownForInvoice(common.RoleID, common.Branch, common.ShowSelected);
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
        public JsonResult GetUnits()
        {
            try
            {
                var data = DropdownService.UnitDropdown();
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetColors()
        {
            try
            {
                var data = DropdownService.ColorDropdown();
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetSizes()
        {
            try
            {
                var data = DropdownService.SizeDropdown();
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetGrades()
        {
            try
            {
                var data = DropdownService.GradeDropdown();
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetWarehouses()
        {
            try
            {
                var data = DropdownService.WareHouseDropdown();
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetDepartments()
        {
            try
            {
                var data = DropdownService.DepartmentDropdownWithControlName();
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetPurchaseBill()
        {
            try
            {
                var data = _purchaseBillService.QuickSearch(CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetPurchaseBillByCode(int code)
        {
            try
            {
                var common = CommonHelper.GetValues(HttpContext);

                var data = _purchaseBillService.GetPurchaseBillByCode(code, CommonHelper.GetValues(HttpContext));
                var detailData = _purchaseBillService.GetPurchaseBillDetailByCode(code, CommonHelper.GetValues(HttpContext));
                var chargesData = _purchaseBillService.GetPurchaseBillChargesByCode(code, CommonHelper.GetValues(HttpContext));
                var DefaultCharges = DropdownService.DefaultCharges(common.MenuID);

                var invoices = JArray.FromObject(data.data);

                string fbrNo = invoices.FirstOrDefault()?["FBR_NO"]?.ToString();

                var qrCode = GenerateQrCode(fbrNo);

                return Json(new { Master = data, Detail = detailData, charges = chargesData, DefaultCharges = DefaultCharges, qrcode = qrCode });
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetChargesByCode(int code)
        {
            try
            {
                var common = CommonHelper.GetValues(HttpContext);

                var chargesData = _purchaseBillService.GetPurchaseBillChargesByCode(code, CommonHelper.GetValues(HttpContext));
                var DefaultCharges = DropdownService.DefaultCharges(common.MenuID);
                return Json(new { charges = chargesData, DefaultCharges = DefaultCharges });
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetPurchaseBillDetailByCode(int code)
        {
            try
            {
                var data = _purchaseBillService.GetPurchaseBillDetailByCode(code, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpPost]
        public JsonResult Save(CustomPurchaseBill modelRecord)
        {
            try
            {
                var data = _purchaseBillService.Save(modelRecord, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpPost]
        public JsonResult SaveCharges(ChargesModel modelRecord)
        {
            try
            {
                var data = _purchaseBillService.SaveCharges(modelRecord, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpPost]
        public JsonResult Delete(int code)
        {
            try
            {
                var data = _purchaseBillService.Delete(code, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult DeleteCharges(int codee, int tranId)
        {
            try
            {
                var data = _purchaseBillService.DeleteCharges(codee, tranId, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpPost]
        public JsonResult CopyRecord(CopyRecord record)
        {
            try
            {
                var data = _purchaseBillService.CopyRecord(record, CommonHelper.GetValues(HttpContext));
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
        public JsonResult DeletePurchaseBillDetailByCode(int code)
        {
            try
            {
                var data = _purchaseBillService.DeletePurchaseBillDetailByCode(code, CommonHelper.GetValues(HttpContext));
                return Json(data);
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                return Json(_catchMessage);
            }
        }

        [HttpGet]
        public JsonResult GetPickDataByParty(int partyCode, int actCode)
        {
            try
            {
                var data = _purchaseBillService.GetPickDataByParty(partyCode, actCode, CommonHelper.GetValues(HttpContext));
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


        //[HttpGet]
        //public MyHttpResponseMessage GetSalesmanByParty(int Id)
        //{
        //    MyHttpResponseMessage response = new MyHttpResponseMessage();
        //    string nextId = "";
        //    string maxIdQuery = "SELECT SACT_CODE FROM TBL_PARTY_TYPES WHERE  PARTY_CODE = '" + Id + "'";
        //    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
        //    {
        //        SqlCommand command = new SqlCommand(maxIdQuery, connection);
        //        connection.Open();
        //        object result = command.ExecuteScalar();
        //        nextId = Convert.ToString(result);
        //    }
        //    var companies = FetchSalesman(nextId);
        //    List<PartyTypes> companyList = new List<PartyTypes>();
        //    if (companies.Tables[0].Rows.Count > 0)
        //    {
        //        foreach (DataRow Row in companies.Tables[0].Rows)
        //        {
        //            PartyTypes company = new PartyTypes();
        //            company.PARTY_CODE = Convert.ToInt32(Row["PARTY_CODE"]);
        //            company.PARTY_NAME = Convert.ToString(Row["PARTY_NAME"]);
        //            companyList.Add(company);
        //        }
        //    }
        //    response.data = companies;
        //    response.msg = "";
        //    response.msgType = 1;
        //    return response;
        //}

        //public DataSet FetchSalesman(string Id)
        //{
        //    string query = string.Empty;
        //    if (Id == "")
        //    {
        //        query = "SELECT PARTY_CODE,PARTY_NAME FROM TBL_PARTY_TYPES WHERE PARTY_TYPE_CODE = 9 AND DLT = 'T' AND ASTATUS = 'Y'";
        //    }
        //    else
        //    {
        //        query = "SELECT PARTY_CODE,PARTY_NAME FROM TBL_PARTY_TYPES WHERE PARTY_TYPE_CODE = 9 AND DLT = 'T' AND ASTATUS = 'Y' AND ACT_CODE = '" + Id + "'";
        //    }
        //    DataSet data = SqlHelper.ExecuteDataset(new SQLService().getconnstring(), CommandType.Text, query);
        //    return data;
        //}

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
        [HttpGet]
        public async Task<IActionResult> GetDataForApi(int code)
        {
            try
            {
                var masterResponse = _purchaseBillService.GetDataForApi(code, CommonHelper.GetValues(HttpContext));

                var masterList = masterResponse?.data as List<SaleTaxInvoice>;
                if (masterList == null || !masterList.Any())
                    return Json(new { msg = "No data found", msgType = 2 });

                var master = masterList.First();

                var detailData = _purchaseBillService.GetDetailDataForApi(code, CommonHelper.GetValues(HttpContext));

                var items = new List<FBRItemModel>();

                foreach (var d in detailData)
                {
                    var item = GetFBRDetail(d,master.SCENARIO_ID);

                    items.Add(item);
                }

                var model = new FBRModel
                {
                    FBRUrlToken = new FBRUrlToken
                    {
                        Url = master.FBR_URL,
                        Token = master.TOKEN
                    },
                    FBRPostModel = new FBRPostModel
                    {
                        invoiceType = "Sale Invoice",
                        invoiceDate = master.INVOICE_DATE,
                        sellerNTNCNIC = master.SELLER_NTN,
                        sellerBusinessName = master.SELLER_BNAME,
                        sellerProvince = master.SELLER_PROVINCE,
                        sellerAddress = master.SELLER_ADDRESS,
                        buyerNTNCNIC = master.BUYER_NTN,
                        buyerBusinessName = master.BUYER_BNAME,
                        buyerProvince = master.BUYER_PROVINCE,
                        buyerAddress = master.BUYER_ADDRESS,
                        //buyerRegistrationType = master.BUYER_REG_TYPE,
                        buyerRegistrationType = master.REG_TYPE,
                        invoiceRefNo = master.SCENARIO_ID == "SN006" || master.SCENARIO_ID == "SN024" ? master.REF : "",
                        scenarioId = master.SCENARIO_ID,
                        items = items
                    }
                };

                var finalJson = JsonConvert.SerializeObject(model.FBRPostModel, Formatting.Indented);

                //System.IO.File.WriteAllText(@"D:\fbr_request.json", finalJson);

                var fbrResult = await PostToFBR(model,finalJson);

                dynamic fbrData = JsonConvert.DeserializeObject<dynamic>((fbrResult as ContentResult).Content);

                return Json(new
                {
                    msgType = 1,
                    msg = "FBR response retrieved",
                    response = fbrData.response,
                    qrCode = fbrData.qrCode
                });
            }
            catch (Exception ex)
            {
                return Json(new { msg = ex.Message, msgType = 2 });
            }
        }
        [HttpGet]
        public JsonResult FBRApi_Status(string code, string apiResponce)
        {
            var data = _purchaseBillService.FBRApi_Status(code, apiResponce, CommonHelper.GetValues(HttpContext));
            return Json(data);
        }
        private FBRItemModel GetFBRDetail(dynamic d,string scenarioId)
        {
            var detail = new FBRItemModel{};
            decimal taxRate = ParseFbrTaxRate(detail.rate);
            decimal inclusiveTotal = GetInclusiveTotalValues(d);
            var tax = Convert.ToString(d.TAX);
            switch (scenarioId)
            {
                case "SN001":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN002":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN003":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN004":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN005":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = 0;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN006":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN007":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN008":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = 0;
                    detail.valueSalesExcludingST = d.W_AMT;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN009":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN010":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN011":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN012":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN013":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN014":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN015":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN016":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN017":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                case "SN024":
                    detail.rate = $"{tax}%";
                    detail.hsCode = d.HS_CODE;
                    detail.productDescription = d.ITEM_NAME;
                    detail.uoM = d.UOM;
                    detail.quantity = d.QTY;
                    detail.totalValues = d.TOTAL_VALUES;
                    detail.valueSalesExcludingST = d.TOTAL_VALUES;
                    detail.fixedNotifiedValueOrRetailPrice = d.TOTAL_VALUES;
                    detail.salesTaxApplicable = d.TAX_AMT;
                    detail.salesTaxWithheldAtSource = 0;
                    detail.extraTax = "";
                    detail.furtherTax = 0;
                    detail.fedPayable = 0;
                    detail.discount = 0;
                    detail.sroScheduleNo = d.SRO_SCH_NO;
                    detail.saleType = d.S_NAME;
                    detail.sroItemSerialNo = d.SERIAL_NO?.ToString();
                    break;

                default:
                    throw new Exception(
                        $"Invalid FBR Scenario ID: {scenarioId}"
                    );
            }

            return detail;
        }
        private decimal ParseFbrTaxRate(string rate)
        {
            if (string.IsNullOrWhiteSpace(rate))
                return 0;

            var match = Regex.Match(rate, @"[\d.]+");
            if (!match.Success)
                return 0;

            decimal.TryParse(
                match.Value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal taxRate
            );

            return taxRate;
        }
        private decimal GetInclusiveTotalValues(dynamic d)
        {
            decimal quantity = d.QTY ?? 0;
            decimal unitRate = 0;

            decimal.TryParse(
                Convert.ToString(d.RATE),
                out unitRate
            );

            return Math.Round(quantity * unitRate, 2);
        }
        private decimal CalculateValueSalesExcludingST(dynamic d, decimal taxRate = 0)
        {
            decimal totalValues = GetInclusiveTotalValues(d);

            if (taxRate <= 0)
                return totalValues;

            decimal valueExcludingST =
                totalValues / (1 + (taxRate / 100));

            return Math.Round(valueExcludingST, 2);
        }
        private decimal CalculateSalesTax(decimal inclusiveTotal, decimal taxRate)
        {
            return inclusiveTotal * taxRate / (100 + taxRate);
        }
        private decimal CalculateFED(dynamic d, decimal fedRate = 0)
        {
            if (fedRate <= 0)
                return 0;

            decimal quantity = d.QTY ?? 0;

            decimal rate = 0;

            decimal.TryParse(
                Convert.ToString(d.RATE),
                out rate
            );

            decimal totalValue = quantity * rate;

            decimal valueExcludingST =
                CalculateValueSalesExcludingST(
                    totalValue,
                    18
                );

            decimal fed =
                valueExcludingST * fedRate / 100;

            return Math.Round(fed, 2);
        }


        [HttpPost]
        public JsonResult GetPrintReport(PurchaseBillRDLCReport model)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var filePath = GenerateReport(model);
                if (!string.IsNullOrEmpty(filePath))
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


        private string GenerateReport(PurchaseBillRDLCReport model)
        {
            var filePath = "";
            try
            {
                if (model != null && model.TRAN_ID > 0 && model.MD_ID > 0 && model.REPORT_NAME != null)
                {
                    Reports.Datasets.BarcodeReportDataset.SaleTaxInvoiceDataTable reportDetails = new Reports.Datasets.BarcodeReportDataset.SaleTaxInvoiceDataTable();
                    var responseMessage = _purchaseBillService.GetDataForReport(model, reportDetails, CommonHelper.GetValues(HttpContext));
                    if (responseMessage.msgType != 1)
                    {
                        return "";
                    }
                    var reportData = (CustomPurchaseBillForPrintReport)responseMessage.data;

                    using (LocalReport report = new LocalReport())
                    {
                        var path = Path.Combine(_hostingEnvironment.ContentRootPath, @$"Reports\{reportData.Master?.REPORT_NAME}.rdlc");
                        var stReader = new StreamReader(path);
                        string stringreader = stReader.ReadToEnd();
                        byte[] byteArray = Encoding.UTF8.GetBytes(stringreader);
                        MemoryStream stream = new MemoryStream(byteArray);
                        report.EnableExternalImages = true;
                        report.LoadReportDefinition(stream);
                        report.DataSources.Clear();

                        if (reportData.Master?.REPORT_NAME == "SaleTaxInvoice")
                        {
                            var companyLogoPath = Path.Combine(_hostingEnvironment.WebRootPath, @$"Client\Company\{reportData.Master?.COMPANY_LOGO}");
                            bool? showCompanyLogo = true;

                            if (!System.IO.File.Exists(companyLogoPath))
                            {
                                showCompanyLogo = false;
                            }

                            string qrRelativePath = GenerateQrCodeZXingToFile(reportData.Master?.FBR_NO);
                            string qrFullPath = Path.Combine(_hostingEnvironment.WebRootPath, qrRelativePath.TrimStart('/'));
                            ReportParameter parameter23 = new ReportParameter("InvoiceQR", new Uri(qrFullPath).AbsoluteUri);

                            //string qrValue = null;
                            //if (!string.IsNullOrEmpty(reportData.Master?.FBR_NO))
                            //{
                            //    string qrRelativePath = GenerateQrCodeZXingToFile(reportData.Master?.FBR_NO);
                            //    if (!string.IsNullOrEmpty(qrRelativePath))
                            //    {
                            //        string qrFullPath = Path.Combine(_hostingEnvironment.WebRootPath, qrRelativePath.TrimStart('/'));
                            //        qrValue = new Uri(qrFullPath).AbsoluteUri;
                            //    }
                            //}
                            //ReportParameter parameter23 = new ReportParameter("InvoiceQR", qrValue);

                            ReportParameter parameter1 = new ReportParameter("Header", reportData.Master?.HEADER_NAME);
                            ReportParameter parameter2 = new ReportParameter("InvoiceNumber", reportData.Master?.INVOICE_NUMBER);
                            ReportParameter parameter3 = new ReportParameter("Date", reportData.Master?.DATE);
                            ReportParameter parameter4 = new ReportParameter("CompanyName", reportData.Master?.COMPANY_NAME);
                            ReportParameter parameter5 = new ReportParameter("BType", reportData.Master?.BTYPE);

                            ReportParameter parameter6 = new ReportParameter("Party", reportData.Master?.PARTY_NAME);
                            ReportParameter parameter7 = new ReportParameter("PAddress", reportData.Master?.PADDRESS);
                            ReportParameter parameter8 = new ReportParameter("Tell", reportData.Master?.TELL);
                            ReportParameter parameter9 = new ReportParameter("PTNtn", reportData.Master?.PT_NTN);
                            ReportParameter parameter10 = new ReportParameter("CName", reportData.Master?.C_NAME);

                            ReportParameter parameter11 = new ReportParameter("BAddress", reportData.Master?.B_ADDRESS);
                            ReportParameter parameter12 = new ReportParameter("BTell", reportData.Master?.B_TEL);
                            ReportParameter parameter13 = new ReportParameter("BNTN", reportData.Master?.B_NTN);
                            ReportParameter parameter14 = new ReportParameter("STRN", reportData.Master?.STRN);
                            ReportParameter parameter15 = new ReportParameter("FBRNo", reportData.Master?.FBR_NO);

                            ReportParameter parameter16 = new ReportParameter("Sig1", reportData.Master?.SIG1);
                            ReportParameter parameter17 = new ReportParameter("Sig2", reportData.Master?.SIG2);
                            ReportParameter parameter18 = new ReportParameter("Sig3", reportData.Master?.SIG3);
                            ReportParameter parameter19 = new ReportParameter("Sig4", reportData.Master?.SIG4);
                            ReportParameter parameter20 = new ReportParameter("MenuTerms", reportData.Master?.MENU_TERMS);

                            ReportParameter parameter21 = new ReportParameter("ShowCompanyLogo", Convert.ToString(showCompanyLogo));
                            ReportParameter parameter22 = new ReportParameter("CompanyLogo", new Uri(Path.Combine(_hostingEnvironment.WebRootPath, @$"Client\Company\{reportData.Master?.COMPANY_LOGO}")).AbsoluteUri);
                            ReportParameter parameter24 = new ReportParameter("FBRLogo", new Uri(Path.Combine(_hostingEnvironment.WebRootPath, @$"Client\Company\FBRLogo.png")).AbsoluteUri);
                            ReportParameter parameter25 = new ReportParameter("BWeb", reportData.Master?.B_WEBSITE);
                            ReportParameter parameter26 = new ReportParameter("BEmail", reportData.Master?.EMAIL);
                            ReportParameter parameter28 = new ReportParameter("PT_CNIC", reportData.Master?.PT_CNIC);
                            ReportParameter parameter29 = new ReportParameter("Ref", reportData.Master?.REF);

                            string fbrNo = reportData.Master?.FBR_NO ?? string.Empty;

                            //bool isValidFbrNo = !string.IsNullOrWhiteSpace(fbrNo) && Regex.IsMatch(fbrNo, "^[A-Z0-9]{21}$");
                            bool isValidFbrNo = !string.IsNullOrWhiteSpace(fbrNo) && Regex.IsMatch(fbrNo, @"^\S+$");
                            ReportParameter parameter27 = new ReportParameter("IsVerified", Convert.ToString(isValidFbrNo));

                            report.SetParameters(new ReportParameter[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8, parameter9,
                            parameter10, parameter11, parameter12, parameter13, parameter14, parameter15, parameter16, parameter17, parameter18, parameter19, parameter20, parameter21,
                                parameter22, parameter23, parameter24, parameter25, parameter26, parameter27, parameter28, parameter29 });
                        }

                        report.Refresh();
                        report.DataSources.Add(new ReportDataSource() { Name = "SaleTaxInvoice", Value = reportData.Detail });

                        byte[] file;
                        string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, @"Client\SaleTaxInvoice");
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
                            filePath = $"{prefix} - {voucherNumber}" + ".pdf";

                            stReader.Close();
                            stReader.Dispose();
                            stream.Flush();
                            stream.Close();
                            stream.Dispose();
                            report.Dispose();
                            string reportPath = Path.Combine(uploadsFolder, filePath);
                            System.IO.File.WriteAllBytes(reportPath, file);
                            filePath = $"/Client/SaleTaxInvoice/{filePath}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return filePath;
        }

        public string GenerateQrCodeZXingToFile(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string folder = Path.Combine(_hostingEnvironment.WebRootPath, @"Client\QR");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = $"{Guid.NewGuid()}.png";
            string fullPath = Path.Combine(folder, fileName);

            var writer = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 300,
                    Width = 300,
                    Margin = 2
                }
            };

            var pixelData = writer.Write(text);

            using (var bitmap = new SkiaSharp.SKBitmap(new SkiaSharp.SKImageInfo(pixelData.Width, pixelData.Height)))
            {
                var ptr = bitmap.GetPixels();
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, ptr, pixelData.Pixels.Length);

                using (var image = SkiaSharp.SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                using (var stream = System.IO.File.OpenWrite(fullPath))
                {
                    data.SaveTo(stream);
                }
            }

            return $"/Client/QR/{fileName}";
        }

        [HttpPost]
        public async Task<IActionResult> PostToFBR([FromBody] FBRModel model, string finalJson)
        {
            try
            {
                var client = new RestClient(model.FBRUrlToken.Url);

                var request = new RestRequest
                {
                    Method = Method.POST
                };

                request.AddHeader(
                    "Authorization",
                    $"Bearer {model.FBRUrlToken.Token}"
                );

                request.AddHeader(
                    "Content-Type",
                    "application/json"
                );

                request.AddParameter(
                    "application/json",
                    finalJson,
                    ParameterType.RequestBody
                );

                var response = await client.ExecuteAsync(request);

                var responseObj =
                    JsonConvert.DeserializeObject<FBRPostResponse>(response.Content);

                string qrText =
                    responseObj?.invoiceNumber
                    ?? responseObj?.validationResponse?.error
                    ?? "Unknown response";

                string qrCode = null;

                // Sirf valid invoiceNumber par QR generate hoga
                if (!string.IsNullOrWhiteSpace(responseObj?.invoiceNumber) &&
                    Regex.IsMatch(responseObj.invoiceNumber, @"^[A-Z0-9]+$"))
                {
                    qrCode = GenerateQrCode(responseObj.invoiceNumber);
                }

                return Content(
                    JsonConvert.SerializeObject(new
                    {
                        response = response.Content,
                        qrCode = qrCode
                    }),
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                return Content(
                    JsonConvert.SerializeObject(new
                    {
                        error = ex.Message
                    }),
                    "application/json"
                );
            }
        }
        public static string GenerateQrCode(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var writer = new ZXing.BarcodeWriterPixelData
                {
                    Format = ZXing.BarcodeFormat.QR_CODE,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Height = 300,
                        Width = 300,
                        Margin = 2
                    }
                };

                var pixelData = writer.Write(text);

                using (var bitmap = new SkiaSharp.SKBitmap(new SkiaSharp.SKImageInfo(pixelData.Width, pixelData.Height)))
                {
                    // Pixel copy
                    var ptr = bitmap.GetPixels();
                    System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, ptr, pixelData.Pixels.Length);

                    using (var image = SkiaSharp.SKImage.FromBitmap(bitmap))
                    using (var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                    {
                        return Convert.ToBase64String(data.ToArray());
                    }
                }
            }
            else
            {
                return string.Empty;
            }
        }

    }
}
