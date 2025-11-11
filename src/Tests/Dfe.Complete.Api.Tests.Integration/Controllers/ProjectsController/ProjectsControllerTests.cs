using AutoFixture;
using AutoFixture.Xunit2;
using Dfe.AcademiesApi.Client.Contracts;
using Dfe.Complete.Api.Tests.Integration.Customizations;
using Dfe.Complete.Client.Contracts;
using Dfe.Complete.Domain.Constants;
using Dfe.Complete.Domain.Entities;
using Dfe.Complete.Infrastructure.Database;
using Dfe.Complete.Tests.Common.Constants;
using Dfe.Complete.Tests.Common.Customizations.Behaviours;
using Dfe.Complete.Tests.Common.Customizations.Models;
using Dfe.Complete.Utils;
using Dfe.Complete.Utils.Exceptions;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations;
using GovUK.Dfe.CoreLibs.Testing.Mocks.WebApplicationFactory;
using GovUK.Dfe.CoreLibs.Testing.Mocks.WireMock;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GiasEstablishment = Dfe.Complete.Domain.Entities.GiasEstablishment;
using LocalAuthority = Dfe.Complete.Domain.Entities.LocalAuthority;
using Project = Dfe.Complete.Domain.Entities.Project;
using ProjectId = Dfe.Complete.Client.Contracts.ProjectId;
using ProjectState = Dfe.Complete.Domain.Enums.ProjectState;
using ProjectType = Dfe.Complete.Domain.Enums.ProjectType;
using Region = Dfe.Complete.Domain.Enums.Region;
using Ukprn = Dfe.Complete.Domain.ValueObjects.Ukprn;
using UserId = Dfe.Complete.Client.Contracts.UserId;

namespace Dfe.Complete.Api.Tests.Integration.Controllers.ProjectsController;

public partial class ProjectsControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization),
        typeof(LocalAuthorityCustomization),
        typeof(GiasEstablishmentsCustomization))]
    public async Task CountAllProjects_Async_ShouldReturnCorrectNumber(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();

        var giasEstablishment = fixture.Create<GiasEstablishment>();
        await dbContext.GiasEstablishments.AddAsync(giasEstablishment);

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        Assert.NotNull(giasEstablishment.Urn);

        var projects = fixture.Customize(new ProjectCustomization
        {
            RegionalDeliveryOfficerId = testUser.Id,
            CaseworkerId = testUser.Id,
            AssignedToId = testUser.Id,
            LocalAuthorityId = localAuthority.Id,
            Urn = giasEstablishment.Urn
        })
            .CreateMany<Project>(50).ToList();


        projects.ForEach(x => x.LocalAuthorityId = localAuthority.Id);

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        var result = await projectsClient.CountAllProjectsAsync(null, null, null, null);

        Assert.Equal(50, result);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjects_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(50)
            .ToList();

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var projects = establishments.Select(establishment =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                RegionalDeliveryOfficerId = testUser.Id,
                CaseworkerId = testUser.Id,
                AssignedToId = testUser.Id
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        projects.ForEach(x => x.LocalAuthorityId = localAuthority.Id);

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsAsync(
            null, null, null, null, null, 0, 50);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(50, results.Count);
        foreach (var result in results)
        {
            var project = projects.Find(p => p.Id.Value == result.ProjectId?.Value);
            var establishment = establishments.Find(e => e.Urn?.Value == result.Urn?.Value);

            Assert.NotNull(result.Urn);
            Assert.Equal(project?.Urn.Value, result.Urn.Value);
            Assert.Equal(establishment?.Urn?.Value, result.Urn.Value);

            Assert.NotNull(result.EstablishmentName);
            Assert.Equal(establishment?.Name, result.EstablishmentName);

            Assert.NotNull(result.ProjectId);
            Assert.Equal(project?.Id.Value, result.ProjectId.Value);

            Assert.NotNull(result.ConversionOrTransferDate);
            Assert.Equal(project?.SignificantDate, new DateOnly(result.ConversionOrTransferDate.Value.Year,
                result.ConversionOrTransferDate.Value.Month, result.ConversionOrTransferDate.Value.Day));

            Assert.NotNull(result.State);
            Assert.Equal(project?.State.ToString(), result.State.ToString());

            Assert.NotNull(result.ProjectType);
            Assert.Equal(project?.Type?.ToString(), result.ProjectType.Value.ToString());

            Assert.Equal(project?.FormAMat, result.IsFormAMAT);

            Assert.NotNull(result.AssignedToFullName);
            Assert.Equal($"{project?.AssignedTo?.FirstName} {project?.AssignedTo?.LastName}",
                result.AssignedToFullName);
        }
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsCompletedState_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();
        var establishments = fixture.CreateMany<GiasEstablishment>(50).ToList();
        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var projects = establishments.Select(establishment =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                RegionalDeliveryOfficerId = testUser.Id,
                CaseworkerId = testUser.Id,
                AssignedToId = testUser.Id
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        var laId = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid())?.Id;
        Assert.NotNull(laId);

        projects.ForEach(x => x.LocalAuthorityId = laId);

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsAsync(
            Complete.Client.Contracts.ProjectState.Completed, null, null, OrderProjectByField.CompletedAt, OrderByDirection.Descending, 0, 50);

        var activeProjects = projects
            .Where(project => project.State == ProjectState.Active)
            .OrderByDescending(project => project.CompletedAt).ToList();

        var daoRevokedProjects = projects
           .Where(project => project.State == ProjectState.DaoRevoked)
           .OrderByDescending(project => project.CompletedAt).ToList();

        var inActiveProjects = projects
            .Where(project => project.State == ProjectState.Inactive)
            .OrderByDescending(project => project.CompletedAt).ToList();

        var deletedProjects = projects
            .Where(project => project.State == ProjectState.Deleted)
            .OrderByDescending(project => project.CompletedAt).ToList();

        projects = projects
            .Where(project => project.State == ProjectState.Completed)
            .OrderByDescending(project => project.CompletedAt).ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(10, results.Count);
        Assert.Equal(10, projects.Count);
        Assert.Equal(10, activeProjects.Count);
        Assert.Equal(10, daoRevokedProjects.Count);
        Assert.Equal(10, inActiveProjects.Count);
        Assert.Equal(10, deletedProjects.Count);
        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var project = projects[i];
            var establishment = establishments.Find(e => e.Urn?.Value == result.Urn?.Value);

            Assert.NotNull(result.EstablishmentName);
            Assert.Equal(establishment?.Name, result.EstablishmentName);

            Assert.NotNull(result.ProjectId);
            Assert.Equal(project?.Id.Value, result.ProjectId.Value);

            Assert.NotNull(result.Urn);
            Assert.Equal(project?.Urn.Value, result.Urn.Value);
            Assert.Equal(establishment?.Urn?.Value, result.Urn.Value);

            Assert.NotNull(result.ConversionOrTransferDate);
            Assert.Equal(project?.SignificantDate, new DateOnly(result.ConversionOrTransferDate.Value.Year,
                result.ConversionOrTransferDate.Value.Month, result.ConversionOrTransferDate.Value.Day));

            Assert.NotNull(result.State);
            Assert.Equal(project?.State.ToString(), result.State.ToString());

            Assert.NotNull(result.ProjectType);
            Assert.Equal(project?.Type?.ToString(), result.ProjectType.Value.ToString());

            Assert.Equal(project?.FormAMat, result.IsFormAMAT);

            Assert.NotNull(result.AssignedToFullName);
            Assert.Equal($"{project?.AssignedTo?.FirstName} {project?.AssignedTo?.LastName}",
                result.AssignedToFullName);
        }
    }

    [Theory]
    [CustomAutoData(
        typeof(CustomWebApplicationDbContextFactoryCustomization),
        typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsByRegion_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        // Arrange
        factory.TestClaims = new[] { ApiRoles.ReadRole, ApiRoles.WriteRole, ApiRoles.DeleteRole, ApiRoles.UpdateRole }
            .Select(x => new Claim(ClaimTypes.Role, x)).ToList();

        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;

        var giasEstablishment = fixture.Create<GiasEstablishment>();
        var projects = new List<Project>();

        projects.AddRange(Enumerable.Range(0, 10).Select(i => fixture.Customize(new ProjectCustomization
        {
            Region = Region.NorthEast,
            Type = i < 4 ? ProjectType.Conversion : ProjectType.Transfer,
        }).Create<Project>()));

        projects.AddRange(Enumerable.Range(0, 15).Select(i => fixture.Customize(new ProjectCustomization
        {
            Region = Region.SouthEast,
            Type = i < 10 ? ProjectType.Conversion : ProjectType.Transfer,
        }).Create<Project>()));

        var localAuthority = await dbContext.LocalAuthorities.FirstAsync();
        projects.ForEach(project =>
        {
            project.RegionalDeliveryOfficerId = testUser.Id;
            project.LocalAuthorityId = localAuthority.Id;
            project.Urn = giasEstablishment.Urn!;
        });

        dbContext.GiasEstablishments.Add(giasEstablishment);
        dbContext.Projects.AddRange(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsByRegionAsync(null, null);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal(Complete.Client.Contracts.Region.NorthEast, results[0].Region);
        Assert.Equal(4, results[0].ConversionsCount);
        Assert.Equal(6, results[0].TransfersCount);
        Assert.Equal(Complete.Client.Contracts.Region.SouthEast, results[1].Region);
        Assert.Equal(10, results[1].ConversionsCount);
        Assert.Equal(5, results[1].TransfersCount);
    }


    [Theory]
    [CustomAutoData(
        typeof(CustomWebApplicationDbContextFactoryCustomization),
        typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsByRegionCompletedProjects_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        // Arrange
        factory.TestClaims = new[] { ApiRoles.ReadRole, ApiRoles.WriteRole, ApiRoles.DeleteRole, ApiRoles.UpdateRole }
            .Select(x => new Claim(ClaimTypes.Role, x)).ToList();

        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;

        var giasEstablishment = fixture.Create<GiasEstablishment>();
        var projects = new List<Project>();

        projects.AddRange(Enumerable.Range(0, 10).Select(i => fixture.Customize(new ProjectCustomization
        {
            Region = Region.NorthEast,
            Type = i < 4 ? ProjectType.Conversion : ProjectType.Transfer,
            State = i < 6 ? (int)ProjectState.Active : (int)ProjectState.Completed
        }).Create<Project>()));

        projects.AddRange(Enumerable.Range(0, 15).Select(i => fixture.Customize(new ProjectCustomization
        {
            Region = Region.SouthEast,
            Type = i < 10 ? ProjectType.Conversion : ProjectType.Transfer,
            State = i < 8 ? (int)ProjectState.Active : (int)ProjectState.Completed
        }).Create<Project>()));

        var localAuthority = await dbContext.LocalAuthorities.FirstAsync();
        projects.ForEach(project =>
        {
            project.RegionalDeliveryOfficerId = testUser.Id;
            project.LocalAuthorityId = localAuthority.Id;
            project.Urn = giasEstablishment.Urn!;
        });

        dbContext.GiasEstablishments.Add(giasEstablishment);
        dbContext.Projects.AddRange(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsByRegionAsync(
            Complete.Client.Contracts.ProjectState.Completed, null);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);

        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal(Complete.Client.Contracts.Region.NorthEast, results[0].Region);
        Assert.Equal(1, results[0].ConversionsCount);
        Assert.Equal(1, results[0].TransfersCount);
        Assert.Equal(Complete.Client.Contracts.Region.SouthEast, results[1].Region);
        Assert.Equal(2, results[1].ConversionsCount);
        Assert.Equal(1, results[1].TransfersCount);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization), typeof(OmitCircularReferenceCustomization))]
    public async Task GetProjectByUrn_should_return_the_correct_project(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = new[] { ApiRoles.ReadRole, ApiRoles.WriteRole, ApiRoles.DeleteRole, ApiRoles.UpdateRole }
            .Select(x => new Claim(ClaimTypes.Role, x)).ToList();

        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;

        var giasEstablishment = fixture.Create<GiasEstablishment>();
        var expected = fixture.Customize(new ProjectCustomization { RegionalDeliveryOfficerId = testUser.Id, Urn = giasEstablishment.Urn! })
            .Create<Project>();

        expected.RegionalDeliveryOfficer = testUser;

        var localAuthority = await dbContext.LocalAuthorities.FirstAsync();
        expected.LocalAuthorityId = localAuthority.Id;

        dbContext.GiasEstablishments.Add(giasEstablishment);
        dbContext.Projects.Add(expected);

        await dbContext.SaveChangesAsync();

        var actual = await projectsClient.GetProjectAsync(expected.Urn.Value);

        Assert.Equivalent(expected.Id, actual.Id);
        Assert.Equivalent(expected.Urn, actual.Urn);
        Assert.Equivalent(expected.CreatedAt, actual.CreatedAt);
        Assert.Equivalent(expected.UpdatedAt, actual.UpdatedAt);
        Assert.Equivalent(expected.IncomingTrustUkprn, actual.IncomingTrustUkprn);
        Assert.Equivalent(expected.RegionalDeliveryOfficerId, actual.RegionalDeliveryOfficerId);
        Assert.Equivalent(expected.CaseworkerId, actual.CaseworkerId);
        Assert.Equivalent(expected.AssignedAt, actual.AssignedAt);
        Assert.Equivalent(expected.AdvisoryBoardDate, DateOnly.FromDateTime(actual.AdvisoryBoardDate!.Value));
        Assert.Equivalent(expected.AdvisoryBoardConditions, actual.AdvisoryBoardConditions);
        Assert.Equivalent(expected.EstablishmentSharepointLink, actual.EstablishmentSharepointLink);
        Assert.Equivalent(expected.CompletedAt, actual.CompletedAt);
        Assert.Equivalent(expected.IncomingTrustSharepointLink, actual.IncomingTrustSharepointLink);
        Assert.Equivalent(expected.Type.ToString(), actual.Type.ToString());
        Assert.Equivalent(expected.AssignedToId, actual.AssignedToId);
        Assert.Equivalent(expected.SignificantDate, DateOnly.FromDateTime(actual.SignificantDate!.Value));
        Assert.Equivalent(expected.SignificantDateProvisional, actual.SignificantDateProvisional);
        Assert.Equivalent(expected.DirectiveAcademyOrder, actual.DirectiveAcademyOrder);
        Assert.Equivalent(expected.Region.ToString(), actual.Region.ToString());
        Assert.Equivalent(expected.AcademyUrn, actual.AcademyUrn);
        Assert.Equivalent(expected.TasksDataId, actual.TasksDataId);
        Assert.Equivalent(expected.TasksDataType.ToString(), actual.TasksDataType.ToString());
        Assert.Equivalent(expected.OutgoingTrustUkprn, actual.OutgoingTrustUkprn);
        Assert.Equivalent(expected.Team.ToString(), actual.Team.ToString());
        Assert.Equivalent(expected.TwoRequiresImprovement, actual.TwoRequiresImprovement);
        Assert.Equivalent(expected.OutgoingTrustSharepointLink, actual.OutgoingTrustSharepointLink);
        Assert.Equivalent(expected.AllConditionsMet, actual.AllConditionsMet);
        Assert.Equivalent(expected.MainContactId, actual.MainContactId);
        Assert.Equivalent(expected.EstablishmentMainContactId, actual.EstablishmentMainContactId);
        Assert.Equivalent(expected.IncomingTrustMainContactId, actual.IncomingTrustMainContactId);
        Assert.Equivalent(expected.OutgoingTrustMainContactId, actual.OutgoingTrustMainContactId);
        Assert.Equivalent(expected.NewTrustReferenceNumber, actual.NewTrustReferenceNumber);
        Assert.Equivalent(expected.NewTrustName, actual.NewTrustName);
        Assert.Equivalent(expected.State.ToString(), actual.State.ToString());
        Assert.Equivalent(expected.PrepareId, actual.PrepareId);
        Assert.Equivalent(expected.LocalAuthorityMainContactId, actual.LocalAuthorityMainContactId);
        Assert.Equivalent(expected.GroupId, actual.GroupId);
        Assert.Equivalent(expected.AssignedTo, actual.AssignedTo);
        Assert.Equivalent(expected.Caseworker, actual.Caseworker);
        Assert.Equivalent(expected.Contacts, actual.Contacts);
        Assert.Equivalent(expected.Notes, actual.Notes);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsByTrust_Async_ShouldReturnListOfNonFormAMatProjects_WhenTrustExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();

        // Add projects for 
        // Stub TrustV4Client GetTrustByUkprn2Async if not form a mat
        // Get NewTrustReferenceNumber if form a mat

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(50)
            .ToList();

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var projects = establishments.Select(establishment =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                RegionalDeliveryOfficerId = testUser.Id,
                CaseworkerId = testUser.Id,
                AssignedToId = testUser.Id
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            project.IncomingTrustUkprn = new Ukprn(12345678);
            project.NewTrustReferenceNumber = null;
            project.NewTrustName = null;
            project.State = Domain.Enums.ProjectState.Active;
            return project;
        }).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        projects.ForEach(x => x.LocalAuthorityId = localAuthority.Id);

        var trustDto = fixture.Customize(new TrustDtoCustomization() { Ukprn = "12345678" }).Create<TrustDto>();

        Assert.NotNull(factory.WireMockServer);

        factory.WireMockServer.AddGetWithJsonResponse($"/v4/trust/{trustDto.Ukprn}", trustDto);

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsInTrustAsync(
            trustDto.Ukprn, false, 0, 50);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(50, results.Count);
        foreach (var result in results)
        {
            var project = projects.Find(p => p.Id.Value == result.ProjectId?.Value);
            var establishment = establishments.Find(e => e.Urn?.Value == result.Urn?.Value);

            Assert.NotNull(result.Urn);
            Assert.Equal(project?.Urn.Value, result.Urn.Value);
            Assert.Equal(establishment?.Urn?.Value, result.Urn.Value);

            Assert.NotNull(result.EstablishmentName);
            Assert.Equal(establishment?.Name, result.EstablishmentName);

            Assert.NotNull(result.ProjectId);
            Assert.Equal(project?.Id.Value, result.ProjectId.Value);

            Assert.NotNull(result.ConversionOrTransferDate);
            Assert.Equal(project?.SignificantDate, new DateOnly(result.ConversionOrTransferDate.Value.Year,
                result.ConversionOrTransferDate.Value.Month, result.ConversionOrTransferDate.Value.Day));

            Assert.NotNull(result.State);
            Assert.Equal(project?.State.ToString(), result.State.ToString());

            Assert.NotNull(result.ProjectType);
            Assert.Equal(project?.Type?.ToString(), result.ProjectType.Value.ToString());

            Assert.Equal(project?.FormAMat, result.IsFormAMAT);

            Assert.NotNull(result.AssignedToFullName);
            Assert.Equal($"{project?.AssignedTo?.FirstName} {project?.AssignedTo?.LastName}",
                result.AssignedToFullName);
        }
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsByTrust_Async_ShouldReturnListOfFormAMatProjects_WhenTrustExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();

        // Add projects for 
        // Stub TrustV4Client GetTrustByUkprn2Async if not form a mat
        // Get NewTrustReferenceNumber if form a mat

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(50)
            .ToList();

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var projects = establishments.Select(establishment =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                RegionalDeliveryOfficerId = testUser.Id,
                CaseworkerId = testUser.Id,
                AssignedToId = testUser.Id
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            project.IncomingTrustUkprn = null;
            project.NewTrustReferenceNumber = "TR123456";
            project.NewTrustName = "New Trust";
            project.State = Domain.Enums.ProjectState.Active;
            return project;
        }).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        projects.ForEach(x => x.LocalAuthorityId = localAuthority.Id);

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsInTrustAsync(
            "TR123456", true, 0, 50);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(50, results.Count);
        foreach (var result in results)
        {
            var project = projects.Find(p => p.Id.Value == result.ProjectId?.Value);
            var establishment = establishments.Find(e => e.Urn?.Value == result.Urn?.Value);

            Assert.NotNull(result.Urn);
            Assert.Equal(project?.Urn.Value, result.Urn.Value);
            Assert.Equal(establishment?.Urn?.Value, result.Urn.Value);

            Assert.NotNull(result.EstablishmentName);
            Assert.Equal(establishment?.Name, result.EstablishmentName);

            Assert.NotNull(result.ProjectId);
            Assert.Equal(project?.Id.Value, result.ProjectId.Value);

            Assert.NotNull(result.ConversionOrTransferDate);
            Assert.Equal(project?.SignificantDate, new DateOnly(result.ConversionOrTransferDate.Value.Year,
                result.ConversionOrTransferDate.Value.Month, result.ConversionOrTransferDate.Value.Day));

            Assert.NotNull(result.State);
            Assert.Equal(project?.State.ToString(), result.State.ToString());

            Assert.NotNull(result.ProjectType);
            Assert.Equal(project?.Type?.ToString(), result.ProjectType.Value.ToString());

            Assert.Equal(project?.FormAMat, result.IsFormAMAT);

            Assert.NotNull(result.AssignedToFullName);
            Assert.Equal($"{project?.AssignedTo?.FirstName} {project?.AssignedTo?.LastName}",
                result.AssignedToFullName);
        }
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsForLocalAuthority_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;
        var localAuthorityCode = "123456";

        var allEstablishments = fixture
            .Customize(new GiasEstablishmentsCustomization())
            .CreateMany<GiasEstablishment>(50)
            .ToList();

        var matchingEstablishments = allEstablishments.Take(20).ToList();
        var otherEstablishments = allEstablishments.Skip(20).ToList();

        foreach (var establishment in matchingEstablishments)
            establishment.LocalAuthorityCode = localAuthorityCode;

        foreach (var establishment in otherEstablishments)
            establishment.LocalAuthorityCode = fixture.Create<string>();

        await dbContext.GiasEstablishments.AddRangeAsync(allEstablishments);

        var allProjects = allEstablishments.Select(establishment =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                AssignedToId = testUser.Id,
                RegionalDeliveryOfficerId = testUser.Id
            }).Create<Project>();

            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        allProjects.ForEach(p => p.LocalAuthorityId = localAuthority.Id);

        await dbContext.Projects.AddRangeAsync(allProjects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsForLocalAuthorityAsync(
            localAuthorityCode, null, null, 0, 50);

        var expectedProjects = allProjects
            .Where(p => p.State == ProjectState.Active)
            .Where(p => matchingEstablishments.Any(e => e.Urn?.Value == p.Urn.Value))
            .ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(expectedProjects.Count, results.Count);

        foreach (var result in results)
        {
            var project = expectedProjects.Find(p => p.Id.Value == result.ProjectId?.Value);
            var establishment = matchingEstablishments.Find(e => e.Urn?.Value == result.Urn?.Value);

            Assert.NotNull(result.Urn);
            Assert.Equal(project?.Urn.Value, result.Urn.Value);
            Assert.Equal(establishment?.Urn?.Value, result.Urn.Value);

            Assert.NotNull(result.EstablishmentName);
            Assert.Equal(establishment?.Name, result.EstablishmentName);

            Assert.NotNull(result.ProjectId);
            Assert.Equal(project?.Id.Value, result.ProjectId.Value);

            Assert.NotNull(result.ConversionOrTransferDate);
            Assert.Equal(project?.SignificantDate, new DateOnly(
                result.ConversionOrTransferDate.Value.Year,
                result.ConversionOrTransferDate.Value.Month,
                result.ConversionOrTransferDate.Value.Day));

            Assert.NotNull(result.State);
            Assert.Equal(project?.State.ToString(), result.State.ToString());

            Assert.NotNull(result.ProjectType);
            Assert.Equal(project?.Type?.ToString(), result.ProjectType.Value.ToString());

            Assert.Equal(project?.FormAMat, result.IsFormAMAT);

            Assert.NotNull(result.AssignedToFullName);
            Assert.Equal($"{project?.AssignedTo?.FirstName} {project?.AssignedTo?.LastName}",
                result.AssignedToFullName);
        }
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsForRegion_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;

        var expectedRegion = Complete.Client.Contracts.Region.EastMidlands;

        var giasEstablishment = fixture.Create<GiasEstablishment>();
        var projects = fixture.Customize(new ProjectCustomization { RegionalDeliveryOfficerId = testUser.Id, Urn = giasEstablishment.Urn! })
            .CreateMany<Project>(50).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        projects.ForEach(p => p.LocalAuthorityId = localAuthority.Id);

        await dbContext.GiasEstablishments.AddAsync(giasEstablishment);
        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsForRegionAsync(
            expectedRegion, null, null, AssignedToState.AssignedOnly, null, null, 0, 50);

        var expectedProjects = projects
            .Where(p => p.State == ProjectState.Active)
            .Where(p => p.Region != null && (Dfe.Complete.Client.Contracts.Region)p.Region == expectedRegion)
            .ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(expectedProjects.Count, results.Count);
        Assert.All(results, project => Assert.Equal(project.Region, expectedRegion));
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsForRegionAsync_InvalidRegionSent_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient)
    {
        // Arrange
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Act & Assert
        await Assert.ThrowsAsync<CompleteApiException>(() =>
            projectsClient.ListAllProjectsForRegionAsync(null, null, null, null, null, null, 0, 50));
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsForTeam_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;

        var expectedTeam = ProjectTeam.BusinessSupport;

        var giasEstablishment = fixture.Create<GiasEstablishment>();
        var projects = fixture.Customize(new ProjectCustomization { RegionalDeliveryOfficerId = testUser.Id, Urn = giasEstablishment.Urn! })
            .CreateMany<Project>(50).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        projects.ForEach(p => p.LocalAuthorityId = localAuthority.Id);

        await dbContext.GiasEstablishments.AddAsync(giasEstablishment);
        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsForTeamAsync(
            expectedTeam, null, null, AssignedToState.AssignedOnly, null, null, 0, 50);

        var expectedProjects = projects
            .Where(p => p.State == Domain.Enums.ProjectState.Active)
            .Where(p => p.Region != null && (ProjectTeam?)p.Team == expectedTeam)
            .ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(expectedProjects.Count, results.Count);
        Assert.All(results, project => Assert.Equal(project.Team, expectedTeam));
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsForTeamAsync_InvalidRegionSent_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient)
    {
        // Arrange
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Act & Assert
        await Assert.ThrowsAsync<CompleteApiException>(() =>
            projectsClient.ListAllProjectsForTeamAsync(null, null, null, null, null, null, 0, 50));
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private class ListByUserInlineAutoDataAttribute(ProjectUserFilter filter) : CompositeDataAttribute(
            new InlineDataAttribute(filter),
            new CustomAutoDataAttribute(
                    typeof(CustomWebApplicationDbContextFactoryCustomization),
                    typeof(GiasEstablishmentsCustomization)))
    {
    }

    [Theory]
    [ListByUserInlineAutoData(ProjectUserFilter.AssignedTo)]
    [ListByUserInlineAutoData(ProjectUserFilter.CreatedBy)]
    public async Task ListAllProjectsForUserAsync_ShouldReturnProjects(
    ProjectUserFilter filter,
    CustomWebApplicationDbContextFactory<Program> factory,
    IProjectsClient projectsClient,
    IFixture fixture)
    {
        const int numberOfEstablishments = 50;
        const int numberOfProjectsAssignedToUser = 10;
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.OrderBy(user => user.CreatedAt).FirstAsync();
        var otherUser = await dbContext.Users.FirstAsync(user => user.Id != testUser.Id);
        const string userAdId = "test-user-adid";

        testUser.ActiveDirectoryUserId = userAdId;
        var incomingTrust = new TrustDto { Ukprn = "12345678", Name = "Trust One" };
        var outgoingTrust = new TrustDto { Ukprn = "87654321", Name = "Trust Two" };
        var trustResults = new List<TrustDto>();
        for (var i = 0; i < numberOfProjectsAssignedToUser; i++)
        {
            trustResults.Add(incomingTrust);
            trustResults.Add(outgoingTrust);
        }

        Assert.NotNull(factory.WireMockServer);
        factory.WireMockServer.AddGetWithJsonResponse(TrustClientEndpointConstants.GetByUkprnsAll, trustResults.ToArray());

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization())
            .CreateMany<GiasEstablishment>(numberOfEstablishments)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);

        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                IncomingTrustUkprn = "12345678",
                OutgoingTrustUkprn = "87654321",
                RegionalDeliveryOfficerId = otherUser.Id
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            switch (filter)
            {
                case ProjectUserFilter.AssignedTo:
                    if (i < numberOfProjectsAssignedToUser) project.AssignedToId = testUser.Id;
                    break;
                case ProjectUserFilter.CreatedBy:
                    if (i < numberOfProjectsAssignedToUser) project.RegionalDeliveryOfficerId = testUser.Id;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Filter not supported {filter}");
            }

            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results =
            await projectsClient.ListAllProjectsForUserAsync(null, userAdId, filter, null, null, null, null, numberOfEstablishments);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(numberOfProjectsAssignedToUser, results.Count);
        Assert.All(results, project =>
        {
            var result = results.First(p => p.ProjectId?.Value == project.ProjectId?.Value);
            Assert.Equal(result.IncomingTrustName, project.IncomingTrustName);
            Assert.Equal("Trust Two", project.OutgoingTrustName);
        });
    }


    [Theory]
    [ListByUserInlineAutoData(ProjectUserFilter.AssignedTo)]
    [ListByUserInlineAutoData(ProjectUserFilter.CreatedBy)]
    public async Task ListAllProjectsForUserAsync_ShouldReturnBadRequest_IfUserNotFound(
        ProjectUserFilter filter,
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CompleteApiException>(() =>
            projectsClient.ListAllProjectsForUserAsync(null, "123", filter, null, null, null, null, 50));

        Assert.Contains("User does not exist for provided UserAdId", exception.Response);
    }


    [Theory]
    [ListByUserInlineAutoData(ProjectUserFilter.AssignedTo)]
    [ListByUserInlineAutoData(ProjectUserFilter.CreatedBy)]
    public async Task ListAllProjectsForUserAsync_DuplicateActiveDirectoryIds_ShouldReturnProjectsForActiveUser(
    ProjectUserFilter filter,
    CustomWebApplicationDbContextFactory<Program> factory,
    IProjectsClient projectsClient,
    IFixture fixture)
    {
        const int numberOfEstablishments = 50;
        const int numberOfProjectsAssignedToUser = 10;
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var otherUser = await dbContext.Users.FirstAsync(user => user.FirstName == "Deactivated");
        var testUser = await dbContext.Users.FirstAsync(user => user.FirstName == "Active"); // We want this one to return projects

        var incomingTrust = new TrustDto { Ukprn = "12345678", Name = "Trust One" };
        var outgoingTrust = new TrustDto { Ukprn = "87654321", Name = "Trust Two" };
        var trustResults = new List<TrustDto>();
        for (var i = 0; i < numberOfProjectsAssignedToUser; i++)
        {
            trustResults.Add(incomingTrust);
            trustResults.Add(outgoingTrust);
        }

        Assert.NotNull(factory.WireMockServer);
        factory.WireMockServer.AddGetWithJsonResponse(TrustClientEndpointConstants.GetByUkprnsAll, trustResults.ToArray());

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization())
            .CreateMany<GiasEstablishment>(numberOfEstablishments)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);

        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                IncomingTrustUkprn = "12345678",
                OutgoingTrustUkprn = "87654321",
                AssignedToId = otherUser.Id,
                RegionalDeliveryOfficerId = otherUser.Id
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            switch (filter)
            {
                case ProjectUserFilter.AssignedTo:
                    if (i < numberOfProjectsAssignedToUser) project.AssignedToId = testUser.Id;
                    break;
                case ProjectUserFilter.CreatedBy:
                    if (i < numberOfProjectsAssignedToUser) project.RegionalDeliveryOfficerId = testUser.Id;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Filter not supported {filter}");
            }

            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results =
            await projectsClient.ListAllProjectsForUserAsync(null, "duplicateAdId", filter, null, null, null, null, numberOfEstablishments);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(numberOfProjectsAssignedToUser, results.Count);
        Assert.All(results, project =>
        {
            var result = results.First(p => p.ProjectId?.Value == project.ProjectId?.Value);
            Assert.Equal(result.IncomingTrustName, project.IncomingTrustName);
            Assert.Equal("Trust Two", project.OutgoingTrustName);
        });
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task SearchProjectsWithEstablishmentName_ShouldReturnBadRequest_IfSearchEmpty(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CompleteApiException>(() =>
            projectsClient.SearchProjectsAsync("  ", [Complete.Client.Contracts.ProjectState.Active, Complete.Client.Contracts.ProjectState.Completed, Complete.Client.Contracts.ProjectState.DaoRevoked], 0, 50));

        Assert.Contains("The SearchTerm field is required.", exception.Response);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task SearchProjectsWithEstablishmentName_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;
        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(10)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);

        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                IncomingTrustUkprn = "12345678",
                OutgoingTrustUkprn = "87654321",
                RegionalDeliveryOfficerId = testUser.Id
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var establishment = establishments.First();
        var searchTerm = establishment.Name;

        // Act
        var results = await projectsClient.SearchProjectsAsync(searchTerm, [Complete.Client.Contracts.ProjectState.Active, Complete.Client.Contracts.ProjectState.Completed, Complete.Client.Contracts.ProjectState.DaoRevoked], 0, 20, CancellationToken.None);

        var expectedProjects = projects
            .Where(p => p.Urn == establishment.Urn)
            .ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(expectedProjects.Count, results.Count);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task SearchProjectsWithUKPRN_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;
        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(10)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);

        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                IncomingTrustUkprn = "12345678",
                OutgoingTrustUkprn = "87654321",
                RegionalDeliveryOfficerId = testUser.Id
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var ukprn = projects.First().IncomingTrustUkprn;
        var projectStatuses = new List<ProjectState> { ProjectState.Active, ProjectState.DaoRevoked, ProjectState.Completed };

        // Act
        var results = await projectsClient.SearchProjectsAsync(ukprn!.ToString(), [Complete.Client.Contracts.ProjectState.Active, Complete.Client.Contracts.ProjectState.Completed, Complete.Client.Contracts.ProjectState.DaoRevoked], 0, 20, CancellationToken.None);

        var expectedProjects = projects
            .Where(p => p.IncomingTrustUkprn == ukprn && projectStatuses.Contains(p.State))
            .ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(expectedProjects.Count, results.Count);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task SearchProjectsWithEstablishmentNumber_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        // Arrange
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;
        int i = 0;
        var establishments = Enumerable.Range(0, 10)
            .Select(_ =>
            {
                i++;

                return fixture.Customize(new GiasEstablishmentsCustomization
                {
                    EstablishmentNumber = i.ToString("D4")
                }).Create<GiasEstablishment>();
            })
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);

        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                IncomingTrustUkprn = "12345678",
                OutgoingTrustUkprn = "87654321",
                RegionalDeliveryOfficerId = testUser.Id
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var establishment = establishments.First();

        // Act
        var results = await projectsClient.SearchProjectsAsync(establishment.EstablishmentNumber!, [Complete.Client.Contracts.ProjectState.Active, Complete.Client.Contracts.ProjectState.Completed, Complete.Client.Contracts.ProjectState.DaoRevoked], 0, 20, CancellationToken.None);

        var expectedProjects = projects
            .Where(p => p.Urn == establishment.Urn && p.State == Domain.Enums.ProjectState.Active)
            .ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(expectedProjects.Count, results.Count);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task SearchProjectsWithUrn_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;
        var random = new Random();
        var usedUrns = new HashSet<int>();

        var establishments = Enumerable.Range(0, 10)
            .Select(_ =>
            {
                int urn;
                do
                {
                    urn = random.Next(100000, 1000000);
                } while (!usedUrns.Add(urn));

                return fixture.Customize(new GiasEstablishmentsCustomization
                {
                    Urn = new Domain.ValueObjects.Urn(urn)
                }).Create<GiasEstablishment>();
            })
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                IncomingTrustUkprn = "12345678",
                OutgoingTrustUkprn = "87654321",
                State = Domain.Enums.ProjectState.Active.GetHashCode(),
                RegionalDeliveryOfficerId = testUser.Id
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var urn = projects.First(p => p.State == Domain.Enums.ProjectState.Active).Urn;

        // Act
        var results = await projectsClient.SearchProjectsAsync(urn!.Value.ToString(), [Complete.Client.Contracts.ProjectState.Active, Complete.Client.Contracts.ProjectState.Completed, Complete.Client.Contracts.ProjectState.DaoRevoked], 0, 20, CancellationToken.None);

        var expectedProjects = projects
            .Where(p => p.Urn == urn)
            .ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(expectedProjects.Count, results.Count);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization),
        typeof(OmitCircularReferenceCustomization))]
    public async Task ListAllProjectsByTrustRef_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(10)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var trustPref = "TR12345";
        var trustReference = string.Concat(trustPref, 0);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                IncomingTrustUkprn = "12345678",
                OutgoingTrustUkprn = "87654321",
                NewTrustReferenceNumber = string.Concat(trustPref, i),
                RegionalDeliveryOfficerId = testUser.Id
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var ukprn = projects.First().IncomingTrustUkprn;

        var expectedProjects = projects
            .Where(p => p.NewTrustReferenceNumber == trustReference && p.State == Domain.Enums.ProjectState.Active)
            .ToList();

        // Act
        var results =
            await projectsClient.ListAllProjectsByTrustRefAsync(trustReference, 0, 20, CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(expectedProjects.Count, results.Count);
    }


    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsByTrustRef_Async_ShouldReturnNoList_OnNoRefMatched(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(1)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var trustPref = "TR12345";
        var trustReference = string.Concat(trustPref, 0);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                IncomingTrustUkprn = "12345678",
                OutgoingTrustUkprn = "87654321",
                NewTrustReferenceNumber = string.Concat(trustPref, i),
                RegionalDeliveryOfficerId = testUser.Id
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var ukprn = projects.First().IncomingTrustUkprn;

        // Act
        var results = await projectsClient.ListAllProjectsByTrustRefAsync("TR12345", 0, 20, CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsStatistics_Async_ShouldReturnStatistics(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();
        const string userAdId = "test-user-adid";
        testUser.ActiveDirectoryUserId = userAdId;

        var giasEstablishment = fixture.Create<GiasEstablishment>();
        var projects = fixture.Customize(new ProjectCustomization { RegionalDeliveryOfficerId = testUser.Id, Urn = giasEstablishment.Urn! })
            .CreateMany<Project>(50).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        projects.ForEach(p => p.LocalAuthorityId = localAuthority!.Id);

        await dbContext.GiasEstablishments.AddAsync(giasEstablishment);
        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var excludeStates = new List<ProjectState> { ProjectState.Deleted };
        var expectedConversionProjects = projects.Where(p => p.Type == ProjectType.Conversion && !excludeStates.Contains(p.State));
        var expectedTransfersProjects = projects.Where(p => p.Type == ProjectType.Transfer && !excludeStates.Contains(p.State));

        // Act
        var results = await projectsClient.ListAllProjectsStatisticsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.NotNull(results.OverAllProjects);
        Assert.NotNull(results.OverAllProjects.Conversions);
        Assert.NotNull(results.OverAllProjects.Transfers);
        Assert.Equal(expectedConversionProjects.Count(), results.OverAllProjects.Conversions.TotalProjects);
        Assert.Equal(expectedTransfersProjects.Count(), results.OverAllProjects.Transfers.TotalProjects);
        Assert.NotNull(results.ConversionsPerRegion);
        Assert.NotNull(results.TransfersPerRegion);
        Assert.NotNull(results.NewProjects);
        Assert.NotNull(results.RegionalCaseworkServicesProjects);
        Assert.NotNull(results.NotRegionalCaseworkServicesProjects);
        Assert.NotNull(results.UsersPerTeam);
        Assert.NotNull(results.SixMonthViewOfAllProjectOpeners);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllConvertingProjects_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.CreateMany<GiasEstablishment>(50).ToList();
        await dbContext.GiasEstablishments.AddRangeAsync(establishments);

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        var projects = establishments.Select(est =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                RegionalDeliveryOfficerId = testUser.Id,
                CaseworkerId = testUser.Id,
                AssignedToId = testUser.Id,
                LocalAuthorityId = localAuthority.Id,
            }).Create<Project>();
            project.Urn = est.Urn ?? project.Urn;
            project.AcademyUrn = est.Urn;
            project.Type = ProjectType.Conversion;
            project.State = ProjectState.Active;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsConvertingAsync(true, 0, 50);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(50, results.Count);

        foreach (var result in results)
        {
            var matchingProject = projects.FirstOrDefault(p => p.Id.Value == result.ProjectId?.Value);
            var matchingEstablishment = establishments.FirstOrDefault(e => e.Urn?.Value == result.AcademyUrn);

            Assert.NotNull(result.ProjectId);
            Assert.Equal(matchingProject?.Id.Value, result.ProjectId.Value);

            Assert.NotNull(result.AcademyUrn);
            Assert.Equal(matchingProject?.AcademyUrn?.Value, result.AcademyUrn.Value);

            Assert.NotNull(result.AcademyName);
            Assert.Equal(matchingEstablishment?.Name, result.AcademyName);
        }
    }
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsHandover_Async_ShouldReturnProjectByUrn(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(10)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var trustPref = "TR12345";
        var trustReference = string.Concat(trustPref, 0);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                RegionalDeliveryOfficerId = testUser.Id,
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            project.State = ProjectState.Inactive;
            project.IncomingTrustUkprn = null;
            project.OutgoingTrustUkprn = null;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var urn = projects.First().Urn;

        // Act
        var results = await projectsClient.ListAllProjectsHandoverAsync(Complete.Client.Contracts.ProjectState.Inactive, urn.Value, OrderProjectByField.SignificantDate, OrderByDirection.Ascending, 0, 50, CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);

        foreach (var result in results)
        {
            var matchingProject = projects.FirstOrDefault(p => p.Id.Value == result.ProjectId?.Value);
            var matchingEstablishment = establishments.FirstOrDefault(e => e.Urn?.Value == result.Urn?.Value);

            Assert.NotNull(result.ProjectId);
            Assert.Equal(matchingProject?.Id.Value, result.ProjectId.Value);

            Assert.NotNull(result.EstablishmentName);
            Assert.NotNull(matchingEstablishment);
            Assert.Equal(matchingEstablishment.Name, result.EstablishmentName);
        }
    }
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task ListAllProjectsHandover_Async_ShouldReturnList(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(10)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var trustPref = "TR12345";
        var trustReference = string.Concat(trustPref, 0);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                RegionalDeliveryOfficerId = testUser.Id,
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            project.State = ProjectState.Inactive;
            project.IncomingTrustUkprn = null;
            project.OutgoingTrustUkprn = null;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        // Act
        var results = await projectsClient.ListAllProjectsHandoverAsync(Complete.Client.Contracts.ProjectState.Inactive, null, OrderProjectByField.SignificantDate, OrderByDirection.Ascending, 0, 50, CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(10, results.Count);

        foreach (var result in results)
        {
            var matchingProject = projects.FirstOrDefault(p => p.Id.Value == result.ProjectId?.Value);
            var matchingEstablishment = establishments.FirstOrDefault(e => e.Urn?.Value == result.Urn?.Value);

            Assert.NotNull(result.ProjectId);
            Assert.Equal(matchingProject?.Id.Value, result.ProjectId.Value);

            Assert.NotNull(result.EstablishmentName);
            Assert.NotNull(matchingEstablishment);
            Assert.Equal(matchingEstablishment.Name, result.EstablishmentName);
        }
    }
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task GetHandoverProjectDetails_Async_ShouldReturnHandoverDetails(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(1)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var trustPref = "TR12345";
        var trustReference = string.Concat(trustPref, 0);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                RegionalDeliveryOfficerId = testUser.Id,
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            project.IncomingTrustUkprn = null;
            project.OutgoingTrustUkprn = null;
            project.State = ProjectState.Inactive;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var projectId = projects.First().Id;

        // Act
        var result = await projectsClient.GetHandoverProjectDetailsAsync(projectId.Value, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var matchingProject = projects.FirstOrDefault(p => p.Id.Value == result.ProjectId?.Value);
        var matchingEstablishment = establishments.FirstOrDefault(e => e.Urn?.Value == result.Urn?.Value);
        Assert.Equal(matchingProject?.Urn?.Value, result.Urn?.Value);
        Assert.NotNull(result.ProjectId);
        Assert.Equal(matchingProject?.Id.Value, result.ProjectId.Value);

        Assert.NotNull(result.EstablishmentName);
        Assert.NotNull(matchingEstablishment);
        Assert.Equal(matchingEstablishment.Name, result.EstablishmentName);
    }
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task AssignHandoverProject_Async_WithConversionType_ShouldAssignToRegionalCaseworkerTeam(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [
            new Claim(ClaimTypes.Role, ApiRoles.ReadRole),
            new Claim(ClaimTypes.Role, ApiRoles.WriteRole),
            new Claim(ClaimTypes.Role, ApiRoles.UpdateRole)
        ];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(1)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var trustPref = "TR12345";
        var trustReference = string.Concat(trustPref, 0);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                RegionalDeliveryOfficerId = testUser.Id,
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            project.Type = ProjectType.Conversion;
            project.State = ProjectState.Inactive;
            project.OutgoingTrustSharepointLink = null;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var projectId = projects.First().Id;
        var command = new UpdateHandoverProjectCommand
        {
            IncomingTrustSharepointLink = "https://example.com/sharepoint",
            AssignedToRegionalCaseworkerTeam = true,
            HandoverComments = "Handover comments",
            TwoRequiresImprovement = true,
            UserTeam = ProjectTeam.London,
            ProjectId = new Complete.Client.Contracts.ProjectId { Value = projectId.Value },
            SchoolSharepointLink = "https://example.com/school-sharepoint",
            UserId = new Complete.Client.Contracts.UserId { Value = testUser.Id.Value }
        };

        // Act
        await projectsClient.AssignHandoverProjectAsync(command, CancellationToken.None);

        // Assert  
        dbContext.ChangeTracker.Clear();
        var dbProject = await dbContext.Projects.FirstAsync();
        var dbNotes = await dbContext.Notes.FirstAsync();

        var matchingProject = projects.FirstOrDefault(p => p.Id.Value == projectId.Value);
        Assert.NotNull(dbProject);
        Assert.Equal(dbProject.State.GetHashCode(), ProjectState.Active.GetHashCode());
        Assert.Equal(dbProject.IncomingTrustSharepointLink, command.IncomingTrustSharepointLink);
        Assert.Equal(dbNotes.Body, command.HandoverComments);
        Assert.Equal(dbProject.EstablishmentSharepointLink, command.SchoolSharepointLink);
        Assert.Equal(dbProject.TwoRequiresImprovement, command.TwoRequiresImprovement);
        Assert.Null(dbProject.OutgoingTrustSharepointLink);
    }
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task AssignHandoverProject_Async_WithTransferType_ShouldAssignToRegionalCaseworkerTeam(
       CustomWebApplicationDbContextFactory<Program> factory,
       IProjectsClient projectsClient,
       IFixture fixture)
    {
        factory.TestClaims = [
            new Claim(ClaimTypes.Role, ApiRoles.ReadRole),
            new Claim(ClaimTypes.Role, ApiRoles.WriteRole),
            new Claim(ClaimTypes.Role, ApiRoles.UpdateRole)
        ];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(1)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var trustPref = "TR12345";
        var trustReference = string.Concat(trustPref, 0);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                RegionalDeliveryOfficerId = testUser.Id,
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            project.Type = ProjectType.Transfer;
            project.State = ProjectState.Inactive;
            return project;
        }).ToList();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var projectId = projects.First().Id;
        var command = new UpdateHandoverProjectCommand
        {
            IncomingTrustSharepointLink = "https://example.com/sharepoint",
            AssignedToRegionalCaseworkerTeam = false,
            OutgoingTrustSharepointLink = "https://example.com/outgoing-sharepoint",
            UserTeam = ProjectTeam.London,
            ProjectId = new Complete.Client.Contracts.ProjectId { Value = projectId.Value },
            SchoolSharepointLink = "https://example.com/school-sharepoint",
            UserId = new Complete.Client.Contracts.UserId { Value = testUser.Id.Value }
        };

        // Act
        await projectsClient.AssignHandoverProjectAsync(command, CancellationToken.None);

        // Assert  
        dbContext.ChangeTracker.Clear();
        var dbProject = await dbContext.Projects.FirstAsync();

        var matchingProject = projects.FirstOrDefault(p => p.Id.Value == projectId.Value);
        Assert.NotNull(dbProject);
        Assert.Equal(dbProject.State.GetHashCode(), ProjectState.Active.GetHashCode());
        Assert.Equal(dbProject.IncomingTrustSharepointLink, command.IncomingTrustSharepointLink);
        Assert.Equal(dbProject.EstablishmentSharepointLink, command.SchoolSharepointLink);
        Assert.Equal(dbProject.OutgoingTrustSharepointLink, command.OutgoingTrustSharepointLink);
        Assert.Equal(dbProject.AssignedToId?.Value, command.UserId.Value);
        Assert.Empty(dbProject.Notes);
        Assert.Null(dbProject.TwoRequiresImprovement);
        Assert.NotNull(dbProject.AssignedAt);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization),
        typeof(GiasEstablishmentsCustomization),
        typeof(IgnoreVirtualMembersCustomisation))]
    public async Task GetProjectSignificantDateAsync_ShouldReturnProjectDto(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();

        var giasEstablishment = fixture.Create<GiasEstablishment>();
        var project = fixture.Customize(new ProjectCustomization
        {
            RegionalDeliveryOfficerId = testUser.Id,
            Urn = giasEstablishment.Urn!,
        }).Create<Project>();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        project.LocalAuthorityId = localAuthority!.Id;

        await dbContext.GiasEstablishments.AddAsync(giasEstablishment);
        await dbContext.Projects.AddAsync(project);
        await dbContext.SaveChangesAsync();

        var result = await projectsClient.GetProjectSignificantDateAsync(project.Id.Value.ToString());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project.Id.Value, result.Id?.Value);
        Assert.Equal(project.SignificantDate, DateOnly.FromDateTime(result.SignificantDate!.Value));
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task GetKeyContactByProjectIdAsync_Async_ShouldReturn_KeyContacts(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole)];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(1)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var trustPref = "TR12345";
        var trustReference = string.Concat(trustPref, 0);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                RegionalDeliveryOfficerId = testUser.Id,
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            project.IncomingTrustUkprn = null;
            project.OutgoingTrustUkprn = null;
            project.State = ProjectState.Inactive;
            return project;
        }).ToList();
        await dbContext.Projects.AddRangeAsync(projects);
        var projectId = projects.First().Id;
        var keyContact = fixture.Customize(new KeyContactCustomization
        {
            ProjectId = projectId,
        }).Create<KeyContact>();
        await dbContext.KeyContacts.AddAsync(keyContact);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await projectsClient.GetKeyContactByProjectIdAsync(projectId.Value, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(keyContact.Id?.Value, result.Id?.Value);
        Assert.Equal(keyContact.HeadteacherId?.Value, result.HeadteacherId?.Value);
        Assert.Equal(keyContact.OutgoingTrustCeoId?.Value, result.OutgoingTrustCeoId?.Value);
        Assert.Equal(keyContact.IncomingTrustCeoId?.Value, result.IncomingTrustCeoId?.Value);
        Assert.Equal(keyContact.ChairOfGovernorsId?.Value, result.ChairOfGovernorsId?.Value);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization), typeof(GiasEstablishmentsCustomization))]
    public async Task RecordDaoRevocationDecisionAsync_Async_ShouldSet_DaoRevocation(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        factory.TestClaims = [
           new Claim(ClaimTypes.Role, ApiRoles.ReadRole),
            new Claim(ClaimTypes.Role, ApiRoles.WriteRole),
            new Claim(ClaimTypes.Role, ApiRoles.UpdateRole)
       ];

        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(1)
            .ToList();
        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);
        var projects = establishments.Select((establishment, i) =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                LocalAuthorityId = localAuthority.Id,
                RegionalDeliveryOfficerId = testUser.Id,
            }).Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            project.IncomingTrustUkprn = null;
            project.OutgoingTrustUkprn = null;
            project.State = ProjectState.Active;
            project.DirectiveAcademyOrder = true;
            return project;
        }).ToList();
        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var decision = new RecordDaoRevocationDecisionCommand
        {
            ProjectId = new ProjectId { Value = projects.First().Id.Value },
            UserId = new UserId { Value = testUser.Id.Value },
            DecisionMakerRole = "Minister",
            MinisterName = "Test Minister",
            DecisionDate = DateTime.UtcNow,
            ReasonNotes = new Dictionary<DaoRevokedReason, string> { { DaoRevokedReason.SchoolClosedOrClosing, "Closing school" } }
        };

        // Act
        await projectsClient.RecordDaoRevocationDecisionAsync(decision, CancellationToken.None);

        // Assert
        dbContext.ChangeTracker.Clear();
        var dbProject = await dbContext.Projects.FirstAsync();
        var dbDaoRevocation = await dbContext.DaoRevocations.FirstAsync();
        Assert.NotNull(dbProject);
        Assert.Equal(decision.ProjectId.Value, dbProject.Id.Value);
        Assert.Equal(ProjectState.DaoRevoked, dbProject.State);
        Assert.True(dbProject.DirectiveAcademyOrder);
        Assert.NotNull(dbDaoRevocation);
        Assert.Equal(dbDaoRevocation.DecisionMakersName, decision.MinisterName);
        Assert.Equal(testUser.Id.Value, decision.UserId.Value);
        Assert.NotNull(dbDaoRevocation.DateOfDecision);
        var dbDaoRevocationReason = await dbContext.DaoRevocationReasons.FirstAsync();
        Assert.NotNull(dbDaoRevocationReason);
        var dbNotes = await dbContext.Notes.FirstAsync(x => x.NotableType == Domain.Enums.NotableType.DaoRevocationReason.ToDescription());
        Assert.NotNull(dbNotes);
        var note = decision.ReasonNotes.First();
        Assert.Equal(note.Value, dbNotes.Body);
        Assert.Equal(decision.ProjectId.Value, dbNotes.ProjectId.Value);
    }

    [Theory]
    [CustomAutoData(
        typeof(CustomWebApplicationDbContextFactoryCustomization),
        typeof(DateOnlyCustomization),
        typeof(OmitCircularReferenceCustomization),
        typeof(ProjectCustomization))]
    public async Task UpdateProjectCompleteAsync_ShouldUpdateProject(
  CustomWebApplicationDbContextFactory<Program> factory,
  IProjectsClient projectsClient,
  IFixture fixture)
    {
        // Arrange
        var dbContext = factory.GetDbContext<CompleteContext>();

        var testUser = await dbContext.Users.FirstOrDefaultAsync();

        factory.TestClaims = [
            new Claim(ClaimTypes.Role, ApiRoles.WriteRole),
        new Claim(ClaimTypes.Role, ApiRoles.ReadRole),
        new Claim(ClaimTypes.Role, ApiRoles.UpdateRole),
        new Claim(CustomClaimTypeConstants.UserId, testUser!.Id.Value.ToString()),
        ];

        var establishment = fixture.Create<GiasEstablishment>();
        var localAuthority = fixture.Create<LocalAuthority>();
        var project = fixture.Create<Project>();
        project.Urn = establishment.Urn ?? project.Urn;
        project.LocalAuthorityId = localAuthority.Id;
        project.RegionalDeliveryOfficerId = testUser!.Id;
        project.CaseworkerId = testUser!.Id;
        project.AssignedToId = testUser!.Id;
        await dbContext.LocalAuthorities.AddAsync(localAuthority);
        await dbContext.GiasEstablishments.AddAsync(establishment);
        await dbContext.Projects.AddAsync(project);
        await dbContext.SaveChangesAsync();

        Assert.NotNull(testUser);

        var command = fixture.Create<UpdateProjectCompletedCommand>();
        command.ProjectId = new Complete.Client.Contracts.ProjectId { Value = project.Id.Value };

        // Act
        await projectsClient.UpdateCompleteAsync(command);

        // Assert        
        dbContext.ChangeTracker.Clear();
        var projects = await dbContext.Projects.ToListAsync();
        var updatedProject = projects.FirstOrDefault(n => n.Id.Value == project.Id.Value);

        Assert.NotNull(project);
        Assert.Equal(ProjectState.Completed, updatedProject?.State);
    }

    [Theory]
    [CustomAutoData(
       typeof(CustomWebApplicationDbContextFactoryCustomization),
       typeof(ProjectCustomization))]
    public async Task UpdateCompleteAsync_ShouldUpdateCompleted_WhenProjectExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        // Arrange
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole), new Claim(ClaimTypes.Role, ApiRoles.UpdateRole), new Claim(ClaimTypes.Role, ApiRoles.WriteRole)];

        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(1)
            .ToList();

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);

        var projects = establishments.Select(establishment =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                RegionalDeliveryOfficerId = testUser.Id,
                CaseworkerId = testUser.Id,
                AssignedToId = testUser.Id,
                State = 0
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        projects.ForEach(x => x.LocalAuthorityId = localAuthority.Id);
        var transferTaskData = fixture.Create<TransferTasksData>();

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var project = projects.First();

        var command = new UpdateProjectCompletedCommand()
        {
            ProjectId = new ProjectId() { Value = project.Id.Value }
        };

        // Act
        Assert.Equal(ProjectState.Active, project.State);
        await projectsClient.UpdateCompleteAsync(command);

        // Assert
        dbContext.ChangeTracker.Clear();
        var existingProject = await dbContext.Projects.SingleOrDefaultAsync(x => x.Id == project.Id);
        Assert.NotNull(existingProject);
        Assert.Equal(ProjectState.Completed, existingProject.State);
    }
    [Theory]
    [CustomAutoData(
       typeof(CustomWebApplicationDbContextFactoryCustomization),
       typeof(ProjectCustomization))]
    public async Task UpdateDeleteProjectStatusAsync_ShouldSetDeleteStatus_WhenProjectExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        // Arrange
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole), new Claim(ClaimTypes.Role, ApiRoles.UpdateRole), new Claim(ClaimTypes.Role, ApiRoles.WriteRole)];

        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(1)
            .ToList();

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);

        var projects = establishments.Select(establishment =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                RegionalDeliveryOfficerId = testUser.Id,
                CaseworkerId = testUser.Id,
                AssignedToId = testUser.Id,
                State = 0
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        projects.ForEach(x => x.LocalAuthorityId = localAuthority.Id);

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var project = projects.First();

        var command = new UpdateDeleteProjectCommand()
        {
            ProjectId = new ProjectId() { Value = project.Id.Value }
        };

        // Act
        Assert.Equal(ProjectState.Active, project.State);
        await projectsClient.UpdateDeleteProjectStatusAsync(command);

        // Assert
        dbContext.ChangeTracker.Clear();
        var existingProject = await dbContext.Projects.SingleOrDefaultAsync(x => x.Id == project.Id);
        Assert.NotNull(existingProject);
        Assert.Equal(ProjectState.Deleted, existingProject.State);
    }
    [Theory]
    [CustomAutoData(
       typeof(CustomWebApplicationDbContextFactoryCustomization),
       typeof(ProjectCustomization))]
    public async Task UpdateDeleteProjectStatusAsync_ShouldNotUpdate_WhenProjectIdNotMatched(
        CustomWebApplicationDbContextFactory<Program> factory,
        IProjectsClient projectsClient,
        IFixture fixture)
    {
        // Arrange
        factory.TestClaims = [new Claim(ClaimTypes.Role, ApiRoles.ReadRole), new Claim(ClaimTypes.Role, ApiRoles.UpdateRole), new Claim(ClaimTypes.Role, ApiRoles.WriteRole)];

        var dbContext = factory.GetDbContext<CompleteContext>();
        var testUser = await dbContext.Users.FirstAsync();

        var establishments = fixture.Customize(new GiasEstablishmentsCustomization()).CreateMany<GiasEstablishment>(1)
            .ToList();

        await dbContext.GiasEstablishments.AddRangeAsync(establishments);

        var projects = establishments.Select(establishment =>
        {
            var project = fixture.Customize(new ProjectCustomization
            {
                RegionalDeliveryOfficerId = testUser.Id,
                CaseworkerId = testUser.Id,
                AssignedToId = testUser.Id,
                State = 0
            })
                .Create<Project>();
            project.Urn = establishment.Urn ?? project.Urn;
            return project;
        }).ToList();

        var localAuthority = dbContext.LocalAuthorities.AsEnumerable().MinBy(_ => Guid.NewGuid());
        Assert.NotNull(localAuthority);
        projects.ForEach(x => x.LocalAuthorityId = localAuthority.Id);

        await dbContext.Projects.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();
        var project = projects.First();

        var command = new UpdateDeleteProjectCommand()
        {
            ProjectId = new ProjectId() { Value = Guid.NewGuid() }
        };

        // Act
        Assert.Equal(ProjectState.Active, project.State);
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => projectsClient.UpdateDeleteProjectStatusAsync(command));

        // Assert
        Assert.Contains($"Project with ProjectId {{ Value = {command.ProjectId.Value} }} is not found.", exception.Message);
        dbContext.ChangeTracker.Clear();
        var existingProject = await dbContext.Projects.SingleOrDefaultAsync(x => x.Id == project.Id);
        Assert.NotNull(existingProject);
        Assert.Equal(ProjectState.Active, existingProject.State);
    }
}