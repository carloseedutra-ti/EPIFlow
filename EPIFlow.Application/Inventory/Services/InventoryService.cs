using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.Inventory.DTOs;
using EPIFlow.Application.Inventory.DTOs.Movements;
using EPIFlow.Domain.Entities;
using EPIFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Application.Inventory.Services;

public class InventoryService : IInventoryService
{
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IRepository<StockMovement> _movementRepository;
    private readonly IRepository<EpiType> _epiTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IRepository<InventoryItem> inventoryRepository,
        IRepository<StockMovement> movementRepository,
        IRepository<EpiType> epiTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _epiTypeRepository = epiTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetAllAsync(string? searchTerm, CancellationToken cancellationToken = default)
    {
        var query = _inventoryRepository.AsQueryable();

        query = query
            .Include(item => item.EpiType)
            .Include(item => item.Movements);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(item =>
                item.EpiType!.Code.Contains(term) ||
                item.EpiType.Description.Contains(term));
        }

        return await query
            .AsNoTracking()
            .OrderBy(item => item.EpiType!.Code)
            .Select(item => new InventoryItemDto(
                item.Id,
                item.EpiTypeId,
                item.EpiType!.Code,
                item.EpiType.Description,
                item.QuantityAvailable,
                item.MinimumQuantity,
                item.Location,
                item.LastInventoryDate,
                item.Movements
                    .OrderByDescending(movement => movement.MovementDate)
                    .Take(10)
                    .Select(movement => new StockMovementDto(
                        movement.Id,
                        movement.MovementType,
                        movement.Quantity,
                        movement.MovementDate,
                        movement.Reference,
                        movement.Notes))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _inventoryRepository.AsQueryable()
            .Where(item => item.Id == id)
            .Include(item => item.EpiType)
            .Include(item => item.Movements)
            .AsNoTracking()
            .Select(item => new InventoryItemDto(
                item.Id,
                item.EpiTypeId,
                item.EpiType!.Code,
                item.EpiType.Description,
                item.QuantityAvailable,
                item.MinimumQuantity,
                item.Location,
                item.LastInventoryDate,
                item.Movements
                    .OrderByDescending(movement => movement.MovementDate)
                    .Take(20)
                    .Select(movement => new StockMovementDto(
                        movement.Id,
                        movement.MovementType,
                        movement.Quantity,
                        movement.MovementDate,
                        movement.Reference,
                        movement.Notes))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> RegisterMovementAsync(StockMovementCreateDto dto, CancellationToken cancellationToken = default)
    {
        var inventoryItem = await _inventoryRepository.AsQueryable()
            .FirstOrDefaultAsync(item => item.Id == dto.InventoryItemId, cancellationToken);

        if (inventoryItem is null)
        {
            throw new NotFoundException("Item de estoque não encontrado.");
        }

        var movement = new StockMovement
        {
            InventoryItemId = dto.InventoryItemId,
            MovementType = dto.MovementType,
            Quantity = dto.Quantity,
            MovementDate = DateTime.UtcNow,
            Reference = dto.Reference,
            Notes = dto.Notes
        };

        switch (dto.MovementType)
        {
            case StockMovementType.Entry:
                inventoryItem.QuantityAvailable += dto.Quantity;
                break;
            case StockMovementType.Exit:
                if (inventoryItem.QuantityAvailable < dto.Quantity)
                {
                    throw new ValidationException(
                        "Quantidade indisponível em estoque.",
                        new Dictionary<string, string[]> { { nameof(dto.Quantity), new[] { "Quantidade informada excede o saldo disponível." } } });
                }

                inventoryItem.QuantityAvailable -= dto.Quantity;
                break;
            case StockMovementType.Adjustment:
                inventoryItem.QuantityAvailable = dto.Quantity;
                inventoryItem.LastInventoryDate = DateTime.UtcNow;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dto.MovementType), dto.MovementType, null);
        }

        await _movementRepository.AddAsync(movement, cancellationToken);
        await _inventoryRepository.UpdateAsync(inventoryItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return movement.Id;
    }

    public async Task RecalculateStockAsync(Guid inventoryItemId, CancellationToken cancellationToken = default)
    {
        var inventoryItem = await _inventoryRepository.AsQueryable()
            .Include(item => item.Movements)
            .FirstOrDefaultAsync(item => item.Id == inventoryItemId, cancellationToken);

        if (inventoryItem is null)
        {
            throw new NotFoundException("Item de estoque não encontrado.");
        }

        var total = inventoryItem.Movements
            .Where(movement => !movement.IsDeleted)
            .Sum(movement => movement.MovementType switch
            {
                StockMovementType.Entry => movement.Quantity,
                StockMovementType.Exit => -movement.Quantity,
                StockMovementType.Adjustment => 0,
                _ => 0
            });

        var lastAdjustment = inventoryItem.Movements
            .Where(movement => movement.MovementType == StockMovementType.Adjustment && !movement.IsDeleted)
            .OrderByDescending(movement => movement.MovementDate)
            .FirstOrDefault();

        if (lastAdjustment is not null)
        {
            var subsequentMovementsTotal = inventoryItem.Movements
                .Where(movement => !movement.IsDeleted && movement.MovementDate > lastAdjustment.MovementDate)
                .Sum(movement => movement.MovementType switch
                {
                    StockMovementType.Entry => movement.Quantity,
                    StockMovementType.Exit => -movement.Quantity,
                    StockMovementType.Adjustment => 0,
                    _ => 0
                });

            inventoryItem.QuantityAvailable = lastAdjustment.Quantity + subsequentMovementsTotal;
        }
        else
        {
            inventoryItem.QuantityAvailable = total;
        }

        await _inventoryRepository.UpdateAsync(inventoryItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
