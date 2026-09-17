using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Empire_ERP.Controllers
{
    [CheckSession]
    [ExtractMenuCode]
    public class CostCenterOpeningController : BaseController
	{
		public ICostCenterOpeningService _CostCenterOpeningService { get; set; }
		public CostCenterOpeningController(ICostCenterOpeningService CostCenterOpeningService, IMenuService menuService,IBaseService baseService) : base(menuService,baseService)
		{
            _CostCenterOpeningService = CostCenterOpeningService;
		}

		public IActionResult Index()
		{
            ViewBag.Permissions = CommonHelper.GetValues(HttpContext).RoleType == "A"
                ? "Admin"
                : CommonHelper.GetPermissionByMenueID(CommonHelper.GetValues(HttpContext).RoleID, CommonHelper.GetValues(HttpContext).MenuID);
            return View();
		}

		[HttpGet]
		public JsonResult GetCostCenterOpenings()
		{
			try
			{
				var data = _CostCenterOpeningService.GetCostCenterOpenings(CommonHelper.GetValues(HttpContext));
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
        public JsonResult Save(List<CostCenterOpening> CostCenterOpenings)
        {
            try
            {
                var data = _CostCenterOpeningService.Save(CostCenterOpenings, CommonHelper.GetValues(HttpContext));
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
    }
}