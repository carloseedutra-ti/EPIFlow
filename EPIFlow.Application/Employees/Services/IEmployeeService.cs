using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Employees.DTOs;

namespace EPIFlow.Application.Employees.Services;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(string? searchTerm, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(EmployeeCreateDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmployeeUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
