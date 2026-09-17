using Empire_ERP.Core.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_ERP.Core.Interfaces
{
    public interface IAdvanceProductionService
    {
		MyHttpResponseMessage QuickSearch(Common common);
        MyHttpResponseMessage Save(CustomAdvanceProduction model, Common common);
        MyHttpResponseMessage CopyRecord(CopyRecord code, Common common);
        MyHttpResponseMessage GetAdvanceProductionByCode(int code, Common common);
        MyHttpResponseMessage GetAdvanceProductionDetailByCode(int code, Common common);
        MyHttpResponseMessage Delete(int code, Common common);
        MyHttpResponseMessage DeleteAdvanceProductionDetailByCode(int code, Common common);
        MyHttpResponseMessage GetDataForReport(AdvanceProductionRDLCReport modelRecord, DataTable details, Common common);
    }
}