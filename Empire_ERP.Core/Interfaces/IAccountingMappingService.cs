using Empire_ERP.Core.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_ERP.Core.Interfaces
{
    public interface IAccountingMappingService
    {

        MyHttpResponseMessage QuickSearch(Common common);
        MyHttpResponseMessage Save(List<AccountingMapping> model, Common common);
        MyHttpResponseMessage GetAccountingMappingByCode(int code, Common common);
        MyHttpResponseMessage GetAccountingMappingDetailsByCode(int code, Common common);
        MyHttpResponseMessage Delete(int code, Common common);
        MyHttpResponseMessage CopyRecord(CopyRecordSalesman code, Common common);
        MyHttpResponseMessage DeleteAccountingMappingDetailByCode(int gcode, int code, Common common);
    }
}
