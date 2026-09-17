using Empire_ERP.Core.Entities;

namespace Empire_ERP.Core.Interfaces
{
    public interface ISalesInvoiceReportService
    {
        MyHttpResponseMessage GetReportTypes(Common common);
        MyHttpResponseMessage UpdateSodeBookFeedingReport(SodePartyReport modelrecord, Common common);
        MyHttpResponseMessage GetReportData(SalesInvoiceReport report, Common common);
    }
}