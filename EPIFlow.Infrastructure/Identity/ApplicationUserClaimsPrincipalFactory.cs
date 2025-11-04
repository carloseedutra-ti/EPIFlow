using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EPIFlow.Infrastructure.Identity;

public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.TenantId != Guid.Empty)
        {
            identity.AddClaim(new Claim("tenant_id", user.TenantId.ToString()));
        }
        else
        {
            identity.AddClaim(new Claim("tenant_id", Guid.Empty.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Role, Domain.Constants.SystemRoles.Administrator));
        }

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identity.AddClaim(new Claim("name", user.FullName));
        }

        if (!string.IsNullOrWhiteSpace(user.Department))
        {
            identity.AddClaim(new Claim("department", user.Department));
        }

        if (!string.IsNullOrWhiteSpace(user.JobTitle))
        {
            identity.AddClaim(new Claim("job_title", user.JobTitle));
        }

        if (user.DefaultBiometricAgentId.HasValue)
        {
            identity.AddClaim(new Claim("default_agent_id", user.DefaultBiometricAgentId.Value.ToString()));
        }

        return identity;
    }
}
