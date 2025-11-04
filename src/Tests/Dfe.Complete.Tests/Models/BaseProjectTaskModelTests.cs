using Dfe.AcademiesApi.Client.Contracts;
using Dfe.Complete.Application.Common.Models;
using Dfe.Complete.Application.Notes.Queries;
using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Application.Projects.Queries.GetProject;
using Dfe.Complete.Application.Services.AcademiesApi;
using Dfe.Complete.Application.Users.Queries.GetUser;
using Dfe.Complete.Domain.Constants;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Dfe.Complete.Pages.Projects.TaskList.Tasks.Tests;

public class BaseProjectTaskModelTests
{
    private readonly Mock<ISender> _mockSender;
    private readonly Mock<IAuthorizationService> _mockAuthService = new();
    private readonly Mock<ILogger<BaseProjectTaskModel>> _mockLogger = new();
    private readonly BaseProjectTaskModel _model;
    private readonly string ValidProjectId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    public BaseProjectTaskModelTests()
    {
        _mockSender = new Mock<ISender>();
        _model = new BaseProjectTaskModel(
            _mockSender.Object,
            _mockAuthService.Object,
            _mockLogger.Object,
            NoteTaskIdentifier.Handover
        )
        {
            ProjectId = ValidProjectId,
            TaskIdentifier = NoteTaskIdentifier.Handover
        };

        var mockProject = new ProjectDto
        {
            Id = new ProjectId(Guid.NewGuid()),
            State = ProjectState.Active,
            Urn = new Urn(123456),
            OutgoingTrustUkprn = "123456789",
            Type = ProjectType.Conversion,
            NewTrustReferenceNumber = "TR123456",
            NewTrustName = "New Trust Name"
        };

        var mockEstablishment = new EstablishmentDto { };

        _mockSender
            .Setup(s => s.Send(It.IsAny<GetProjectByIdQuery>(), default))
            .ReturnsAsync(Result<ProjectDto?>.Success(mockProject));

        _mockSender
            .Setup(s => s.Send(It.IsAny<GetEstablishmentByUrnRequest>(), default))
            .ReturnsAsync(Result<EstablishmentDto>.Success(mockEstablishment));

        var mockUser = new Mock<ClaimsPrincipal>();
        mockUser
            .Setup(u => u.Claims)
            .Returns([new Claim("objectidentifier", "MockObjectIdentifier")]);
        var mockPageContext = new PageContext { HttpContext = new DefaultHttpContext { User = mockUser.Object } };

        _mockSender
            .Setup(s => s.Send(It.IsAny<GetUserByOidQuery>(), default))
            .ReturnsAsync(Result<UserDto?>.Success(new UserDto
            {
                ActiveDirectoryUserId = "MockObjectIdentifier",
                Team = ProjectTeam.BusinessSupport.ToString()
            }));

        _model.PageContext = mockPageContext;
    }

    [Fact]
    public async Task OnGetAsync_ReturnsPageAndLoadsNotes_WhenBaseReturnsPageResult()
    {
        // Arrange
        var noteList = new List<NoteDto> {
            new(
                new NoteId(Guid.NewGuid()),
                "Test Note",
                new ProjectId(Guid.NewGuid()),
                new UserId(Guid.NewGuid()),
                "Test User",
                DateTime.UtcNow
            )
        };

        _mockSender
            .Setup(s => s.Send(It.IsAny<GetTaskNotesByProjectIdQuery>(), default))
            .ReturnsAsync(Result<List<NoteDto>>.Success(noteList));

        // Act
        var result = await _model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(noteList, _model.Notes);
    }

    [Fact]
    public async Task OnGetAsync_ThrowsException_WhenNoteFetchFails()
    {
        // Arrange
        _mockSender
            .Setup(s => s.Send(It.IsAny<GetTaskNotesByProjectIdQuery>(), default))
            .ReturnsAsync(Result<List<NoteDto>>.Failure("Error fetching notes"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(_model.OnGetAsync);
    }

    [Theory]
    [InlineData(ProjectState.Deleted, false)]
    [InlineData(ProjectState.Completed, false)]
    [InlineData(ProjectState.DaoRevoked, false)]
    [InlineData(ProjectState.Active, true)]
    [InlineData(ProjectState.Inactive, true)]
    public void CanAddNotes_ReturnsExpectedResult(ProjectState projectState, bool expected)
    {
        // Arrange
        var model = new BaseProjectTaskModel(_mockSender.Object, _mockAuthService.Object, _mockLogger.Object, NoteTaskIdentifier.Handover)
        {
            Project = new ProjectDto { State = projectState },
            TaskIdentifier = NoteTaskIdentifier.Handover
        };

        // Act
        var result = model.CanAddNotes;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ProjectState.Completed, "00000000-0000-0000-0000-000000000001", false)]
    [InlineData(ProjectState.Active, "00000000-0000-0000-0000-000000000001", true)]
    [InlineData(ProjectState.Active, "00000000-0000-0000-0000-000000000002", false)]
    public void CanEditNote_ReturnsExpectedResult(ProjectState projectState, string currentUserGuidString, bool expected)
    {
        // Arrange
        var assignedUserGuid = new Guid("00000000-0000-0000-0000-000000000001");
        var assignedUserId = new UserId(assignedUserGuid);

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("objectidentifier", currentUserGuidString),
            new Claim(CustomClaimTypeConstants.UserId, currentUserGuidString)
        ]));

        var model = new BaseProjectTaskModel(_mockSender.Object, _mockAuthService.Object, _mockLogger.Object, NoteTaskIdentifier.Handover)
        {
            Project = new ProjectDto { State = projectState },
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            },
            TaskIdentifier = NoteTaskIdentifier.Handover
        };

        // Act
        var result = model.CanEditNote(assignedUserId);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task OnPostAddNoteAsync_ReturnsEarly_IfBaseReturnsNonPage()
    {
        var model = new BaseProjectTaskModel(
            _mockSender.Object,
            _mockAuthService.Object,
            _mockLogger.Object,
            NoteTaskIdentifier.Handover
        )
        {
            TaskIdentifier = NoteTaskIdentifier.Handover
        };

        var result = await model.OnPostAddNoteAsync();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAddNoteAsync_RedirectsToAddNote_WhenAuthorisedAndCanAdd()
    {
        _mockAuthService.Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                null,
                UserPolicyConstants.CanAddNotes))
            .ReturnsAsync(AuthorizationResult.Success());

        var result = await _model.OnPostAddNoteAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal($"/projects/{ValidProjectId}/notes/new?task_identifier=handover", redirect.Url);
    }
}
