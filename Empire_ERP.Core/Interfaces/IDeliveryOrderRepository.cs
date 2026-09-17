using Empire_ERP.Core.Entities;
using System.Data;

namespace Empire_ERP.Core.Interfaces
{
    public interface IDeliveryOrderRepository
    {
        MyHttpResponseMessage QuickSearch(Common common);
        MyHttpResponseMessage GetDeliveryOrderByCode(int code, Common common);
        MyHttpResponseMessage GetDeliveryOrderDetailByCode(int code, Common common);
        MyHttpResponseMessage Save(CustomDeliveryOrder modelRecord, Common common);
        MyHttpResponseMessage Delete(int code, Common common);
        MyHttpResponseMessage CopyRecord(CopyRecord record, Common common, Menu menu);
        MyHttpResponseMessage DeleteDeliveryOrderDetailByCode(int code, Common common);
        MyHttpResponseMessage GetPickDataByParty(int partyCode, int actCode, Common common);
        MyHttpResponseMessage GetDataForReport(PurchaseBillRDLCReport modelRecord, DataTable details, CustomMenuDetail menuDetails, Company currentCompany, Common common);
    }
}