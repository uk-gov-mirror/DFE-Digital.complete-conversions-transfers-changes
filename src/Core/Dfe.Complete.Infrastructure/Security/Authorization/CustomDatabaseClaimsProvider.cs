using Dfe.Complete.Domain.Constants;
using Dfe.Complete.Domain.Entities;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.Extensions;
using Dfe.Complete.Domain.Interfaces.Repositories;
using Dfe.Complete.Utils;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Dfe.Complete.Infrastructure.Security.Authorization
{
    public class CustomDatabaseClaimsProvider(ICompleteRepository<User> userRepository, IMemoryCache cache)
        : ICustomClaimProvider
    {
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public async Task<IEnumerable<Claim>> GetClaimsAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (string.IsNullOrEmpty(userId))
                return [];

            string cacheKey = $"UserClaims_{userId}";

            if (!cache.TryGetValue(cacheKey, out List<Claim>? additionalClaims))
            {
                // Try to find user by OID first
                var userRecord = await userRepository.FindAsync(u => u.EntraUserObjectId == userId);
                var email = principal.FindFirst(CustomClaimTypeConstants.PreferredUsername)?.Value;

                // If the email doesn't match (claims vs DB) - reject.
                // Persons name probably changed and there is probably an orphaned record. Contact service support
                if (userRecord != null && !string.Equals(userRecord.Email, email, StringComparison.OrdinalIgnoreCase))
                    return [];

                // If there was no OID match but there was an email match, this is probably first login. 
                if (userRecord == null! && !string.IsNullOrEmpty(email))
                {
                    userRecord = await userRepository.FindAsync(u => u.Email == email);
                    if (userRecord != null)
                    {
                        userRecord.EntraUserObjectId = userId;
                        await userRepository.UpdateAsync(userRecord);
                    }
                }

                // If no OID or email match, reject
                if (userRecord == null!)
                    return [];

                additionalClaims =
                [
                    new (CustomClaimTypeConstants.UserId, userRecord.Id.Value.ToString())
                ];


                if (!string.IsNullOrEmpty(userRecord.Team))
                {
                    additionalClaims.Add(new Claim(ClaimTypes.Role, userRecord.Team));
                }

                var userTeam = EnumExtensions.FromDescription<ProjectTeam>(userRecord.Team);
                if (userTeam.TeamIsRegionalDeliveryOfficer())
                    additionalClaims.Add(new Claim(ClaimTypes.Role, UserRolesConstants.RegionalDeliveryOfficer));

                if (userRecord.ManageTeam == true)
                    additionalClaims.Add(new Claim(ClaimTypes.Role, UserRolesConstants.ManageTeam));

                if (userRecord.AddNewProject)
                    additionalClaims.Add(new Claim(ClaimTypes.Role, UserRolesConstants.AddNewProject));

                if (userRecord.AssignToProject == true)
                    additionalClaims.Add(new Claim(ClaimTypes.Role, UserRolesConstants.AssignToProject));

                if (userRecord.ManageUserAccounts == true)
                    additionalClaims.Add(new Claim(ClaimTypes.Role, UserRolesConstants.ManageUserAccounts));

                if (userRecord.ManageConversionUrns == true)
                    additionalClaims.Add(new Claim(ClaimTypes.Role, UserRolesConstants.ManageConversionUrns));

                if (userRecord.ManageLocalAuthorities == true)
                    additionalClaims.Add(new Claim(ClaimTypes.Role, UserRolesConstants.ManageLocalAuthorities));

                cache.Set(cacheKey, additionalClaims, _cacheDuration);
            }

            return additionalClaims ?? [];
        }
    }
}
