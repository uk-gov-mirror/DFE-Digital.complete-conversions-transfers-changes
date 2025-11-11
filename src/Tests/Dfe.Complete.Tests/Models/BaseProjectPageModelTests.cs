using AutoFixture.Xunit2;
using Dfe.Complete.Application.Common.Models;
using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Application.Projects.Queries.GetProject;
using Dfe.Complete.Models;
using Dfe.Complete.Tests.Common.Assertions;
using Dfe.Complete.Tests.Common.Customizations.Behaviours;
using Dfe.Complete.Utils.Exceptions;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Dfe.Complete.Tests.Models;

public class BaseProjectPageModelTests
{
    [Theory]
    [CustomAutoData(typeof(IgnoreVirtualMembersCustomisation))]
    public async Task UpdateCurrentProject_LogsWarning_WhenProjectIdIsInvalid(
        [Frozen] ISender sender,
        [Frozen] ILogger logger)
    {
        // Arrange
        var model = new TestBaseProjectPageModel(sender, logger)
        {
            ProjectId = "not-a-guid"
        };

        // Act
        await model.UpdateCurrentProject();

        // Assert
        logger.ShouldHaveLogged("not-a-guid is not a valid Guid.", LogLevel.Warning);
        Assert.Null(model.Project);
    }

    [Theory]
    [CustomAutoData(typeof(IgnoreVirtualMembersCustomisation))]
    public async Task UpdateCurrentProject_LogsWarning_WhenResultIsFailure(
        [Frozen] ISender sender,
        [Frozen] ILogger logger,
        Guid guid)
    {
        // Arrange
        var model = new TestBaseProjectPageModel(sender, logger)
        {
            ProjectId = guid.ToString()
        };

        var failedResult = Result<ProjectDto?>.Failure("Project not found");

        sender.Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(failedResult);

        // Act
        await model.UpdateCurrentProject();

        // Assert
        logger.ShouldHaveLogged($"Project {guid} does not exist.", LogLevel.Warning);
        Assert.Null(model.Project);
    }

    [Theory]
    [CustomAutoData(typeof(IgnoreVirtualMembersCustomisation))]
    public async Task UpdateCurrentProject_LogsWarning_WhenResultValueIsNull(
        [Frozen] ISender sender,
        [Frozen] ILogger logger,
        Guid guid)
    {
        // Arrange
        var model = new TestBaseProjectPageModel(sender, logger)
        {
            ProjectId = guid.ToString()
        };

        var nullResult = Result<ProjectDto?>.Success(null!); // simulate null value

        sender.Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(nullResult);

        // Act
        await model.UpdateCurrentProject();

        // Assert
        logger.ShouldHaveLogged($"Project {guid} does not exist.", LogLevel.Warning);
        Assert.Null(model.Project);
    }

    [Theory]
    [CustomAutoData(typeof(IgnoreVirtualMembersCustomisation), typeof(DateOnlyCustomization))]
    public async Task UpdateCurrentProject_SetsProject_WhenResultIsSuccessful(
        [Frozen] ISender sender,
        [Frozen] ILogger logger,
        Guid guid,
        ProjectDto projectDto)
    {
        // Arrange
        var model = new TestBaseProjectPageModel(sender, logger)
        {
            ProjectId = guid.ToString()
        };

        var successResult = Result<ProjectDto?>.Success(projectDto);

        sender.Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        // Act
        await model.UpdateCurrentProject();

        // Assert
        Assert.Equal(projectDto, model.Project);
    }

    [Fact]
    public async Task GetKeyContactForProjectsAsync_ReturnsAKeyContacts_WhenFound()
    {
        // Arrange
        var sender = Substitute.For<ISender>();
        var logger = Substitute.For<ILogger>();
        var model = new TestBaseProjectPageModel(sender, logger)
        {
            ProjectId = Guid.NewGuid().ToString(),
        };

        var expectedKeyContacts = new Application.KeyContacts.Models.KeyContactDto();
        var result = Result<Application.KeyContacts.Models.KeyContactDto?>.Success(expectedKeyContacts);

        sender.Send(Arg.Any<Application.KeyContacts.Queries.GetKeyContactsForProjectQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        // Act
        await model.GetKeyContactForProjectsAsync();

        // Assert
        Assert.Equal(expectedKeyContacts, model.KeyContacts);
    }

    [Fact]
    public async Task GetKeyContactForProjectsAsync_ThrowsNotFoundException_WhenNotFound()
    {
        // Arrange
        var sender = Substitute.For<ISender>();
        var logger = Substitute.For<ILogger>();
        var model = new TestBaseProjectPageModel(sender, logger)
        {
            ProjectId = Guid.NewGuid().ToString(),
        };

        var result = Result<Application.KeyContacts.Models.KeyContactDto?>.Success(null);

        sender.Send(Arg.Any<Application.KeyContacts.Queries.GetKeyContactsForProjectQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => model.GetKeyContactForProjectsAsync());
        Assert.Contains($"Key contacts for project {model.ProjectId} doesn't exist", exception.Message);
    }
}

public class TestBaseProjectPageModel(ISender sender, ILogger logger) : BaseProjectPageModel(sender, logger)
{
    public new async Task GetKeyContactForProjectsAsync() => await base.GetKeyContactForProjectsAsync();
}