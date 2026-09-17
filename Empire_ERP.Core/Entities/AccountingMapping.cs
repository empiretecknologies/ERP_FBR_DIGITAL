using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_ERP.Core.Entities
{
    public class AccountingMapping
    {

        public DateTime? V_DATE { get; set; }
        public int? TRAN_ID { get; set; }
        public int? DT_CODE { get; set; }
        public int? SALESMAN { get; set; }
        //public int? PARTY { get; set; }

        public int? ACT_SETUP { get; set; }
        public int? COA { get; set; }
        public int? ITEM_CODE { get; set; }
        //public double? RATE { get; set; }
        //public double? DISC { get; set; }
        //public double? ADV { get; set; }

        public string? REMARKS { get; set; }
        public string? COMM_UNIT { get; set; }
        public int? COMM_VALUE { get; set; }
        public string? ASTATUS { get; set; }

    }
}
