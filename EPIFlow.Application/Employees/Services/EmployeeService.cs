using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Exceptions;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Application.Employees.DTOs;
using EPIFlow.Domain.Entities;
using EPIFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Application.Employees.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public EmployeeService(
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(string? searchTerm, CancellationToken cancellationToken = default)
    {
        var query = _employeeRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(employee =>
                employee.Name.Contains(term) ||
                employee.Cpf.Contains(term) ||
                employee.RegistrationNumber.Contains(term));
        }

        return await query
            .AsNoTracking()
            .OrderBy(employee => employee.Name)
            .Select(employee => new EmployeeDto(
                employee.Id,
                employee.Name,
                employee.Cpf,
                employee.RegistrationNumber,
                employee.JobTitle,
                employee.Department,
                employee.AdmissionDate,
                employee.Status,
                employee.Deliveries.Count(delivery => !delivery.IsDeleted)))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _employeeRepository.AsQueryable();

        return await query
            .AsNoTracking()
            .Where(employee => employee.Id == id)
            .Select(employee => new EmployeeDto(
                employee.Id,
                employee.Name,
                employee.Cpf,
                employee.RegistrationNumber,
                employee.JobTitle,
                employee.Department,
                employee.AdmissionDate,
                employee.Status,
                employee.Deliveries.Count(delivery => !delivery.IsDeleted)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(EmployeeCreateDto dto, CancellationToken cancellationToken = default)
    {
        var exists = await _employeeRepository.AsQueryable()
            .AnyAsync(employee => employee.Cpf == dto.Cpf || employee.RegistrationNumber == dto.RegistrationNumber, cancellationToken);

        if (exists)
        {
            throw new ConflictException("Já existe um colaborador com o mesmo CPF ou matrícula.");
        }

        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue || tenantId == Guid.Empty)
        {
            throw new ValidationException("Não foi possível identificar a empresa do colaborador.",
                new Dictionary<string, string[]> { { "Tenant", new[] { "Faça login novamente e tente novamente." } } });
        }

        var entity = new Employee
        {
            Name = dto.Name.Trim(),
            Cpf = dto.Cpf.Trim(),
            RegistrationNumber = dto.RegistrationNumber.Trim(),
            JobTitle = dto.JobTitle?.Trim(),
            Department = dto.Department?.Trim(),
            AdmissionDate = dto.AdmissionDate,
            Status = EmployeeStatus.Active,
            TenantId = tenantId.Value
        };

        await _employeeRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task UpdateAsync(EmployeeUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _employeeRepository.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException("Colaborador não encontrado.");
        }

        var exists = await _employeeRepository.AsQueryable()
            .AnyAsync(employee =>
                employee.Id != dto.Id &&
                (employee.Cpf == dto.Cpf || employee.RegistrationNumber == dto.RegistrationNumber), cancellationToken);

        if (exists)
        {
            throw new ConflictException("Já existe outro colaborador com o mesmo CPF ou matrícula.");
        }

        entity.Name = dto.Name.Trim();
        entity.Cpf = dto.Cpf.Trim();
        entity.RegistrationNumber = dto.RegistrationNumber.Trim();
        entity.JobTitle = dto.JobTitle?.Trim();
        entity.Department = dto.Department?.Trim();
        entity.AdmissionDate = dto.AdmissionDate;
        entity.Status = dto.Status;

        await _employeeRepository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _employeeRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException("Colaborador não encontrado.");
        }

        await _employeeRepository.RemoveAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
