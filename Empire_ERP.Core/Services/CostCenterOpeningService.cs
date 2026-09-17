using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empire_ERP.Core.Services
{
    public class CostCenterOpeningService : ICostCenterOpeningService
    {
        public ICostCenterOpeningRepository _CostCenterOpeningRepository { get; set; }
        public CostCenterOpeningService(ICostCenterOpeningRepository CostCenterOpeningRepository)
        {
            _CostCenterOpeningRepository = CostCenterOpeningRepository;
        }

        public MyHttpResponseMessage GetCostCenterOpenings(Common common)
        {
            return _CostCenterOpeningRepository.GetCostCenterOpenings(common);
        }

        public MyHttpResponseMessage Save(List<CostCenterOpening> modelRecord, Common common)
        {
            return _CostCenterOpeningRepository.Save(modelRecord, common);
        }
    }
}