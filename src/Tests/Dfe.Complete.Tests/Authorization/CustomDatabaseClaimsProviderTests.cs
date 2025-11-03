using Dfe.Complete.Domain.Constants;
using Dfe.Complete.Domain.Entities;
using Dfe.Complete.Domain.Interfaces.Repositories;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Infrastructure.Security.Authorization;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Dfe.Complete.Tests.Authorization
{
    public class CustomDatabaseClaimsProviderTests
    {
        private readonly ICompleteRepository<User> _repository;
        private readonly ICustomClaimProvider _provider;
        private readonly IFixture _fixture;

        public CustomDatabaseClaimsProviderTests()
        {
            _repository = Substitute.For<ICompleteRepository<User>>();
            // Use a real MemoryCache instance.
            IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
            _provider = new CustomDatabaseClaimsProvider(_repository, memoryCache);
            _fixture = new Fixture();
        }

        [Fact]
        public async Task GetClaimsAsync_NoUserId_ReturnsEmpty()
        {
            // Arrange: Create a principal without the object identifier claim.
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);

            // Act
            var claims = await _provider.GetClaimsAsync(principal);

            // Assert
            Assert.Empty(claims);
        }

        [Fact]
        public async Task GetClaimsAsync_UserNotFound_ReturnsEmpty()
        {
            // Arrange
            var userId = "123";
            var identity = new ClaimsIdentity(
            [
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId)
            ]);
            var principal = new ClaimsPrincipal(identity);

            // Repository returns null to simulate a user not found.
            _repository.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                       .Returns(Task.FromResult<User>(null!));

            // Act
            var claims = await _provider.GetClaimsAsync(principal);

            // Assert
            Assert.Empty(claims);
        }

        [Fact]
        public async Task GetClaimsAsync_UserFound_ReturnsExpectedClaims()
        {
            // Arrange
            var userId = "00000000-0000-0000-0000-000000000123";
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId)
            });
            var principal = new ClaimsPrincipal(identity);

            // Create a user record with specific properties.
            var userRecord = new User
            {
                Id = new UserId(new Guid(userId)),
                ActiveDirectoryUserId = userId,
                Team = "TeamA",
                ManageTeam = true,
                AddNewProject = true,
                AssignToProject = false,
                ManageUserAccounts = true,
                ManageConversionUrns = false,
                ManageLocalAuthorities = true
            };

            _repository.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                       .Returns(Task.FromResult(userRecord));

            // Act
            var claims = await _provider.GetClaimsAsync(principal);

            // Assert: Verify the expected claims are present.
            var collection = claims as Claim[] ?? claims.ToArray();

            Assert.NotEmpty(collection);
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "TeamA");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_team");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "add_new_project");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_user_accounts");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_local_authorities");

            // Verify claims that should not be present.
            Assert.DoesNotContain(collection, c => c.Type == ClaimTypes.Role && c.Value == "assign_to_project");
            Assert.DoesNotContain(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_conversion_urns");
        }

        [Fact]
        public async Task GetClaimsAsync_UserFound_ReturnsAllClaims()
        {
            // Arrange
            var userId = "00000000-0000-0000-0000-000000001234";
            var identity = new ClaimsIdentity(
            [
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId)
            ]);
            var principal = new ClaimsPrincipal(identity);

            // Create a user record with specific properties.
            var userRecord = new User
            {
                Id = new UserId(new Guid(userId)),
                ActiveDirectoryUserId = userId,
                Team = "london",
                ManageTeam = true,
                AddNewProject = true,
                AssignToProject = true,
                ManageUserAccounts = true,
                ManageConversionUrns = true,
                ManageLocalAuthorities = true
            };

            _repository.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                       .Returns(Task.FromResult(userRecord));

            // Act
            var claims = await _provider.GetClaimsAsync(principal);

            // Assert: Verify the expected claims are present.
            var collection = claims as Claim[] ?? claims.ToArray();

            Assert.NotEmpty(collection);
            Assert.Contains(collection, c => c.Type == CustomClaimTypeConstants.UserId && c.Value == "00000000-0000-0000-0000-000000001234");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "london");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_team");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "add_new_project");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_user_accounts");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "regional_delivery_officer");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_local_authorities");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "assign_to_project");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_conversion_urns");
        }

        [Fact]
        public async Task GetClaimsAsync_UserFoundByOid_EmailMismatch_ReturnsEmpty()
        {
            // Arrange
            var userId = "00000000-0000-0000-0000-000000000123";
            var userEmail = "user@example.com";
            var claimEmail = "different@example.com"; // Different email in claims
            
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId),
                new Claim(CustomClaimTypeConstants.PreferredUsername, claimEmail)
            });
            var principal = new ClaimsPrincipal(identity);

            var userRecord = new User
            {
                Id = new UserId(new Guid(userId)),
                EntraUserObjectId = userId,
                Email = userEmail, // Different from claim email
                ActiveDirectoryUserId = userId,
                Team = "TeamA"
            };

            // Setup repository to return user on first call (OID lookup)
            _repository.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                       .Returns(Task.FromResult(userRecord));

            // Act
            var claims = await _provider.GetClaimsAsync(principal);

            // Assert: Should return empty due to email mismatch
            Assert.Empty(claims);
        }

        [Fact]
        public async Task GetClaimsAsync_NoOidMatch_EmailMatch_UpdatesOidAndReturnsClaims()
        {
            // Arrange
            var userId = "00000000-0000-0000-0000-000000000123";
            var userEmail = "user@example.com";
            
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId),
                new Claim(CustomClaimTypeConstants.PreferredUsername, userEmail)
            });
            var principal = new ClaimsPrincipal(identity);

            var userRecord = new User
            {
                Id = new UserId(Guid.NewGuid()),
                EntraUserObjectId = null, // No OID set initially
                Email = userEmail,
                ActiveDirectoryUserId = "old-ad-id",
                Team = "TeamA",
                ManageTeam = true,
                AddNewProject = false
            };

            // Setup repository calls in sequence - first OID lookup (null), then email lookup (user found)
            _repository.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                       .Returns(Task.FromResult<User>(null!), Task.FromResult(userRecord));

            // Act
            var claims = await _provider.GetClaimsAsync(principal);

            // Assert: Should update OID and return claims
            Assert.NotEmpty(claims);
            
            // Verify OID was updated
            await _repository.Received(1).UpdateAsync(Arg.Is<User>(u => u.EntraUserObjectId == userId));
            
            // Verify expected claims
            var collection = claims as Claim[] ?? claims.ToArray();
            Assert.Contains(collection, c => c.Type == CustomClaimTypeConstants.UserId && c.Value == userRecord.Id.Value.ToString());
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "TeamA");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_team");
            Assert.DoesNotContain(collection, c => c.Type == ClaimTypes.Role && c.Value == "add_new_project");
        }

        [Fact]
        public async Task GetClaimsAsync_NoOidMatch_NoEmailMatch_ReturnsEmpty()
        {
            // Arrange
            var userId = "00000000-0000-0000-0000-000000000123";
            var userEmail = "user@example.com";
            
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId),
                new Claim(CustomClaimTypeConstants.PreferredUsername, userEmail)
            });
            var principal = new ClaimsPrincipal(identity);

            // Setup repository to return null on both OID and email lookups
            _repository.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                       .Returns(Task.FromResult<User>(null!));

            // Act
            var claims = await _provider.GetClaimsAsync(principal);

            // Assert: Should return empty as no user found
            Assert.Empty(claims);
            
            // Verify no update was attempted
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task GetClaimsAsync_NoOidMatch_EmptyEmail_ReturnsEmpty()
        {
            // Arrange
            var userId = "00000000-0000-0000-0000-000000000123";
            
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId)
                // No email claim
            });
            var principal = new ClaimsPrincipal(identity);

            // Setup repository to return null on OID lookup
            _repository.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                       .Returns(Task.FromResult<User>(null!));

            // Act
            var claims = await _provider.GetClaimsAsync(principal);

            // Assert: Should return empty as no email to lookup
            Assert.Empty(claims);
            
            // Verify email lookup was not attempted
            await _repository.Received(1).FindAsync(Arg.Any<Expression<Func<User, bool>>>());
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task GetClaimsAsync_UserFoundByOid_EmailMatches_ReturnsClaimsWithoutUpdate()
        {
            // Arrange
            var userId = "00000000-0000-0000-0000-000000000123";
            var userEmail = "user@example.com";
            
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId),
                new Claim(CustomClaimTypeConstants.PreferredUsername, userEmail)
            });
            var principal = new ClaimsPrincipal(identity);

            var userRecord = new User
            {
                Id = new UserId(new Guid(userId)),
                EntraUserObjectId = userId,
                Email = userEmail, // Same as claim email
                ActiveDirectoryUserId = userId,
                Team = "TeamA",
                ManageTeam = false,
                AddNewProject = true
            };

            // Setup repository to return user on first call (OID lookup)
            _repository.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                       .Returns(Task.FromResult(userRecord));

            // Act
            var claims = await _provider.GetClaimsAsync(principal);

            // Assert: Should return claims without update
            Assert.NotEmpty(claims);
            
            // Verify no update was attempted (user already has OID and emails match)
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>());
            
            // Verify expected claims
            var collection = claims as Claim[] ?? claims.ToArray();
            Assert.Contains(collection, c => c.Type == CustomClaimTypeConstants.UserId && c.Value == userRecord.Id.Value.ToString());
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "TeamA");
            Assert.Contains(collection, c => c.Type == ClaimTypes.Role && c.Value == "add_new_project");
            Assert.DoesNotContain(collection, c => c.Type == ClaimTypes.Role && c.Value == "manage_team");
        }

        [Fact]
        public async Task GetClaimsAsync_CachesClaims_RepositoryCalledOnce()
        {
            // Arrange
            var userId = "00000000-0000-0000-0000-000000000123";
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId)
            });
            var principal = new ClaimsPrincipal(identity);

            var userRecord = new User
            {
                Id = new UserId(new Guid(userId)),
                ActiveDirectoryUserId = userId,
                Team = "TeamA",
                ManageTeam = true,
                AddNewProject = true,
                AssignToProject = false,
                ManageUserAccounts = true,
                ManageConversionUrns = false,
                ManageLocalAuthorities = true
            };

            _repository.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                       .Returns(Task.FromResult(userRecord));

            // Act: Call GetClaimsAsync twice.
            var claimsFirstCall = await _provider.GetClaimsAsync(principal);
            var claimsSecondCall = await _provider.GetClaimsAsync(principal);

            // Assert: The repository's FindAsync should be called only once due to caching.
            await _repository.Received(1).FindAsync(Arg.Any<Expression<Func<User, bool>>>());

            // Also, verify that the claims returned on both calls are the same.
            var first = claimsFirstCall.Select(c => $"{c.Type}:{c.Value}").OrderBy(x => x);
            var second = claimsSecondCall.Select(c => $"{c.Type}:{c.Value}").OrderBy(x => x);
            Assert.Equal(first, second);
        }
    }
}

