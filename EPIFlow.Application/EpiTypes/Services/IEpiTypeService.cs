using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.EpiTypes.DTOs;

namespace EPIFlow.Application.EpiTypes.Services;

public interface IEpiTypeService
{
    Task<IReadOnlyList<EpiTypeDto>> GetAllAsync(string? searchTerm, CancellationToken cancellationToken = default);
    Task<EpiTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(EpiTypeCreateDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(EpiTypeUpdateDto dto, CancellationToken cancellationToken = default);
    Task ToggleStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
