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

        public MyHttpResponseMessage UpdateSodeBookFeedingReport(SodePartyReport modelrecord, Common common)
        {
            return _salesInvoiceReportRepository.UpdateSodeBookFeedingReport(modelrecord, common);
        }

        public MyHttpResponseMessage GetReportData(SalesInvoiceReport report, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                if ((report.ReportID >= 18 && report.ReportID <= 36) || (report.ReportID >= 41 && report.ReportID <= 44) || (report.ReportID >= 45 && report.ReportID <= 67) 
                    || (report.ReportID >= 74 && report.ReportID <= 76) || (report.ReportID >= 87 && report.ReportID <= 90) || report.ReportID == 94 || report.ReportID == 95 
                    || report.ReportID == 117 || report.ReportID == 118 || report.ReportID == 132 || report.ReportID == 133 || report.ReportID == 134 || report.ReportID==135 
                    || report.ReportID == 137 || (report.ReportID == 100) || (report.ReportID >= 125 && report.ReportID <= 130) || report.ReportID == 138 || report.ReportID == 140 
                    || report.ReportID == 141 || report.ReportID == 91 || report.ReportID == 144 || report.ReportID == 145 || report.ReportID == 146 || report.ReportID == 147 
                    || report.ReportID == 148 || report.ReportID == 149 || report.ReportID == 150 || report.ReportID == 151 || report.ReportID == 152 || report.ReportID == 153 
                    || report.ReportID == 154 || report.ReportID == 155 || report.ReportID == 156 || report.ReportID == 157 || report.ReportID == 158 || report.ReportID == 159
                    || report.ReportID == 160 || report.ReportID == 161 || report.ReportID == 163 || report.ReportID == 164 || report.ReportID == 165 || report.ReportID == 166
                    || report.ReportID == 169 || report.ReportID == 170 || report.ReportID == 172 || report.ReportID == 162)
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