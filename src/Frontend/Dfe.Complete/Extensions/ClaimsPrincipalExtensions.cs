using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Application.Users.Queries.GetUser;
using Dfe.Complete.Domain.Constants;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Utils;
using Dfe.Complete.Utils.Exceptions;
using MediatR;
using System.Security.Claims;

namespace Dfe.Complete.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserOid(this ClaimsPrincipal value)
        {
            var userAdId = value.Claims.SingleOrDefault(c => c.Type.Contains("objectidentifier"))?.Value;
            return userAdId ?? throw new InvalidOperationException("User does not have an objectidentifier claim.");
        }

        public static UserId GetUserId(this ClaimsPrincipal value)
        {
            var userId = value.FindFirstValue(CustomClaimTypeConstants.UserId) is { } id
                ? new UserId(Guid.Parse(id))
                : null;

            return userId ?? throw new InvalidOperationException("Could not retrieve user ID.");
        }

        public static async Task<ProjectTeam> GetUserTeam(this ClaimsPrincipal value, ISender sender)
        {
            var user = await value.GetUser(sender);
            return EnumExtensions.FromDescription<ProjectTeam>(user.Team);
        }

        public static async Task<UserDto> GetUser(this ClaimsPrincipal value, ISender sender)
        {
            var objectId = value.GetUserOid();

            var request = new GetUserByOidQuery(objectId);
            var userResult = await sender.Send(request);

            if (!userResult.IsSuccess || userResult.Value == null)
            {
                throw new NotFoundException(userResult.Error ?? "User not found.");
            }

            return userResult.Value;
        }
    }
}
