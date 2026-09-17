using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_ERP.Core.Entities
{
    public class PurchaseOrder
    {
        public int? TRAN_ID { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? VOUCHER_NO { get; set; }
        public int? PARTY_CODE { get; set; }
        public int? ACT_CODE { get; set; }
        public int? SCODE { get; set; }
        public int? SACODE { get; set; }
        public double? COMM { get; set; }
        public string? COMM_AMT { get; set; }
        public double? COMM_VAL { get; set; }
        public double? DISC { get; set; }
        public double? CARTAGE { get; set; }
        public string? REF { get; set; }
        public string? ORDER_TYPE { get; set; }
        public string? REMARKS { get; set; }
        public string? BTYPE { get; set; }
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
        public string? HS_CODE { get; set; }
        public string? DOC { get; set; }
        public string? RINV_NO { get; set; }
        public string? REGION { get; set; }
        public DateTime? RINV_DATE { get; set; }
        public int? CURR_CODE { get; set; }
        public int? TERMS { get; set; }
        public double? CRATE { get; set; }
        public int? BARCODE_ID { get; set; }
        public int? WAREHOUSE { get; set; }

    }

    public class CustomPurchaseOrder
    {
        public PurchaseOrder? Master { get; set; }
        public List<PurchaseOrderDetail>? Detail { get; set; }
    }


    

    public class PurchaseOrderRDLCReport
    {
        public int? TRAN_ID { get; set; }
        public int? MD_ID { get; set; }
        public string? REPORT_NAME { get; set; }
        public string? BALANCE { get; set; }
        public string? MD_NAME { get; set; }
        public string? INVOICE_NUMBER { get; set; }
        public string? DATE { get; set; }
        public string? PARTY_NAME { get; set; }
        public string? PADDRESS { get; set; }
        public string? BT_CUSTOMER { get; set; }
        public string? COMPANY_NAME { get; set; }
        public string? B_NAME { get; set; }
        public string? B_TERMS { get; set; }
        public string? B_WEBSITE { get; set; }
        public string? EMAIL { get; set; }
        public string? B_GST { get; set; }
        public string? B_NTN { get; set; }
        public string? USER { get; set; }
        public string? STATUS { get; set; }
        public string? ORDER_TYPE { get; set; }
        public string? TERMS { get; set; }
        public string? REF { get; set; }
        public string? COMMENT { get; set; }
        public string? PNTN { get; set; }
        public string? RINV_NO { get; set; }
        public string? COMPANY_ADDRESS { get; set; }
        public string? COMPANY_PHONE { get; set; }
        public string? BRANCH_ADDRESS { get; set; }
        public string? BRANCH_PHONE { get; set; }
        public string? COMPANY_LOGO { get; set; }
        public string? RCOMPANY_LOGO { get; set; }
        public string? COMPANY_WATER { get; set; }
        public string? HEADER_NAME { get; set; }
        public string? ACT_GRCODE { get; set; }
        public string? SIG1 { get; set; }
        public string? SIG2 { get; set; }
        public string? SIG3 { get; set; }
        public string? SIG4 { get; set; }
        public string? CARTAGE { get; set; }
        public string? REFERENCENO { get; set; }
        public string? TERM { get; set; }
        public string? CURR { get; set; }
        public string? CURR_SIG { get; set; }
        public string? EDIT_USER_ID { get; set; }
        public string? MENU_TERMS { get; set; }
        public decimal DISC { get; set; }
    }

    public class CustomPurchaseOrderForPrintReport
    {
        public PurchaseOrderRDLCReport? Master { get; set; }
        public DataTable? Detail { get; set; }
    }

    public class PurchaseOrderDetail
    {
        public int? TRAN_ID { get; set; }
        public int? DT_CODE { get; set; }
        public int? ITEM_CODE { get; set; }
        public double? QTY { get; set; }
        public double? BAGS { get; set; }
        public double? COMM_VALUE { get; set; }
        public string? COMM_TYPE { get; set; }
        public int? UNIT { get; set; }
        public int? LOT { get; set; }
        public double? QTY2 { get; set; }
        public double? PACK { get; set; }
        public double? TOTAL_PACK { get; set; }
        public double? BAL_QTY { get; set; }
        public double? WEIGHT { get; set; }
        public double? RATE { get; set; }
        public double? AMT { get; set; }
        public double? DISC { get; set; }
        public double? DISC_AMT { get; set; }
        public double? TAX { get; set; }
        public double? TAX_AMT { get; set; }
        public double? ADV { get; set; }
        public double? LAST_RATE { get; set; }
        public double? ADV_AMT { get; set; }
        public double? NET_AMT { get; set; }
        public string? DT_DESC { get; set; }
        public string? HS_CODE { get; set; }
        public string? INC_EXC { get; set; }

        public int? COLOR { get; set; }
        public int? SIZE { get; set; }
        public int? PAY_TERM { get; set; }
        public int? GRADE { get; set; }
        public int? WAREHOUSE { get; set; }
        public DateTime? DEL_DATE { get; set; }
        public DateTime? DUE_DATE { get; set; }
        public int? DUE_DAYS { get; set; }
        public string? VEH { get; set; }
        public int? BCODE { get; set; }
        public int? PERIOD_ID { get; set; }
        public string? ADD_USER_ID { get; set; }
        public string? BARCODE { get; set; }
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
        public int? PICK_ID { get; set; }
        public int? PICK_ID_D { get; set; }
        public int? BPARTY_CODE { get; set; }
        public int? BACT_CODE { get; set; }
    }
}
