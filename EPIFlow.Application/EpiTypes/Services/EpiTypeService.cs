using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.EpiTypes.DTOs;
using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Application.EpiTypes.Services;

public class EpiTypeService : IEpiTypeService
{
    private readonly IRepository<EpiType> _epiTypeRepository;
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public EpiTypeService(
        IRepository<EpiType> epiTypeRepository,
        IRepository<InventoryItem> inventoryRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _epiTypeRepository = epiTypeRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<EpiTypeDto>> GetAllAsync(string? searchTerm, CancellationToken cancellationToken = default)
    {
        var query = _epiTypeRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(epi => epi.Code.Contains(term) || epi.Description.Contains(term));
        }

        return await query
            .AsNoTracking()
            .OrderBy(epi => epi.Code)
            .Select(epi => new EpiTypeDto(
                epi.Id,
                epi.Code,
                epi.Description,
                epi.Category,
                epi.ValidityInMonths,
                epi.CaNumber,
                epi.IsActive,
                epi.InventoryItems
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Select(item => item.QuantityAvailable)
                    .FirstOrDefault(),
                epi.InventoryItems
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Select(item => item.MinimumQuantity)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<EpiTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _epiTypeRepository.AsQueryable()
            .AsNoTracking()
            .Where(epi => epi.Id == id)
            .Select(epi => new EpiTypeDto(
                epi.Id,
                epi.Code,
                epi.Description,
                epi.Category,
                epi.ValidityInMonths,
                epi.CaNumber,
                epi.IsActive,
                epi.InventoryItems
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Select(item => item.QuantityAvailable)
                    .FirstOrDefault(),
                epi.InventoryItems
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Select(item => item.MinimumQuantity)
                    .FirstOrDefault()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(EpiTypeCreateDto dto, CancellationToken cancellationToken = default)
    {
        var exists = await _epiTypeRepository.AsQueryable()
            .AnyAsync(epi => epi.Code == dto.Code, cancellationToken);

        if (exists)
        {
            throw new ConflictException("Já existe um EPI com o mesmo código.");
        }

        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue || tenantId == Guid.Empty)
        {
            throw new ValidationException("Não foi possível identificar a empresa para vincular o EPI.",
                new Dictionary<string, string[]> { { "Tenant", new[] { "Faça login novamente e tente novamente." } } });
        }

        var epiType = new EpiType
        {
            Code = dto.Code.Trim(),
            Description = dto.Description.Trim(),
            Category = dto.Category,
            ValidityInMonths = dto.ValidityInMonths,
            CaNumber = dto.CaNumber?.Trim(),
            IsActive = true,
            TenantId = tenantId.Value
        };

        var inventoryItem = new InventoryItem
        {
            EpiType = epiType,
            QuantityAvailable = 0,
            MinimumQuantity = dto.MinimumQuantity,
            TenantId = tenantId.Value
        };

        await _epiTypeRepository.AddAsync(epiType, cancellationToken);
        await _inventoryRepository.AddAsync(inventoryItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return epiType.Id;
    }

    public async Task UpdateAsync(EpiTypeUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var epiType = await _epiTypeRepository.AsQueryable()
            .Include(epi => epi.InventoryItems)
            .FirstOrDefaultAsync(epi => epi.Id == dto.Id, cancellationToken);

        if (epiType is null)
        {
            throw new NotFoundException("Tipo de EPI não encontrado.");
        }

        var exists = await _epiTypeRepository.AsQueryable()
            .AnyAsync(epi => epi.Id != dto.Id && epi.Code == dto.Code, cancellationToken);

        if (exists)
        {
            throw new ConflictException("Já existe outro EPI com o mesmo código.");
        }

        epiType.Code = dto.Code.Trim();
        epiType.Description = dto.Description.Trim();
        epiType.Category = dto.Category;
        epiType.ValidityInMonths = dto.ValidityInMonths;
        epiType.CaNumber = dto.CaNumber?.Trim();
        epiType.IsActive = dto.IsActive;

        var inventoryItem = epiType.InventoryItems.FirstOrDefault();
        if (inventoryItem is null)
        {
            inventoryItem = new InventoryItem
            {
                EpiTypeId = epiType.Id,
                MinimumQuantity = dto.MinimumQuantity,
                QuantityAvailable = 0,
                TenantId = epiType.TenantId
            };

            await _inventoryRepository.AddAsync(inventoryItem, cancellationToken);
        }
        else
        {
            inventoryItem.MinimumQuantity = dto.MinimumQuantity;
            await _inventoryRepository.UpdateAsync(inventoryItem, cancellationToken);
        }

        await _epiTypeRepository.UpdateAsync(epiType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ToggleStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var epiType = await _epiTypeRepository.GetByIdAsync(id, cancellationToken);
        if (epiType is null)
        {
            throw new NotFoundException("Tipo de EPI não encontrado.");
        }

        epiType.IsActive = isActive;
        await _epiTypeRepository.UpdateAsync(epiType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
