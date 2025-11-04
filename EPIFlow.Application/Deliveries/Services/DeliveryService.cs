using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.Deliveries.DTOs;
using EPIFlow.Application.Deliveries.DTOs.Requests;
using EPIFlow.Domain.Entities;
using EPIFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Application.Deliveries.Services;

public class DeliveryService : IDeliveryService
{
    private readonly IRepository<EpiDelivery> _deliveryRepository;
    private readonly IRepository<DeliveryItem> _deliveryItemRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IRepository<StockMovement> _movementRepository;
    private readonly IRepository<EpiType> _epiTypeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserLookupService _userLookupService;
    private readonly IUnitOfWork _unitOfWork;

    public DeliveryService(
        IRepository<EpiDelivery> deliveryRepository,
        IRepository<DeliveryItem> deliveryItemRepository,
        IRepository<Employee> employeeRepository,
        IRepository<InventoryItem> inventoryRepository,
        IRepository<StockMovement> movementRepository,
        IRepository<EpiType> epiTypeRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUserLookupService userLookupService,
        IUnitOfWork unitOfWork)
    {
        _deliveryRepository = deliveryRepository;
        _deliveryItemRepository = deliveryItemRepository;
        _employeeRepository = employeeRepository;
        _inventoryRepository = inventoryRepository;
        _movementRepository = movementRepository;
        _epiTypeRepository = epiTypeRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _userLookupService = userLookupService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<DeliveryDto>> GetAllAsync(Guid? employeeId, Guid? epiTypeId, CancellationToken cancellationToken = default)
    {
        var query = _deliveryRepository.AsQueryable();

        query = query
            .Include(delivery => delivery.Employee)
            .Include(delivery => delivery.Items)
                .ThenInclude(item => item.EpiType);

        if (employeeId.HasValue)
        {
            query = query.Where(delivery => delivery.EmployeeId == employeeId.Value);
        }

        if (epiTypeId.HasValue)
        {
            query = query.Where(delivery => delivery.Items.Any(item => item.EpiTypeId == epiTypeId.Value));
        }

        var deliveries = await query
            .AsNoTracking()
            .OrderByDescending(delivery => delivery.DeliveryDate)
            .ToListAsync(cancellationToken);

        var responsibleNames = new Dictionary<Guid, string>();
        var result = new List<DeliveryDto>(deliveries.Count);

        foreach (var delivery in deliveries)
        {
            var responsibleName = await ResolveUserNameAsync(delivery.ResponsibleUserId, responsibleNames, cancellationToken);

            result.Add(new DeliveryDto(
                delivery.Id,
                delivery.EmployeeId,
                delivery.Employee?.Name ?? string.Empty,
                delivery.DeliveryDate,
                delivery.ResponsibleUserId,
                responsibleName,
                delivery.DeliveryNumber,
                delivery.Notes,
                delivery.Items
                    .Where(item => !item.IsDeleted)
                    .Select(item => new DeliveryItemDto(
                        item.Id,
                        item.EpiTypeId,
                        item.EpiType?.Code ?? string.Empty,
                        item.EpiType?.Description ?? string.Empty,
                        item.Quantity,
                        item.ValidUntil,
                        item.ReturnedAtUtc,
                        item.ReturnedQuantity))
                    .ToList()));
        }

        return result;
    }

    public async Task<DeliveryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var delivery = await _deliveryRepository.AsQueryable()
            .Include(entity => entity.Employee)
            .Include(entity => entity.Items)
                .ThenInclude(item => item.EpiType)
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (delivery is null)
        {
            return null;
        }

        var responsibleName = await ResolveUserNameAsync(delivery.ResponsibleUserId, new Dictionary<Guid, string>(), cancellationToken);

        return new DeliveryDto(
            delivery.Id,
            delivery.EmployeeId,
            delivery.Employee?.Name ?? string.Empty,
            delivery.DeliveryDate,
            delivery.ResponsibleUserId,
            responsibleName,
            delivery.DeliveryNumber,
            delivery.Notes,
            delivery.Items
                .Where(item => !item.IsDeleted)
                .Select(item => new DeliveryItemDto(
                    item.Id,
                    item.EpiTypeId,
                    item.EpiType?.Code ?? string.Empty,
                    item.EpiType?.Description ?? string.Empty,
                    item.Quantity,
                    item.ValidUntil,
                    item.ReturnedAtUtc,
                    item.ReturnedQuantity))
                .ToList());
    }

    public async Task<Guid> RegisterDeliveryAsync(DeliveryCreateDto dto, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException("Colaborador não encontrado para a entrega.");
        }

        var responsibleUserId = _currentUserService.GetUserId();
        if (!responsibleUserId.HasValue)
        {
            throw new ValidationException("Usuário responsável não identificado.", new Dictionary<string, string[]> { { "User", new[] { "Usuário não autenticado." } } });
        }

        var tenantId = employee.TenantId;

        var delivery = new EpiDelivery
        {
            EmployeeId = dto.EmployeeId,
            DeliveryDate = dto.DeliveryDate,
            ResponsibleUserId = responsibleUserId.Value,
            DeliveryNumber = dto.DeliveryNumber,
            Notes = dto.Notes,
            TenantId = tenantId
        };

        foreach (var itemDto in dto.Items)
        {
            var movementTimestamp = _dateTimeProvider.UtcNow;

            var inventoryItem = await _inventoryRepository.AsQueryable()
                .Include(item => item.EpiType)
                .FirstOrDefaultAsync(item => item.EpiTypeId == itemDto.EpiTypeId, cancellationToken);

            if (inventoryItem is null)
            {
                throw new NotFoundException("EPI não encontrado no estoque.");
            }

            if (!inventoryItem.EpiType!.IsActive)
            {
                throw new ValidationException(
                    "EPI inativo não pode ser entregue.",
                    new Dictionary<string, string[]> { { nameof(itemDto.EpiTypeId), new[] { "O EPI selecionado está inativo." } } });
            }

            if (inventoryItem.QuantityAvailable < itemDto.Quantity)
            {
                throw new ValidationException(
                    "Quantidade insuficiente no estoque.",
                    new Dictionary<string, string[]> { { nameof(itemDto.Quantity), new[] { "Quantidade informada excede o saldo disponível." } } });
            }

            inventoryItem.QuantityAvailable -= itemDto.Quantity;

            var deliveryItem = new DeliveryItem
            {
                EpiTypeId = itemDto.EpiTypeId,
                Quantity = itemDto.Quantity,
                ValidUntil = itemDto.ValidUntil,
                TenantId = tenantId
            };

            delivery.Items.Add(deliveryItem);

            var movement = new StockMovement
            {
                InventoryItemId = inventoryItem.Id,
                MovementType = StockMovementType.Exit,
                Quantity = itemDto.Quantity,
                MovementDate = movementTimestamp,
                Reference = delivery.DeliveryNumber,
                Notes = $"Entrega para colaborador {employee.Name}",
                TenantId = tenantId
            };

            await _inventoryRepository.UpdateAsync(inventoryItem, cancellationToken);
            await _movementRepository.AddAsync(movement, cancellationToken);
        }

        await _deliveryRepository.AddAsync(delivery, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return delivery.Id;
    }

    public async Task ReturnItemsAsync(IEnumerable<DeliveryReturnDto> items, CancellationToken cancellationToken = default)
    {
        var itemsList = items.ToList();
        if (!itemsList.Any())
        {
            return;
        }

        var itemIds = itemsList.Select(item => item.DeliveryItemId).ToList();

        var deliveryItems = await _deliveryItemRepository.AsQueryable()
            .Include(item => item.Delivery)
            .Include(item => item.EpiType)
            .Where(item => itemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        foreach (var returnDto in itemsList)
        {
            var deliveryItem = deliveryItems.FirstOrDefault(item => item.Id == returnDto.DeliveryItemId);
            if (deliveryItem is null)
            {
                throw new NotFoundException("Item de entrega não encontrado.");
            }

            var tenantId = deliveryItem.TenantId != Guid.Empty
                ? deliveryItem.TenantId
                : deliveryItem.Delivery?.TenantId ?? Guid.Empty;

            if (deliveryItem.ReturnedQuantity.HasValue)
            {
                throw new ValidationException(
                    "Item já devolvido anteriormente.",
                    new Dictionary<string, string[]> { { nameof(returnDto.DeliveryItemId), new[] { "Este item já foi devolvido." } } });
            }

            if (returnDto.ReturnedQuantity > deliveryItem.Quantity)
            {
                throw new ValidationException(
                    "Quantidade devolvida inválida.",
                    new Dictionary<string, string[]> { { nameof(returnDto.ReturnedQuantity), new[] { "Quantidade devolvida não pode exceder a quantidade entregue." } } });
            }

            deliveryItem.ReturnedQuantity = returnDto.ReturnedQuantity;
            deliveryItem.ReturnedAtUtc = returnDto.ReturnedAtUtc;

            var inventoryItem = await _inventoryRepository.AsQueryable()
                .FirstOrDefaultAsync(item => item.EpiTypeId == deliveryItem.EpiTypeId, cancellationToken);

            var isNewInventoryItem = false;

            if (inventoryItem is null)
            {
                inventoryItem = new InventoryItem
                {
                    EpiTypeId = deliveryItem.EpiTypeId,
                    QuantityAvailable = 0,
                    MinimumQuantity = 0,
                    TenantId = tenantId
                };

                await _inventoryRepository.AddAsync(inventoryItem, cancellationToken);
                isNewInventoryItem = true;
            }

            inventoryItem.QuantityAvailable += returnDto.ReturnedQuantity;

            var movement = new StockMovement
            {
                InventoryItemId = inventoryItem.Id,
                MovementType = StockMovementType.Entry,
                Quantity = returnDto.ReturnedQuantity,
                MovementDate = returnDto.ReturnedAtUtc,
                Reference = deliveryItem.Delivery?.DeliveryNumber,
                Notes = "Devolução de EPI",
                TenantId = tenantId
            };

            if (!isNewInventoryItem)
            {
                await _inventoryRepository.UpdateAsync(inventoryItem, cancellationToken);
            }

            await _movementRepository.AddAsync(movement, cancellationToken);
            await _deliveryItemRepository.UpdateAsync(deliveryItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> ResolveUserNameAsync(Guid userId, IDictionary<Guid, string> cache, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        var userName = await _userLookupService.GetUserNameAsync(userId, cancellationToken) ?? "Usuário";
        cache[userId] = userName;
        return userName;
    }
}
