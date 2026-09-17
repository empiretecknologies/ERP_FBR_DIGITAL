using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Core.Services;
using Empire_ERP.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Reporting.NETCore;
using System.Text;

namespace Empire_ERP.Controllers
{
    [CheckSession]
    [ExtractMenuCode]
    public class AccountingMappingController : BaseController
    {
        //public ICommMapService _custPricingService { get; set; }
        public IAccountingMappingService _custPricingService { get; set; }


        public IPeriodService _periodService { get; set; }
        public AccountingMappingController(IMenuService menuService, IAccountingMappingService custPricingService,IPeriodService periodService,IBaseService baseService) : base(menuService,baseService)
        {
            _custPricingService = custPricingService;
            _periodService = periodService;
        }
        public IActionResult Index()
        {
            var common = CommonHelper.GetValues(HttpContext);
            ViewBag.Permissions = common.RoleType == "A"
                ? "Admin"
                : CommonHelper.GetPermissionByMenueID(common.RoleID, common.MenuID);
            var Menu = _menuService.GetMenu(common.MenuID);

            //var response = _menuService.GetMenu(common.MenuID);
            if (Menu.msgType == 1)
            {
                ViewBag.DC_TYPE = ((Menu)Menu.data).DCTYPE;
                ViewBag.DATA_CLEAR = ((Menu)Menu.data).DATA_CLEAR;
            }

            int? pType = 0; string? dcType = string.Empty, itemType=string.Empty;
            if (Menu.data != null)
            {
                var menu = (Menu)Menu.data;
                pType = menu.PTYPE;
                dcType = menu.DCTYPE;
                itemType = menu.ITEM_TYPE;

            }
            ViewBag.Limit = CommonHelper.GetLimitByMenueID(common.MenuID);
            var periodInfo = _periodService.GetPeriodById(Convert.ToInt32(common.Period));
            ViewBag.Warehouse = DropdownService.WareHouseDropdownWthSubWithGr();


            ViewBag.PartyType = DropdownService.CustomPartyTypeDropdownWithAccountCodeAndPType(common.RoleID, common.RoleType,pType);

            ViewBag.SalesMan = DropdownService.SalesmanNameDropdown(common);
            
            ViewBag.ItemMaster = DropdownService.ItemMasterDropdownWithPrice(common.RoleID, common.RoleType,dcType, itemType);
            ViewBag.ActSetup = DropdownService.GetActSetupData();
            ViewBag.GLData = DropdownService.GetGLData();
            ViewBag.Items = DropdownService.ItemMasterDropdownWithUnits();
            return View();
        }

        [HttpGet]
        public JsonResult GetSalesMan(Common common)
        {
            try
            {
                var data = DropdownService.SalesmanNameDropdown(common);
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
        
        public JsonResult GetAccountingMapping()
        {
            try
            {
                var data = _custPricingService.QuickSearch(CommonHelper.GetValues(HttpContext));
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
        public JsonResult Save(List<AccountingMapping> modelRecord)
        {
            try
            {
                var data = _custPricingService.Save(modelRecord, CommonHelper.GetValues(HttpContext));
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

        public JsonResult GetAccountingMappingByCode(int code)
        {
            try
            {
                var data = _custPricingService.GetAccountingMappingByCode(code, CommonHelper.GetValues(HttpContext));
                var detailData = _custPricingService.GetAccountingMappingDetailsByCode(code, CommonHelper.GetValues(HttpContext));
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
        public JsonResult GetAccountingMappingrDetailsByCode(int code)
        {
            try
            {
                var data = _custPricingService.GetAccountingMappingDetailsByCode(code, CommonHelper.GetValues(HttpContext));
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
                var data = _custPricingService.Delete(code, CommonHelper.GetValues(HttpContext));
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
        public JsonResult CopyRecord(CopyRecordSalesman record)
        {
            try
            {
                var data = _custPricingService.CopyRecord(record, CommonHelper.GetValues(HttpContext));
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
        public JsonResult DeleteAccountingMappingDetailByCode(int gcode, int code)
        {
            try
            {
                var data = _custPricingService.DeleteAccountingMappingDetailByCode(gcode, code, CommonHelper.GetValues(HttpContext));
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

        
    }
}
