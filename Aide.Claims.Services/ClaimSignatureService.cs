using Aide.Core.CustomExceptions;
using Aide.Core.Data;
using Aide.Claims.Domain.Mapping;
using Aide.Claims.Domain.Objects;
using Aide.Claims.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aide.Claims.Services
{
	public interface IClaimSignatureService
	{
		Task<ClaimSignature> GetClaimSignatureById(int claimSignatureId);
		Task<ClaimSignature> GetClaimSignatureByClaimId(int claimId);
		Task InsertClaimSignature(ClaimSignature dto);
		Task UpdateClaimSignature(ClaimSignature dto);
	}

	public class ClaimSignatureService : IClaimSignatureService
	{
		#region Properties

		private readonly IServiceProvider _serviceProvider;

		#endregion

		#region Constructor

		public ClaimSignatureService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		#endregion

		#region Methods

		public async Task<ClaimSignature> GetClaimSignatureById(int claimSignatureId)
		{
			if (claimSignatureId <= 0) throw new ArgumentException(nameof(claimSignatureId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_signature>(transientContext);
				var entity = await repository.GetByIdAsync(claimSignatureId).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				var dto = ClaimSignatureMap.ToDto(entity);
				return dto;
			}
		}

		public async Task<ClaimSignature> GetClaimSignatureByClaimId(int claimId)
		{
			if (claimId <= 0) throw new ArgumentException(nameof(claimId));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_signature>(transientContext);
				var query = from claim_signature in repository.TableNoTracking
							where claim_signature.claim_id == claimId
							select claim_signature;
				var entities = await query.ToListAsync().ConfigureAwait(false);
				if (entities == null) throw new NonExistingRecordCustomizedException();
				var entity = entities.FirstOrDefault();

				var dto = ClaimSignatureMap.ToDto(entity);
				return dto;
			}
		}

		public async Task InsertClaimSignature(ClaimSignature dto)
		{
			if (dto == null) throw new ArgumentNullException();

			dto.DateCreated = DateTime.UtcNow;

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var entity = ClaimSignatureMap.ToEntity(dto);
				var repository = new EfRepository<claim_signature>(transientContext);
				await repository.InsertAsync(entity).ConfigureAwait(false);
				dto.Id = entity.claim_signature_id;
			}
		}

		public async Task UpdateClaimSignature(ClaimSignature dto)
		{
			if (dto == null) throw new ArgumentNullException();
			if (dto.Id <= 0) throw new ArgumentException(nameof(dto.Id));

			using (var transientContext = _serviceProvider.GetRequiredService<AideDbContext>())
			{
				var repository = new EfRepository<claim_signature>(transientContext);
				var entity = await repository.GetByIdAsync(dto.Id).ConfigureAwait(false);
				if (entity == null) throw new NonExistingRecordCustomizedException();

				////dto.DateModified = DateTime.UtcNow;
				dto.DateCreated = DateTime.UtcNow; // This is because there's no DateModified field
				entity = ClaimSignatureMap.ToEntity(dto, entity);
				await repository.UpdateAsync(entity).ConfigureAwait(false);
			}
		}

		#endregion
	}
}
