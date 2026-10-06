using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Empire_ERP.Core.Services
{
    public class SalesInvoiceReportService : ISalesInvoiceReportService
    {
        public ISalesInvoiceReportRepository _salesInvoiceReportRepository { get; set; }
        public SalesInvoiceReportService(ISalesInvoiceReportRepository salesInvoiceReportRepository)
        {
            _salesInvoiceReportRepository = salesInvoiceReportRepository;
        }

        public MyHttpResponseMessage GetReportTypes(Common common)
        {
            return _salesInvoiceReportRepository.GetReportTypes(common.MenuID, common.RoleType, common.RoleID);
        }

        //public MyHttpResponseMessage UpdateSodeBookFeedingReport(SodePartyReport modelrecord, Common common)
        //{
        //    return _salesInvoiceReportRepository.UpdateSodeBookFeedingReport(modelrecord, common);
        //}

        public MyHttpResponseMessage GetReportData(SalesInvoiceReport report, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                if (report.ReportID == 154 || report.ReportID == 155 || report.ReportID == 157 || report.ReportID == 158)
                {
                    response = _salesInvoiceReportRepository.GetReportData(report, common);
                }
                else
                {
                    response.msgType = 2;
                    response.msg = "This report is not available yet but this will be available soon.";
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
            return response;
        }


    }
}