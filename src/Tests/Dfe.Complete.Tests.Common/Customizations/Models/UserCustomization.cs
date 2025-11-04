using AutoFixture;
using Dfe.Complete.Domain.Entities;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Tests.Common.Customizations.Behaviours;
using Dfe.Complete.Utils;

namespace Dfe.Complete.Tests.Common.Customizations.Models
{
    public class UserCustomization : ICustomization
    {
        public UserId Id { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool? ManageTeam { get; set; }

        public bool AddNewProject { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? ActiveDirectoryUserId { get; set; }

        public string? EntraUserObjectId { get; set; }

        public bool? AssignToProject { get; set; }

        public bool? ManageUserAccounts { get; set; }

        public string? ActiveDirectoryUserGroupIds { get; set; }

        public string? Team { get; set; }

        public DateTime? DeactivatedAt { get; set; }

        public bool? ManageConversionUrns { get; set; }

        public bool? ManageLocalAuthorities { get; set; }

        public DateTime? LatestSession { get; set; }

        public void Customize(IFixture fixture)
        {
            fixture.Customize(new IgnoreVirtualMembersCustomisation())
                   .Customize<User>(composer => composer
                   .With(x => x.Id, fixture.Create<UserId>())
                   .With(x => x.Email, fixture.Create<string>())
                   .With(x => x.CreatedAt, fixture.Create<DateTime>())
                   .With(x => x.UpdatedAt, fixture.Create<DateTime>())
                   .With(x => x.ManageTeam, fixture.Create<bool?>())
                   .With(x => x.AddNewProject, fixture.Create<bool>())
                   .With(x => x.FirstName, fixture.Create<string>())
                   .With(x => x.LastName, fixture.Create<string>())
                   .With(x => x.ActiveDirectoryUserId, fixture.Create<string>())
                   .With(x => x.EntraUserObjectId, fixture.Create<string>())
                   .With(x => x.AssignToProject, fixture.Create<bool?>())
                   .With(x => x.ManageUserAccounts, fixture.Create<bool?>())
                   .With(x => x.ActiveDirectoryUserGroupIds, fixture.Create<string>())
                   .With(x => x.Team, fixture.Create<ProjectTeam>().ToDescription())
                   .With(x => x.DeactivatedAt, fixture.Create<DateTime?>())
                   .With(x => x.ManageConversionUrns, fixture.Create<bool?>())
                   .With(x => x.ManageLocalAuthorities, fixture.Create<bool?>())
                   .With(x => x.LatestSession, fixture.Create<DateTime?>()));
        }
    }
}
