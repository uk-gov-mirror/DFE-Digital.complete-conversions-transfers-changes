using Dfe.Complete.Domain.Constants;
using Dfe.Complete.Infrastructure.Security.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Dfe.Complete.Security;

public static class CustomPolicies
{
    public static Dictionary<string, Action<AuthorizationPolicyBuilder>> PolicyCustomizations => new()
    {
        [UserPolicyConstants.CanCreateProjects] = builder => //Remove all access to create projects and any related sub-tasks, for example the URL projects/new
        {
            builder.RequireAuthenticatedUser();
            builder.RequireAssertion(_ => false);
        },
        [UserPolicyConstants.CanViewTeamProjectsUnassigned] = builder =>
        {
            builder.RequireAuthenticatedUser();
            builder.RequireAssertion(context =>
            {
                var user = context.User;
                return
                    user.IsInRole(UserRolesConstants.ManageTeam) &&
                    (user.IsInRole(UserRolesConstants.RegionalCaseworkServices) || user.IsInRole(UserRolesConstants.RegionalDeliveryOfficer));
            });
        }
    };
}