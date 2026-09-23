using Empire_ERP.Core.Entities;
using System.Data;

namespace Empire_ERP.Core.Interfaces
{
    public interface IPurchaseBillService
    {
        MyHttpResponseMessage QuickSearch(Common common);
        MyHttpResponseMessage GetPurchaseBillByCode(int code, Common common);
        MyHttpResponseMessage GetPurchaseBillDetailByCode(int code, Common common);
        MyHttpResponseMessage GetPurchaseBillChargesByCode(int code, Common common);
        MyHttpResponseMessage Save(CustomPurchaseBill modelRecord, Common common);
        MyHttpResponseMessage SaveCharges(ChargesModel modelRecord, Common common);
        MyHttpResponseMessage Delete(int code, Common common);
        MyHttpResponseMessage DeleteCharges(int code, int tranId, Common common);
        MyHttpResponseMessage GetDataForApi(int code, Common common);
        List<SaleTaxInvoiceDetail> GetDetailDataForApi(int code, Common common);
        MyHttpResponseMessage FBRApi_Status(string code, string apiResponce, Common common);
        MyHttpResponseMessage CopyRecord(CopyRecord code, Common common);
        MyHttpResponseMessage DeletePurchaseBillDetailByCode(int code, Common common);
        MyHttpResponseMessage GetPickDataByParty(int partyCode, int actCode, Common common);
        MyHttpResponseMessage GetDataForReport(PurchaseBillRDLCReport modelRecord, DataTable details, Common common);
        MyHttpResponseMessage GetDashboardData(Common common);
    }
}