using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_ERP.Core.Entities
{
    public class KnockOff
    {
        public int? TRAN_ID { get; set; }
        public int? PICK_ID { get; set; }
        public int? PMENU_ID { get; set; }
        public int DT_CODE { get; set; }
        public decimal? AMOUNT { get; set; }
        public decimal? CHARGES_SUM { get; set; }
        public int? PARTY_CODE { get; set; }
        public string? PARTY_NAME { get; set; }
        public int? ACT_CODE { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? QTY { get; set; }
        public string? REMARKS { get; set; }
        public string? VOUCHER_NO { get; set; }
        public int? PICK_AMT { get; set; }
        public double? KO_AMT { get; set; }
        public double? WHT { get; set; }
        public double? WHT_AMT { get; set; }
        public double? WHT_NET_AMT { get; set; }
        public string? ASTATUS { get; set; }
        public string? BOOK_NAME { get; set; }
        public string? DC_TYPE { get; set; }
        public double? WHT_RATE { get; set; }
        public double? PARTY_BAL { get; set; }


    }


    public class CustomKnockOff
    {
        public int K_ID { get; set; }
        public List<KnockOff>? Data { get; set; }
    }
}