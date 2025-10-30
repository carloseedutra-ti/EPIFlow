using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.Common.Models;
using EPIFlow.Application.Tenants.DTOs;
using EPIFlow.Domain.Constants;
using EPIFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Application.Tenants.Services;

public class TenantManagementService : ITenantManagementService
{
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EpiDelivery> _deliveryRepository;
    private readonly IRepository<EpiType> _epiTypeRepository;
    private readonly IRepository<TenantPayment> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlatformUserService _platformUserService;

    public TenantManagementService(
        IRepository<Tenant> tenantRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EpiDelivery> deliveryRepository,
        IRepository<EpiType> epiTypeRepository,
        IRepository<TenantPayment> paymentRepository,
        IUnitOfWork unitOfWork,
        IPlatformUserService platformUserService)
    {
        _tenantRepository = tenantRepository;
        _employeeRepository = employeeRepository;
        _deliveryRepository = deliveryRepository;
        _epiTypeRepository = epiTypeRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _platformUserService = platformUserService;
    }

    public async Task<TenantDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantsQuery = _tenantRepository.AsQueryable().IgnoreQueryFilters();
        var totalTenants = await tenantsQuery.CountAsync(cancellationToken);
        var activeTenants = await tenantsQuery.Where(t => t.IsActive && !t.IsSuspended).CountAsync(cancellationToken);
        var suspendedTenants = await tenantsQuery.Where(t => t.IsSuspended).CountAsync(cancellationToken);
        var blockedTenants = await tenantsQuery.Where(t => !t.IsActive).CountAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var paymentsQuery = _paymentRepository.AsQueryable().IgnoreQueryFilters();
        var totalPaymentsOverall = await paymentsQuery.SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
        var totalPaymentsLast30Days = await paymentsQuery
            .Where(payment => payment.PaymentDateUtc >= now.AddDays(-30))
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;

        return new TenantDashboardDto(
            totalTenants,
            activeTenants,
            suspendedTenants,
            blockedTenants,
            totalPaymentsLast30Days,
            totalPaymentsOverall);
    }

    public async Task<IReadOnlyList<TenantListItemDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _tenantRepository.AsQueryable().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(tenant =>
                tenant.Name.Contains(search) ||
                (tenant.LegalName != null && tenant.LegalName.Contains(search)) ||
                (tenant.ResponsibleEmail != null && tenant.ResponsibleEmail.Contains(search)));
        }

        var tenants = await query
            .OrderBy(tenant => tenant.Name)
            .ToListAsync(cancellationToken);

        var tenantIds = tenants.Select(t => t.Id).ToList();

        var employees = await _employeeRepository.AsQueryable()
            .IgnoreQueryFilters()
            .Where(employee => tenantIds.Contains(employee.TenantId) && !employee.IsDeleted)
            .GroupBy(employee => employee.TenantId)
            .Select(group => new { TenantId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var employeesLookup = employees.ToDictionary(item => item.TenantId, item => item.Count);

        return tenants
            .Select(tenant => new TenantListItemDto(
                tenant.Id,
                tenant.Name,
                tenant.Document,
                tenant.ResponsibleEmail,
                tenant.IsActive,
                tenant.IsSuspended,
                tenant.EmployeeLimit,
                employeesLookup.TryGetValue(tenant.Id, out var count) ? count : 0,
                tenant.ActiveSinceUtc,
                tenant.LogoPath))
            .ToList();
    }

    public async Task<TenantDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.AsQueryable()
            .IgnoreQueryFilters()
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        var activeEmployees = await _employeeRepository.AsQueryable()
            .IgnoreQueryFilters()
            .Where(employee => employee.TenantId == id && !employee.IsDeleted)
            .CountAsync(cancellationToken);

        var deliveriesCount = await _deliveryRepository.AsQueryable()
            .IgnoreQueryFilters()
            .Where(delivery => delivery.TenantId == id)
            .CountAsync(cancellationToken);

        var epiTypesCount = await _epiTypeRepository.AsQueryable()
            .IgnoreQueryFilters()
            .Where(epi => epi.TenantId == id)
            .CountAsync(cancellationToken);

        PlatformUserInfo? adminUser = await _platformUserService.GetTenantAdministratorAsync(id, cancellationToken);

        var adminDto = adminUser is null ? null : new TenantAdminUserDto(
            adminUser.Id,
            adminUser.Email,
            adminUser.FullName,
            adminUser.IsActive);

        var payments = tenant.Payments
            .OrderByDescending(payment => payment.PaymentDateUtc)
            .Select(payment => new TenantPaymentDto(
                payment.Id,
                payment.Amount,
                payment.PaymentDateUtc,
                payment.Reference,
                payment.Notes,
                payment.CreatedAtUtc))
            .ToList();

        return new TenantDetailDto(
            tenant.Id,
            tenant.Name,
            tenant.LegalName,
            tenant.Document,
            tenant.PhoneNumber,
            tenant.Address,
            tenant.AddressComplement,
            tenant.City,
            tenant.State,
            tenant.PostalCode,
            tenant.Country,
            tenant.ResponsibleName,
            tenant.ResponsibleEmail,
            tenant.IsActive,
            tenant.IsSuspended,
            tenant.EmployeeLimit,
            tenant.ActiveSinceUtc,
            tenant.SuspendedAtUtc,
            tenant.SubscriptionExpiresOnUtc,
            tenant.Notes,
            activeEmployees,
            deliveriesCount,
            epiTypesCount,
            payments,
            adminDto,
            tenant.LogoPath);
    }

    public async Task<Guid> CreateAsync(TenantCreateDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = new Tenant
        {
            Name = dto.Name.Trim(),
            LegalName = dto.LegalName?.Trim(),
            Document = dto.Document?.Trim(),
            Email = dto.ResponsibleEmail,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Address = dto.Address?.Trim(),
            AddressComplement = dto.AddressComplement?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            PostalCode = dto.PostalCode?.Trim(),
            Country = dto.Country?.Trim(),
            ResponsibleName = dto.ResponsibleName?.Trim(),
            ResponsibleEmail = dto.ResponsibleEmail?.Trim(),
            EmployeeLimit = dto.EmployeeLimit,
            Notes = dto.Notes?.Trim(),
            IsActive = true,
            ActiveSinceUtc = DateTime.UtcNow,
            LogoPath = dto.LogoPath
        };

        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _platformUserService.EnsureRoleExistsAsync(SystemRoles.Administrator, cancellationToken);
        await _platformUserService.EnsureRoleExistsAsync(SystemRoles.Warehouse, cancellationToken);
        await _platformUserService.EnsureRoleExistsAsync(SystemRoles.User, cancellationToken);

        if (await _platformUserService.UserExistsAsync(dto.AdminEmail, cancellationToken))
        {
            throw new ConflictException("Já existe um usuário com o e-mail informado para o administrador da empresa.");
        }

        await _platformUserService.CreateTenantAdministratorAsync(
            tenant.Id,
            dto.AdminName,
            dto.AdminEmail,
            dto.AdminPassword,
            cancellationToken);

        return tenant.Id;
    }

    public async Task UpdateAsync(TenantUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.AsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException("Empresa não encontrada.");
        }

        tenant.Name = dto.Name.Trim();
        tenant.LegalName = dto.LegalName?.Trim();
        tenant.Document = dto.Document?.Trim();
        tenant.Email = dto.ResponsibleEmail;
        tenant.PhoneNumber = dto.PhoneNumber?.Trim();
        tenant.Address = dto.Address?.Trim();
        tenant.AddressComplement = dto.AddressComplement?.Trim();
        tenant.City = dto.City?.Trim();
        tenant.State = dto.State?.Trim();
        tenant.PostalCode = dto.PostalCode?.Trim();
        tenant.Country = dto.Country?.Trim();
        tenant.ResponsibleName = dto.ResponsibleName?.Trim();
        tenant.ResponsibleEmail = dto.ResponsibleEmail?.Trim();
        tenant.EmployeeLimit = dto.EmployeeLimit;
        tenant.IsActive = dto.IsActive;
        tenant.IsSuspended = dto.IsSuspended;
        tenant.SubscriptionExpiresOnUtc = dto.SubscriptionExpiresOnUtc;
        tenant.Notes = dto.Notes?.Trim();
        tenant.LogoPath = string.IsNullOrWhiteSpace(dto.LogoPath) ? tenant.LogoPath : dto.LogoPath;

        if (!tenant.IsSuspended)
        {
            tenant.SuspendedAtUtc = null;
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetSuspendedAsync(Guid id, bool isSuspended, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.AsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException("Empresa não encontrada.");
        }

        tenant.IsSuspended = isSuspended;
        tenant.SuspendedAtUtc = isSuspended ? DateTime.UtcNow : null;

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.AsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException("Empresa não encontrada.");
        }

        tenant.IsActive = isActive;
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPaymentAsync(TenantPaymentCreateDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.AsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == dto.TenantId, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException("Empresa não encontrada.");
        }

        var payment = new TenantPayment
        {
            TenantId = dto.TenantId,
            Amount = dto.Amount,
            PaymentDateUtc = dto.PaymentDate.ToUniversalTime(),
            Reference = dto.Reference?.Trim(),
            Notes = dto.Notes?.Trim()
        };

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

}
