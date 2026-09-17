using Empire_ERP.Core.Entities;
using System.Data;

namespace Empire_ERP.Core.Interfaces
{
    public interface IWarehouseTransferService
    {
        MyHttpResponseMessage QuickSearch(Common common);
        MyHttpResponseMessage GetStockTransferByCode(int code, Common common);
        MyHttpResponseMessage GetStockTransferDetailByCode(int code, Common common);
        MyHttpResponseMessage Save(CustomStockTransfer modelRecord, Common common);
        MyHttpResponseMessage Delete(int code, Common common);
        MyHttpResponseMessage CopyRecord(CopyRecord code, Common common);
        MyHttpResponseMessage DeleteStockTransferDetailByCode(int code, Common common);
        MyHttpResponseMessage GetDataForCartonSticker(List<StockTransferStickerPrint> stockData);
        MyHttpResponseMessage GetDataForPrintReport(RDLCReport modelRecord, DataTable details, Common common);
        MyHttpResponseMessage GetStockTransferDetailByItem(int code, int qty, Common common);
        MyHttpResponseMessage GetSodaBookFeedingDetailBySodaDate(DateTime? sodaDate, int branch, Common common);
        MyHttpResponseMessage UpdatePrintStatus(string codes, Common common);
        MyHttpResponseMessage GetBarcodeList();
    }
}