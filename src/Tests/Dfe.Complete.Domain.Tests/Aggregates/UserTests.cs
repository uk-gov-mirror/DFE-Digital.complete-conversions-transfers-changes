using Dfe.Complete.Domain.Entities;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Tests.Common.Customizations.Behaviours;
using Dfe.Complete.Tests.Common.Customizations.Models;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;

namespace Dfe.Complete.Domain.Tests.Aggregates
{
    public class UserTests
    {
        [Theory]
        [CustomAutoData(typeof(UserCustomization), typeof(IgnoreVirtualMembersCustomisation))]
        public void Create_ShouldCreateUserWithCorrectProperties(
            UserId id,
            string email,
            string firstName,
            string lastName,
            string team)
        {
            // Act
            var user = User.Create(id, email, firstName, lastName, team);

            // Assert
            Assert.Equal(id, user.Id);
            Assert.Equal(email, user.Email);
            Assert.Equal(firstName, user.FirstName);
            Assert.Equal(lastName, user.LastName);
            Assert.Equal(team, user.Team);
            Assert.True(user.CreatedAt <= DateTime.UtcNow);
            Assert.True(user.UpdatedAt <= DateTime.UtcNow);
            Assert.Null(user.ActiveDirectoryUserId);
            Assert.Null(user.EntraUserObjectId);
            Assert.Null(user.ActiveDirectoryUserGroupIds);
            Assert.Null(user.ManageUserAccounts);
            Assert.False(user.ManageConversionUrns);
            Assert.False(user.ManageLocalAuthorities);
            Assert.False(user.ManageTeam);
            Assert.Null(user.AssignToProject);
            Assert.False(user.AddNewProject);
            Assert.Null(user.DeactivatedAt);
            Assert.Null(user.LatestSession);
        }

        [Theory]
        [CustomAutoData(typeof(UserCustomization), typeof(IgnoreVirtualMembersCustomisation))]
        public void Create_WithNullTeam_ShouldCreateUserWithNullTeam(
            UserId id,
            string email,
            string firstName,
            string lastName)
        {
            // Act
            var user = User.Create(id, email, firstName, lastName, null);

            // Assert
            Assert.Equal(id, user.Id);
            Assert.Equal(email, user.Email);
            Assert.Equal(firstName, user.FirstName);
            Assert.Equal(lastName, user.LastName);
            Assert.Null(user.Team);
        }

        [Theory]
        [CustomAutoData(typeof(UserCustomization), typeof(IgnoreVirtualMembersCustomisation))]
        public void Create_ShouldSetCreatedAtAndUpdatedAtToCurrentUtcTime(
            UserId id,
            string email,
            string firstName,
            string lastName,
            string team)
        {
            // Arrange
            var beforeCreate = DateTime.UtcNow;

            // Act
            var user = User.Create(id, email, firstName, lastName, team);

            // Assert
            var afterCreate = DateTime.UtcNow;
            Assert.True(user.CreatedAt >= beforeCreate);
            Assert.True(user.CreatedAt <= afterCreate);
            Assert.True(user.UpdatedAt >= beforeCreate);
            Assert.True(user.UpdatedAt <= afterCreate);
        }

        [Theory]
        [InlineData("John", "Doe", "John Doe")]
        [InlineData("Jane", "Smith", "Jane Smith")]
        [InlineData("", "Doe", " Doe")]
        [InlineData("John", "", "John ")]
        [InlineData("", "", " ")]
        public void FullName_ShouldReturnFirstNameAndLastNameConcatenated(string firstName, string lastName, string expected)
        {
            // Arrange
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName
            };

            // Act
            var result = user.FullName;

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [CustomAutoData(typeof(UserCustomization), typeof(IgnoreVirtualMembersCustomisation))]
        public void Notes_ShouldInitializeAsEmptyList(User user)
        {
            // Assert
            Assert.NotNull(user.Notes);
            Assert.Empty(user.Notes);
            Assert.IsAssignableFrom<ICollection<Note>>(user.Notes);
        }

        [Theory]
        [CustomAutoData(typeof(UserCustomization), typeof(IgnoreVirtualMembersCustomisation))]
        public void ProjectAssignedTos_ShouldInitializeAsEmptyList(User user)
        {
            // Assert
            Assert.NotNull(user.ProjectAssignedTos);
            Assert.Empty(user.ProjectAssignedTos);
            Assert.IsAssignableFrom<ICollection<Project>>(user.ProjectAssignedTos);
        }

        [Theory]
        [CustomAutoData(typeof(UserCustomization), typeof(IgnoreVirtualMembersCustomisation))]
        public void ProjectCaseworkers_ShouldInitializeAsEmptyList(User user)
        {
            // Assert
            Assert.NotNull(user.ProjectCaseworkers);
            Assert.Empty(user.ProjectCaseworkers);
            Assert.IsAssignableFrom<ICollection<Project>>(user.ProjectCaseworkers);
        }

        [Theory]
        [CustomAutoData(typeof(UserCustomization), typeof(IgnoreVirtualMembersCustomisation))]
        public void ProjectRegionalDeliveryOfficers_ShouldInitializeAsEmptyList(User user)
        {
            // Assert
            Assert.NotNull(user.ProjectRegionalDeliveryOfficers);
            Assert.Empty(user.ProjectRegionalDeliveryOfficers);
            Assert.IsAssignableFrom<ICollection<Project>>(user.ProjectRegionalDeliveryOfficers);
        }

        [Theory]
        [CustomAutoData(typeof(UserCustomization), typeof(IgnoreVirtualMembersCustomisation))]
        public void Notes_ShouldAllowAddingAndRemoving(User user)
        {
            // Arrange
            var note = new Note
            {
                Id = new NoteId(Guid.NewGuid()),
                Body = "Test note",
                UserId = user.Id,
                ProjectId = new ProjectId(Guid.NewGuid()),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            user.Notes.Add(note);

            // Assert
            Assert.Single(user.Notes);
            Assert.Contains(note, user.Notes);

            // Act - Remove
            user.Notes.Remove(note);

            // Assert
            Assert.Empty(user.Notes);
        }
    }
}
