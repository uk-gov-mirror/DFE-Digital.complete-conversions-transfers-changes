namespace Dfe.Complete.Tests.Pages.Projects.Completion;

using AutoFixture;
using AutoFixture.Xunit2;
using Dfe.Complete.Application.Common.Models;
using Dfe.Complete.Application.KeyContacts.Models;
using Dfe.Complete.Application.KeyContacts.Queries;
using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Application.Projects.Queries.GetConversionTasksData;
using Dfe.Complete.Application.Projects.Queries.GetProject;
using Dfe.Complete.Application.Projects.Queries.GetTransferTasksData;
using Dfe.Complete.Application.Users.Queries.GetUser;
using Dfe.Complete.Domain.Constants;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Models;
using Dfe.Complete.Pages.Projects.Completion;
using Dfe.Complete.Services.Project;
using Dfe.Complete.Tests.Common.Customizations.Behaviours;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using NSubstitute;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

public class CompleteProjectModelTests
{
    [Theory]
    [CustomAutoData(typeof(DateOnlyCustomization),
         typeof(IgnoreVirtualMembersCustomisation))]
    public async Task OnPost_ValidTransferProjectCompletionModel_RetursRedirectResult_ToConfirmation
    (
        [Frozen] IProjectService projectService,
        [Frozen] ISender sender,
        [Frozen] ILogger<CompleteProjectModel> logger,
        IFixture fixture
    )
    {
        // Arrange
        var userId = "00000000-0000-0000-0000-000000001234";

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
           [
               new Claim("objectidentifier", userId),
               new Claim(CustomClaimTypeConstants.UserId, userId)
           ]));

        var projectId = new ProjectId(Guid.NewGuid());

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        CompleteProjectModel testClass = new CompleteProjectModel(sender, projectService, logger)
        {
            ProjectId = projectId.Value.ToString(),
            CurrentUserTeam = Domain.Enums.ProjectTeam.ServiceSupport,
            PageContext = new PageContext
            {
                HttpContext = httpContext
            },
            TempData = tempData
        };

        var projectDto = fixture.Build<ProjectDto>()
            .With(p => p.Id, projectId)
            .With(p => p.Type, Domain.Enums.ProjectType.Transfer)
            .With(p => p.SignificantDate, DateOnly.FromDateTime(DateTime.Today))
            .With(p => p.TasksDataId, new TaskDataId(Guid.NewGuid()))
            .With(p => p.SignificantDateProvisional, false)
            .Create();

        UserDto? userDto = new UserDto { Team = "service_support" };
        var userResult = Result<UserDto?>.Success(userDto);

        var successResult = Result<ProjectDto?>.Success(projectDto);

        sender.Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        sender.Send(Arg.Any<GetUserByOidQuery>(), Arg.Any<CancellationToken>())
            .Returns(userResult);

        sender.Send(Arg.Any<GetTransferTasksDataByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TransferTaskDataDto>.Success(fixture.Create<TransferTaskDataDto>()));

        sender.Send(Arg.Any<GetKeyContactsForProjectQuery>(), Arg.Any<CancellationToken>())
           .Returns(Result<KeyContactDto>.Success(fixture.Create<KeyContactDto>()));

        projectService.GetTransferProjectCompletionValidationResult(Arg.Any<DateOnly?>(), Arg.Any<bool>(), Arg.Any<TransferTaskListViewModel>())
            .Returns(new List<string>());

        // Act
        var result = await testClass.OnPostAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains($"/projects/{projectDto.Id.Value}/complete", redirectResult.Url);
    }

    [Theory]
    [CustomAutoData(typeof(DateOnlyCustomization),
         typeof(IgnoreVirtualMembersCustomisation))]
    public async Task OnPost_ValidConversionProjectCompletionModel_RetursRedirectResult_ToConfirmation
    (
        [Frozen] IProjectService projectService,
        [Frozen] ISender sender,
        [Frozen] ILogger<CompleteProjectModel> logger,
        IFixture fixture
    )
    {
        // Arrange
        var projectId = new ProjectId(Guid.NewGuid());
        var userId = "00000000-0000-0000-0000-000000001234";

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
           [
               new Claim("objectidentifier", userId),
               new Claim(CustomClaimTypeConstants.UserId, userId)
           ]));

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        CompleteProjectModel testClass = new CompleteProjectModel(sender, projectService, logger)
        {
            ProjectId = projectId.Value.ToString(),
            CurrentUserTeam = Domain.Enums.ProjectTeam.ServiceSupport,
            PageContext = new PageContext
            {
                HttpContext = httpContext
            },
            TempData = tempData
        };

        var projectDto = fixture.Build<ProjectDto>()
            .With(p => p.Id, projectId)
            .With(p => p.Type, Domain.Enums.ProjectType.Conversion)
            .With(p => p.SignificantDate, DateOnly.FromDateTime(DateTime.Today))
            .With(p => p.SignificantDateProvisional, false)
            .With(p => p.TasksDataId, new TaskDataId(Guid.NewGuid()))
            .Create();

        UserDto? userDto = new UserDto { Team = "service_support" };
        var userResult = Result<UserDto?>.Success(userDto);

        var successResult = Result<ProjectDto?>.Success(projectDto);

        sender.Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        sender.Send(Arg.Any<GetUserByOidQuery>(), Arg.Any<CancellationToken>())
          .Returns(userResult);

        sender.Send(Arg.Any<GetConversionTasksDataByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ConversionTaskDataDto>.Success(fixture.Create<ConversionTaskDataDto>()));

        sender.Send(Arg.Any<GetKeyContactsForProjectQuery>(), Arg.Any<CancellationToken>())
           .Returns(Result<KeyContactDto>.Success(fixture.Create<KeyContactDto>()));

        projectService.GetConversionProjectCompletionValidationResult(Arg.Any<DateOnly?>(), Arg.Any<bool>(), Arg.Any<ConversionTaskListViewModel>())
            .Returns(new List<string>());

        // Act
        var result = await testClass.OnPostAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains($"/projects/{projectDto.Id.Value}/complete", redirectResult.Url);
    }

    [Theory]
    [CustomAutoData(typeof(DateOnlyCustomization),
         typeof(IgnoreVirtualMembersCustomisation))]
    public async Task OnPost_InValidTransferProjectCompletionModel_RetursRedirectResult_ToTaskListPage
    (
        [Frozen] IProjectService projectService,
        [Frozen] ISender sender,
        [Frozen] ILogger<CompleteProjectModel> logger,
        IFixture fixture
    )
    {
        // Arrange
        var userId = "00000000-0000-0000-0000-000000001234";

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
           [
               new Claim("objectidentifier", userId),
               new Claim(CustomClaimTypeConstants.UserId, userId)
           ]));

        var projectId = new ProjectId(Guid.NewGuid());

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        CompleteProjectModel testClass = new CompleteProjectModel(sender, projectService, logger)
        {
            ProjectId = projectId.Value.ToString(),
            TempData = tempData,
            PageContext = new PageContext
            {
                HttpContext = httpContext
            },
        };

        var projectDto = fixture.Build<ProjectDto>()
            .With(p => p.Id, projectId)
            .With(p => p.Type, Domain.Enums.ProjectType.Transfer)
            .With(p => p.SignificantDate, DateOnly.FromDateTime(DateTime.Today))
            .With(p => p.SignificantDateProvisional, false)
            .With(p => p.TasksDataId, new TaskDataId(Guid.NewGuid()))
            .Create();

        var successResult = Result<ProjectDto?>.Success(projectDto);

        UserDto? userDto = new UserDto { Team = "service_support" };
        var userResult = Result<UserDto?>.Success(userDto);

        sender.Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        sender.Send(Arg.Any<GetUserByOidQuery>(), Arg.Any<CancellationToken>())
          .Returns(userResult);

        sender.Send(Arg.Any<GetTransferTasksDataByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TransferTaskDataDto>.Success(fixture.Create<TransferTaskDataDto>()));

        sender.Send(Arg.Any<GetKeyContactsForProjectQuery>(), Arg.Any<CancellationToken>())
           .Returns(Result<KeyContactDto>.Success(fixture.Create<KeyContactDto>()));

        projectService.GetTransferProjectCompletionValidationResult(Arg.Any<DateOnly?>(), Arg.Any<bool>(), Arg.Any<TransferTaskListViewModel>())
            .Returns(new List<string> { "validation message" });

        // Act
        var result = await testClass.OnPostAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Multiple(
            () => Assert.Equal($"/projects/{projectDto.Id.Value}/tasks?projectCompletionValidation=true", redirectResult.Url),
            () => Assert.True(testClass.TempData.ContainsKey("CompleteProjectValidationMessages"))
        );
    }

    [Theory]
    [CustomAutoData(typeof(DateOnlyCustomization),
         typeof(IgnoreVirtualMembersCustomisation))]
    public async Task OnPost_InValidConversionProjectCompletionModel_RetursRedirectResult_ToTaskListPage
    (
        [Frozen] IProjectService projectService,
        [Frozen] ISender sender,
        [Frozen] ILogger<CompleteProjectModel> logger,
        IFixture fixture
    )
    {
        // Arrange
        var userId = "00000000-0000-0000-0000-000000001234";

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
           [
               new Claim("objectidentifier", userId),
               new Claim(CustomClaimTypeConstants.UserId, userId)
           ]));

        var projectId = new ProjectId(Guid.NewGuid());

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        CompleteProjectModel testClass = new CompleteProjectModel(sender, projectService, logger)
        {
            ProjectId = projectId.Value.ToString(),
            TempData = tempData,
            PageContext = new PageContext
            {
                HttpContext = httpContext
            },
        };

        var projectDto = fixture.Build<ProjectDto>()
            .With(p => p.Id, projectId)
            .With(p => p.Type, Domain.Enums.ProjectType.Conversion)
            .With(p => p.SignificantDate, DateOnly.FromDateTime(DateTime.Today))
            .With(p => p.SignificantDateProvisional, false)
            .With(p => p.TasksDataId, new TaskDataId(Guid.NewGuid()))
            .Create();

        UserDto? userDto = new UserDto { Team = "service_support" };
        var userResult = Result<UserDto?>.Success(userDto);

        var successResult = Result<ProjectDto?>.Success(projectDto);

        sender.Send(Arg.Any<GetProjectByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        sender.Send(Arg.Any<GetUserByOidQuery>(), Arg.Any<CancellationToken>())
         .Returns(userResult);

        sender.Send(Arg.Any<GetConversionTasksDataByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ConversionTaskDataDto>.Success(fixture.Create<ConversionTaskDataDto>()));

        sender.Send(Arg.Any<GetKeyContactsForProjectQuery>(), Arg.Any<CancellationToken>())
           .Returns(Result<KeyContactDto>.Success(fixture.Create<KeyContactDto>()));

        projectService.GetConversionProjectCompletionValidationResult(Arg.Any<DateOnly?>(), Arg.Any<bool>(), Arg.Any<ConversionTaskListViewModel>())
            .Returns(new List<string> { "validation message" });

        // Act
        var result = await testClass.OnPostAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);

        Assert.Multiple(
            () => Assert.Equal($"/projects/{projectDto.Id.Value}/tasks?projectCompletionValidation=true", redirectResult.Url),
            () => Assert.True(testClass.TempData.ContainsKey("CompleteProjectValidationMessages"))
        );
    }
}