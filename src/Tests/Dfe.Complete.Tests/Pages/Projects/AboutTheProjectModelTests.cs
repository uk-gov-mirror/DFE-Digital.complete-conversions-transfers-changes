using AutoFixture.Xunit2;
using Dfe.AcademiesApi.Client.Contracts;
using Dfe.Complete.Application.Common.Models;
using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Application.Projects.Queries.GetGiasEstablishment;
using Dfe.Complete.Application.Projects.Queries.GetProject;
using Dfe.Complete.Application.Projects.Queries.GetTransferTasksData;
using Dfe.Complete.Application.Services.AcademiesApi;
using Dfe.Complete.Application.Users.Queries.GetUser;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Pages.Projects.AboutTheProject;
using Dfe.Complete.Utils.Exceptions;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Dfe.Complete.Tests.Pages.Projects
{
    public class AboutTheProjectModelTests
    {
        private static UserDto GetUser()
        {
            return new UserDto { EntraUserObjectId = "test-ad-id", FirstName = "Test", LastName = "User", Team = "Support team" };
        }

        private static PageContext GetPageContext()
        {
            var expectedUser = GetUser();
            var claims = new List<Claim> { new Claim("objectidentifier", expectedUser?.EntraUserObjectId!) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            var httpContext = new DefaultHttpContext()
            {
                User = principal
            };

            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);

            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);

            return new PageContext(actionContext)
            {
                ViewData = viewData
            };
        }

        [Theory]
        [CustomAutoData(typeof(DateOnlyCustomization))]
        public async Task OnGet_When_AcademyUrn_IsNotSupplied_ThrowsException([Frozen] Mock<ISender> mockSender, [Frozen] ILogger<AboutTheProjectModel> _logger)
        {
            var projectIdGuid = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var model = new AboutTheProjectModel(mockSender.Object, _logger)
            {
                PageContext = GetPageContext(),
                ProjectId = projectIdGuid.ToString()
            };

            var project = new ProjectDto
            {
                Id = new ProjectId(projectIdGuid),
                Urn = new Urn(133274),
                AcademyUrn = new Urn(123456),
                CreatedAt = now,
                UpdatedAt = now
            };

            var getProjectByIdQuery = new GetProjectByIdQuery(project.Id);

            mockSender.Setup(s => s.Send(getProjectByIdQuery, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDto?>.Success(project));

            var userDto = GetUser();
            var userResult = Result<UserDto?>.Success(userDto);

            mockSender
                .Setup(s => s.Send(It.Is<GetUserByOidQuery>(q => q.ObjectId == userDto.EntraUserObjectId), default))
                .ReturnsAsync(userResult);

            var establishment = new EstablishmentDto
            {
                Ukprn = "10060668",
                Urn = project.Urn.Value.ToString(),
                Name = "Park View Primary School",
            };

            mockSender.Setup(s => s.Send(new GetEstablishmentByUrnRequest(project.Urn.Value.ToString()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<EstablishmentDto?>.Success(establishment));

            mockSender.Setup(s => s.Send(new GetGiasEstablishmentByUrnQuery(project.AcademyUrn), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GiasEstablishmentDto?>.Failure("Database error"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => model.OnGetAsync());

            Assert.Equal($"Academy {project.AcademyUrn.Value} does not exist.", ex.Message);
        }

        [Theory]
        [CustomAutoData(typeof(DateOnlyCustomization))]
        public async Task OnGet_Loads_Correctly([Frozen] Mock<ISender> mockSender, [Frozen] ILogger<AboutTheProjectModel> _logger)
        {
            var projectIdGuid = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var model = new AboutTheProjectModel(mockSender.Object, _logger)
            {
                PageContext = GetPageContext(),
                ProjectId = projectIdGuid.ToString()
            };

            var project = new ProjectDto
            {
                Id = new ProjectId(projectIdGuid),
                Type = ProjectType.Transfer,
                Urn = new Urn(133274),
                AcademyUrn = new Urn(123456),
                IncomingTrustUkprn = "10058828",
                OutgoingTrustUkprn = "10066101",
                GroupId = new ProjectGroupId(Guid.NewGuid()),
                TasksDataId = new TaskDataId(Guid.NewGuid()),
                CreatedAt = now,
                UpdatedAt = now
            };

            var getProjectByIdQuery = new GetProjectByIdQuery(project.Id);

            mockSender.Setup(s => s.Send(getProjectByIdQuery, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDto?>.Success(project));

            var establishment = new EstablishmentDto
            {
                Ukprn = "10060668",
                Urn = project.Urn.Value.ToString(),
                Name = "Park View Primary School",
            };
            mockSender.Setup(s => s.Send(new GetEstablishmentByUrnRequest(project.Urn.Value.ToString()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<EstablishmentDto?>.Success(establishment));

            var academy = new GiasEstablishmentDto
            {
                Ukprn = "10055198",
                Urn = project.AcademyUrn,
                Name = "Elmstead Primary School",
            };
            mockSender.Setup(s => s.Send(new GetGiasEstablishmentByUrnQuery(project.AcademyUrn!), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GiasEstablishmentDto?>.Success(academy));

            var incomingTrust = new TrustDto
            {
                Name = "Test Incoming Trust",
                Ukprn = project.IncomingTrustUkprn.Value.ToString()
            };
            mockSender.Setup(s => s.Send(new GetTrustByUkprnRequest(project.IncomingTrustUkprn.Value.ToString()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TrustDto?>.Success(incomingTrust));

            var outgoingTrust = new TrustDto
            {
                Name = "Test Outgoing Trust",
                Ukprn = project.OutgoingTrustUkprn.Value.ToString()
            };
            mockSender.Setup(s => s.Send(new GetTrustByUkprnRequest(project.OutgoingTrustUkprn.Value.ToString()), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Result<TrustDto?>.Success(outgoingTrust));

            var userDto = GetUser();
            var userResult = Result<UserDto?>.Success(userDto);

            mockSender
                .Setup(s => s.Send(It.Is<GetUserByOidQuery>(q => q.ObjectId == userDto.EntraUserObjectId), default))
                .ReturnsAsync(userResult);

            var group = new ProjectGroupDto
            {
                Id = project.GroupId,
                GroupIdentifier = "GRP_12345678"
            };
            mockSender.Setup(s => s.Send(new GetProjectGroupByIdQuery(project.GroupId), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Result<ProjectGroupDto>.Success(group));

            var transferTask = new TransferTaskDataDto
            {
                Id = project.TasksDataId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            mockSender.Setup(s => s.Send(new GetTransferTasksDataByIdQuery(project.TasksDataId), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Result<TransferTaskDataDto>.Success(transferTask));

            //Act  
            await model.OnGetAsync();

            // Assert  
            Assert.NotNull(model.Project);
            Assert.Equal(project.Id, model.Project.Id);

            Assert.NotNull(model.Establishment);
            Assert.Equal(establishment.Ukprn, model.Establishment.Ukprn);
            Assert.Equal(establishment.Urn, model.Establishment.Urn);

            Assert.NotNull(model.Academy);
            Assert.Equal(academy.Ukprn, model.Academy.Ukprn);
            Assert.Equal(academy.Urn, model.Academy.Urn);

            Assert.NotNull(model.IncomingTrust);
            Assert.Equal(incomingTrust.Ukprn, model.IncomingTrust.Ukprn);

            Assert.NotNull(model.OutgoingTrust);
            Assert.Equal(outgoingTrust.Ukprn, model.OutgoingTrust.Ukprn);

            Assert.NotNull(model.ProjectGroup);
            Assert.Equal(group.Id, model.ProjectGroup.Id);

            Assert.NotNull(model.TransferTaskData);
            Assert.Equal(transferTask.Id, model.TransferTaskData.Id);
        }
    }
}
