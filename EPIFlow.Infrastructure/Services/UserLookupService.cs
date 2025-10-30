using System;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Infrastructure.Services;

public class UserLookupService : IUserLookupService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserLookupService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(applicationUser => applicationUser.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
    }
}
