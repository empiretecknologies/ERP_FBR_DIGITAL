using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_ERP.Core.Entities
{
    public class AdvanceProduction
    {
        public int? TRAN_ID { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? VOUCHER_NO { get; set; }
        public int? ITEM_CODE { get; set; }
        public int? UNIT { get; set; }
        public int? WAREHOUSE { get; set; }
        public int? LOT { get; set; }
        public int? WASTAGE_CODE { get; set; }
        public int? COMP { get; set; }
        public double? WASTAGE_QTY { get; set; }
        public int? FITEM_CODE { get; set; }
        public string? REF { get; set; }
        public string? BATCH_NAME { get; set; }
        public double? COST { get; set; }
        public double? LOSS { get; set; }
        public int? PROCESS { get; set; }
        public double? BQTY { get; set; }
        public double? RATE { get; set; }
        public double? BAMT { get; set; }
        public double? QTY { get; set; }
        public double? WASTAGE { get; set; }
        public double? AMT { get; set; }
        public double? FRATE { get; set; }
        public string? REMARKS { get; set; }
        public int? BCODE { get; set; }
        public int? PERIOD_ID { get; set; }
        public string? ADD_USER_ID { get; set; }
        public DateTime? ADD_DATE { get; set; }
        public string? ADD_COMPUTER_NAME { get; set; }
        public string? ADD_IP_ADDRESS { get; set; }
        public string? EDIT_USER_ID { get; set; }
        public DateTime? EDIT_DATE { get; set; }
        public string? EDIT_COMPUTER_NAME { get; set; }
        public string? EDIT_IP_ADDRESS { get; set; }
        public string? ADD_POSTALCODE { get; set; }
        public string? EDIT_POSTALCODE { get; set; }
        public string? ASTATUS { get; set; }
        public int? MENU_ID { get; set; }
        public string? DLT { get; set; }
    }

    public class CustomAdvanceProduction
    {
        public AdvanceProduction? Master { get; set; }
        public List<AdvanceProductionDetail>? Detail { get; set; }
        //public List<AdvanceBCharges>? AdvanceDetail { get; set; }
    }

    //public class AdvanceCharges
    //{
    //    public int? CODE { get; set; }
    //    public int? ITEM_CODE { get; set; }
    //    public string? P_TRAN_ID { get; set; }
    //    public string? VDATE { get; set; }
    //    public string? SDATE { get; set; }
    //    public string? P_DT_CODE { get; set; }
    //    public List<AdvanceBCharges>? Detail { get; set; }
    //}

    public class AdvanceProductionRDLCReport
    {
        public int? TRAN_ID { get; set; }
        public int? MD_ID { get; set; }
        public string? REPORT_NAME { get; set; }
        public string? MD_NAME { get; set; }
        public string? INVOICE_NUMBER { get; set; }
        public string? DATE { get; set; }
        public string? RAW_ITEM { get; set; }
        public string? WASTAGE_ITEM { get; set; }
        public string? WAREHOUSE { get; set; }
        public string? LOT { get; set; }
        public string? COMPANY_NAME { get; set; }
        public string? COMPANY_ADDRESS { get; set; }
        public string? COMPANY_PHONE { get; set; }
        public string? BRANCH_ADDRESS { get; set; }
        public string? BRANCH_PHONE { get; set; }
        public string? COMPANY_LOGO { get; set; }
        public string? COMPANY_WATER { get; set; }
        public string? HEADER_NAME { get; set; }
        public string? REF { get; set; }
        public string? PROCESS { get; set; }
        public string? REMARKS { get; set; }
        public string? BQTY { get; set; }
        public string? COST { get; set; }
        public string? LOSS { get; set; } 
        public string? REFERENCENO { get; set; }
        public string? TERM { get; set; }
        public string? EDIT_USER_ID { get; set; }
        public string? MENU_TERMS { get; set; }
        public string? STATUS { get; set; }
        public string? SIG1 { get; set; }
        public string? SIG2 { get; set; }
        public string? SIG3 { get; set; }
        public string? SIG4 { get; set; }
        public string? USER { get; set; }
        public string? B_NAME { get; set; }
        public string? B_TERMS { get; set; }
        public string? B_GST { get; set; }
        public string? B_NTN { get; set; }
        public string? B_WEBSITE { get; set; }
        public string? EMAIL { get; set; }

        public decimal? B_QTY { get; set; }
        public decimal? WASTAGE { get; set; }
        public decimal? WASTAGE_QTY { get; set; }

    }

    public class CustomAdvanceProductionForPrintReport
    {
        public AdvanceProductionRDLCReport? Master { get; set; }
        public DataTable? Detail { get; set; }
    }

    public class AdvanceProductionDetail
    {
        public int? TRAN_ID { get; set; }
        public int? DT_CODE { get; set; }
        public int? ITEM_CODE { get; set; }
        public double? QTY { get; set; }
        public double? WASTAGE_RATE { get; set; }
        public int? UNIT { get; set; }
        public int? WAREHOUSE { get; set; }
        public int? LOT { get; set; }
        public double? QTY2 { get; set; }
        public double? BAL_QTY { get; set; }
        public double? RATE { get; set; }
        public double? AMT { get; set; }
        public double? LOSS { get; set; }
        public double? LOSS_WEIGHT { get; set; }
        public string? DT_DESC { get; set; }
        public int? BCODE { get; set; }
        public int? PERIOD_ID { get; set; }
        public string? ADD_USER_ID { get; set; }
        public DateTime? ADD_DATE { get; set; }
        public string? ADD_COMPUTER_NAME { get; set; }
        public string? ADD_IP_ADDRESS { get; set; }
        public string? EDIT_USER_ID { get; set; }
        public DateTime? EDIT_DATE { get; set; }
        public string? EDIT_COMPUTER_NAME { get; set; }
        public string? EDIT_IP_ADDRESS { get; set; }
        public string? ADD_POSTALCODE { get; set; }
        public string? EDIT_POSTALCODE { get; set; }
        public int? MENU_ID { get; set; }
        public string? DLT { get; set; }
        public int? CHK { get; set; }
    }
}