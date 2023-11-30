using Aide.Admin.Domain.Mapping;
using Aide.Admin.Domain.Objects;
using Aide.Admin.Models;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Aide.Admin.Services
{
    public interface IInsuranceCompanyClaimTypeSettingsService
    {
        Task<IEnumerable<InsuranceCompanyClaimTypeSettings>> GetAll();
        Task<IDictionary<int, InsuranceCompanyClaimTypeSettings>> GetByInsuranceCompanyId(int insuranceCompanyId);
        Task Upsert(InsuranceCompanyClaimTypeSettings dto);
    }

    public class InsuranceCompanyClaimTypeSettingsService : IInsuranceCompanyClaimTypeSettingsService
    {
        #region Properties

        private readonly IServiceProvider _serviceProvider;
        private readonly ICacheService _cacheService;
        private const string _cacheKeyNameForInsuranceCompanyClaimTypeSettings = "Dto-List-InsuranceCompanyClaimTypeSettings";

        #endregion

        #region Constructor

        public InsuranceCompanyClaimTypeSettingsService(IServiceProvider serviceProvider, ICacheService cacheService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        #endregion

        #region Methods

        public async Task<IEnumerable<InsuranceCompanyClaimTypeSettings>> GetAll()
        {
            //Begin Cache
            if (_cacheService.Exist(_cacheKeyNameForInsuranceCompanyClaimTypeSettings))
            {
                return _cacheService.Get<IEnumerable<InsuranceCompanyClaimTypeSettings>>(_cacheKeyNameForInsuranceCompanyClaimTypeSettings);
            }
            //End Cache

            IEnumerable<InsuranceCompanyClaimTypeSettings> dtos = new List<InsuranceCompanyClaimTypeSettings>();
            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                var repository = new EfRepository<insurance_company_claim_type_settings>(transientContext);
                var query = from insurance_company_claim_type_settings in repository.TableNoTracking select insurance_company_claim_type_settings;
                var entities = await query.ToListAsync().ConfigureAwait(false);
                if (!entities.Any()) return dtos;
                dtos = InsuranceCompanyClaimTypeSettingsMap.ToDto(entities);
            }

            //Begin Cache
            _cacheService.Set(_cacheKeyNameForInsuranceCompanyClaimTypeSettings, dtos);
            //End Cache

            return dtos;
        }

        public async Task<IDictionary<int, InsuranceCompanyClaimTypeSettings>> GetByInsuranceCompanyId(int insuranceCompanyId)
        {
            if (insuranceCompanyId <= 0) throw new ArgumentOutOfRangeException(nameof(insuranceCompanyId));

            var dtos = await GetAll().ConfigureAwait(false);
            var result = dtos.Where(x => x.InsuranceCompanyId == insuranceCompanyId);
            if (dtos.Any())
            {
                return result.GroupBy(gb => gb.ClaimTypeId).ToDictionary(dk => (int)dk.Key, dv => dv.FirstOrDefault());
            }
            else
            {
                return new Dictionary<int, InsuranceCompanyClaimTypeSettings>();
            }
        }

        public async Task Upsert(InsuranceCompanyClaimTypeSettings dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.InsuranceCompanyId <= 0) throw new ArgumentException(nameof(dto.InsuranceCompanyId));
            if (dto.ClaimTypeId <= 0) throw new ArgumentException(nameof(dto.ClaimTypeId));

            using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
            {
                var repository = new EfRepository<insurance_company_claim_type_settings>(transientContext);
                var query = from insurance_company_claim_type_settings in repository.Table
                            where insurance_company_claim_type_settings.insurance_company_id == dto.InsuranceCompanyId && insurance_company_claim_type_settings.claim_type_id == (int)dto.ClaimTypeId
                            select insurance_company_claim_type_settings;
                var entities = await query.ToListAsync().ConfigureAwait(false);
                insurance_company_claim_type_settings entity = null;

                dto.DateCreated = DateTime.UtcNow;
                dto.DateModified = DateTime.UtcNow;
                if (entities.Any())
                {
                    entity = entities.FirstOrDefault();
                    dto.DateCreated = entity.date_created;
                    entity = InsuranceCompanyClaimTypeSettingsMap.ToEntity(dto, entity);
                    await repository.UpdateAsync(entity).ConfigureAwait(false);
                }
                else
                {
                    entity = InsuranceCompanyClaimTypeSettingsMap.ToEntity(dto);
                    await repository.InsertAsync(entity).ConfigureAwait(false);
                    //dto.Id = entity.insurance_company_claim_type_settings_id;
                }
            }

            //Begin Cache
            if (_cacheService.Exist(_cacheKeyNameForInsuranceCompanyClaimTypeSettings))
            {
                var dtos = await GetAll().ConfigureAwait(false);
                var list = dtos.ToList();
                var currentDto = list.FirstOrDefault(li => li.InsuranceCompanyId == dto.InsuranceCompanyId && li.ClaimTypeId == dto.ClaimTypeId);
                if (currentDto == null)
                {
                    list.Add(dto);
                }
                else
                {
                    currentDto = InsuranceCompanyClaimTypeSettingsMap.ToDto(dto, currentDto);
                }
                _cacheService.Set(_cacheKeyNameForInsuranceCompanyClaimTypeSettings, list);
            }
            //End Cache
        }

        #endregion
    }
}
