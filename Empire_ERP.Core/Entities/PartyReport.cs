namespace Empire_ERP.Core.Entities
{
    public class PartyReport
    {
        public string? Pass { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ReportID { get; set; }
        public string? ControlCode { get; set; }
        public string? RegionCode { get; set; }
        public string? BRANCH { get; set; }
        public string? PERIOD { get; set; }
        public int? PartyCode { get; set; }
        public int? Nature { get; set; }
        public int? ZERO { get; set; }
    }

    public class CustomPartyReport
    {
        public int S_NO { get; set; }
        public int TRAN_ID { get; set; }
        public string? VoucherDate { get; set; }
        public string? VoucherNo { get; set; }
        public int? AccountCode { get; set; }
        public string? AccountName { get; set; }
        public string? BName { get; set; }
        public string? AccountNature { get; set; }
        public string? PartyName { get; set; }
        public string? PAYMENT_MODE { get; set; }
        public string? ACT_DESC { get; set; }
        public string? AccountDescription { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public decimal? Balance { get; set; }
        public decimal? Amount { get; set; }
        public decimal? DueAmount { get; set; }
        public decimal? LastAmount { get; set; }
        public decimal? Qty { get; set; }
        public decimal? Rate { get; set; }
        public decimal? Amt { get; set; }
        public decimal? PREVIOUS_BALANCE { get; set; }
        //public decimal? BALANCE { get; set; }
        public decimal? RECEIVED_AMOUNT { get; set; }
        public decimal? Disc { get; set; }
        public int? DueYear { get; set; }
        public int? DueMonth { get; set; }
        public int? DueDay { get; set; }
        public string? LINK { get; set; }
        public string? DueDate { get; set; }
        public string? LastDate { get; set; }
        public string? BillType { get; set; }
        public string? ChqNo { get; set; }
        public string? ChqDate { get; set; }

    }
}