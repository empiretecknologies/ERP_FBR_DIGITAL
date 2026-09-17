using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_ERP.Core.Interfaces
{
    public interface IAdvanceProductionRepository
    {
        MyHttpResponseMessage QuickSearch(Common common, Menu menu);
        MyHttpResponseMessage Save(CustomAdvanceProduction model, Common common, Menu menu);
        MyHttpResponseMessage CopyRecord(CopyRecord record, Common common, Menu menu);
        MyHttpResponseMessage GetAdvanceProductionByCode(int code, Common common, Menu menu);
        MyHttpResponseMessage GetAdvanceProductionDetailByCode(int code, Common common, Menu menu);
        MyHttpResponseMessage Delete(int code, Common common, Menu menu);
        MyHttpResponseMessage DeleteAdvanceProductionDetailByCode(int code, Common common, Menu menu);
        MyHttpResponseMessage GetDataForReport(AdvanceProductionRDLCReport modelRecord, DataTable details, CustomMenuDetail menuDetails, Company currentCompany, Common common);
    }
}