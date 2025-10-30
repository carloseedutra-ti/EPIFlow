using System;
using System.Threading;
using System.Threading.Tasks;

namespace EPIFlow.Application.Common.Interfaces;

public interface IUserLookupService
{
    Task<string?> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default);
}
