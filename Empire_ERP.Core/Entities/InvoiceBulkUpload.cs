namespace Empire_ERP.Core.Entities
{
    public class InvoiceBulkPartyRow
    {
        public string PartyName { get; set; }
        public string PartyAddress { get; set; }
        public string PartyRegion { get; set; }
        public string PartyRegType { get; set; }
        public string PartyNtn { get; set; }
        public string PartyStReg { get; set; }
        public string Status { get; set; }
        public string ActionType { get; set; }
        public int? ExistingPartyCode { get; set; }
    }

    public class InvoiceBulkItemRow
    {
        public string ItemName { get; set; }
        public string ItemUnit { get; set; }
        public string HsCode { get; set; }
        public string Status { get; set; }
        public int? UnitCode { get; set; }
        public string ActionType { get; set; }
        public int? ExistingItemCode { get; set; }
    }

    public class InvoiceBulkDetailRow
    {
        public string PartyName { get; set; }
        public string PartyAddress { get; set; }
        public string PartyRegion { get; set; }
        public string PartyRegType { get; set; }
        public string PartyNtn { get; set; }
        public string PartyStReg { get; set; }
        public string ItemName { get; set; }
        public string ItemUnit { get; set; }
        public string HsCode { get; set; }
        public decimal InvoiceQty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal WRate { get; set; }
        public decimal WAmt { get; set; }
        public decimal Tax { get; set; }
        public decimal TaxAmt { get; set; }
        public decimal NetAmount { get; set; }
        public string SaleType { get; set; }
        public string ItemSno { get; set; }
        public string ScheduleNo { get; set; }
        public string SerialSno { get; set; }
    }

    public class InvoiceBulkPartyRequest
    {
        public List<InvoiceBulkPartyRow> Parties { get; set; }
    }

    public class InvoiceBulkItemRequest
    {
        public List<InvoiceBulkItemRow> Items { get; set; }
    }

    public class InvoiceBulkInvoiceRequest
    {
        public List<InvoiceBulkDetailRow> Rows { get; set; }
    }
}
