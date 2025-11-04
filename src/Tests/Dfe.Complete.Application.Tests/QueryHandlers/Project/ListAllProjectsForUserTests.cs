using AutoFixture;
using AutoFixture.Xunit2;
using Dfe.AcademiesApi.Client.Contracts;
using Dfe.Complete.Application.Common.Models;
using Dfe.Complete.Application.Projects.Interfaces;
using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Application.Projects.Queries.ListAllProjects;
using Dfe.Complete.Application.Users.Queries.GetUser;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Tests.Common.Customizations.Models;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations;
using MediatR;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NSubstitute;
using System.Collections.ObjectModel;

namespace Dfe.Complete.Application.Tests.QueryHandlers.Project;

public class ListAllProjectsForUserTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private class InlineAutoDataAttribute(ProjectUserFilter filter, OrderProjectByField sortingField,
        OrderByDirection sortingDirection) : CompositeDataAttribute(
            new InlineDataAttribute(filter),
            new InlineDataAttribute(new OrderProjectQueryBy(sortingField, sortingDirection)),
            new CustomAutoDataAttribute(
                    typeof(OmitCircularReferenceCustomization),
                    typeof(ListAllProjectsQueryModelCustomization),
                    typeof(DateOnlyCustomization)))
    {
    }

    [Theory]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.SignificantDate, OrderByDirection.Ascending)]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserValueIsNull(
    ProjectUserFilter filter,
    OrderProjectQueryBy ordering,
    [Frozen] IListAllProjectsQueryService mockListAllProjectsQueryService,
    [Frozen] Mock<ISender> mockSender,
    Mock<ILogger<ListAllProjectsForUserQueryHandler>> _mockLogger)
    {
        // Arrange
        var mockTrustsClient = new Mock<ITrustsV4Client>();
        var handler = new ListAllProjectsForUserQueryHandler(
            mockListAllProjectsQueryService,
            mockTrustsClient.Object,
            mockSender.Object,
            _mockLogger.Object);

        // Simulate user lookup returns null value
        mockSender.Setup(sender => sender.Send(It.IsAny<GetUserByOidQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto?>.Success(null));

        var query = new ListAllProjectsForUserQuery(
            ProjectState.Active,
            "missing-ad-id",
            filter,
            ordering)
        { Page = 1 };

        // Act
        var result = await handler.Handle(query, default);

        //Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("User not found.", result.Error);
    }


    [Theory]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.SignificantDate, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.SignificantDate, OrderByDirection.Descending)]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.CreatedAt, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.CreatedAt, OrderByDirection.Descending)]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.CompletedAt, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.CompletedAt, OrderByDirection.Descending)]
    public async Task Handle_ShouldReturnCorrectList_WhenPaginationIsCorrect(
        ProjectUserFilter filter,
        OrderProjectQueryBy ordering,
        [Frozen] IListAllProjectsQueryService mockListAllProjectsQueryService,
        [Frozen] Mock<ISender> mockSender,
        IFixture fixture,
        Mock<ILogger<ListAllProjectsForUserQueryHandler>> _mockLogger)
    {
        //Arrange 
        var mockTrustsClient = new Mock<ITrustsV4Client>();

        var handler = new ListAllProjectsForUserQueryHandler(mockListAllProjectsQueryService,
            mockTrustsClient.Object,
            mockSender.Object,
            _mockLogger.Object);

        var userDto = fixture.Create<UserDto>();

        mockSender.Setup(sender => sender.Send(It.IsAny<GetUserByOidQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto?>.Success(userDto));

        var mockListAllProjectsForUserQueryModels = fixture.CreateMany<ListAllProjectsQueryModel>(50).ToList();

        var trustDtos = new ObservableCollection<TrustDto>(
            fixture.Build<TrustDto>().With(dto => dto.Ukprn, new Ukprn(new Random().Next()).Value.ToString)
                .CreateMany(10));

        mockTrustsClient.Setup(service => service.GetByUkprnsAllAsync(It.IsAny<IEnumerable<string>>(), default))
            .ReturnsAsync(trustDtos);

        foreach (var projectsQueryModel in mockListAllProjectsForUserQueryModels)
        {
            var incomingTrustUkprn = trustDtos.OrderBy(_ => new Random().Next()).First().Ukprn;
            var outgoingTrustUkprn = trustDtos.OrderBy(_ => new Random().Next()).First().Ukprn;
            Assert.NotNull(projectsQueryModel.Project);
            Assert.NotNull(incomingTrustUkprn);
            Assert.NotNull(outgoingTrustUkprn);
            projectsQueryModel.Project.IncomingTrustUkprn = incomingTrustUkprn;
            projectsQueryModel.Project.OutgoingTrustUkprn = outgoingTrustUkprn;
        }

        var trustList = trustDtos.ToList();

        var expectedQuery = mockListAllProjectsForUserQueryModels.Select(item =>
        {
            Assert.NotNull(item.Project);
            var incomingTrustName = item.Project.FormAMat
                ? item.Project.NewTrustName
                : trustList.FirstOrDefault(t => t.Ukprn! == item.Project.IncomingTrustUkprn)?.Name;
            var outgoingTrustName = trustList.FirstOrDefault(t => t.Ukprn! == item.Project.OutgoingTrustUkprn)?.Name;
            return ListAllProjectsForUserQueryResultModel
                .MapProjectAndEstablishmentToListAllProjectsForUserQueryResultModel(
                    item.Project,
                    item.Establishment!,
                    outgoingTrustName,
                    incomingTrustName);
        });

        var expected = expectedQuery
            .Skip(20).Take(20).ToList();

        mockListAllProjectsQueryService.ListAllProjects(new ProjectFilters(ProjectState.Active, null,
                AssignedToUserId: filter == ProjectUserFilter.AssignedTo ? userDto.Id : null,
                CreatedByUserId: filter == ProjectUserFilter.CreatedBy ? userDto.Id : null),
                orderBy: Arg.Any<OrderProjectQueryBy>())
            .Returns(mockListAllProjectsForUserQueryModels.BuildMock());

        Assert.NotNull(userDto.EntraUserObjectId);

        var query = new ListAllProjectsForUserQuery(ProjectState.Active, userDto.EntraUserObjectId, filter,
            ordering)
        { Page = 1 };

        //Act
        var result = await handler.Handle(query, default);

        //Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);

        Assert.Equal(expected.Count, result.Value?.Count);
        for (var i = 0; i < result.Value!.Count; i++)
        {
            Assert.Equivalent(expected[i], result.Value![i]);
        }
    }

    [Theory]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.SignificantDate, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.SignificantDate, OrderByDirection.Descending)]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.CreatedAt, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.CreatedAt, OrderByDirection.Descending)]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.CompletedAt, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.CompletedAt, OrderByDirection.Descending)]
    public async Task Handle_ShouldReturnCorrectListForEmail_WhenPaginationIsCorrect(
        ProjectUserFilter filter,
        OrderProjectQueryBy ordering,
        [Frozen] IListAllProjectsQueryService mockListAllProjectsQueryService,
        [Frozen] Mock<ISender> mockSender,
        IFixture fixture,
        Mock<ILogger<ListAllProjectsForUserQueryHandler>> _mockLogger)
    {
        //Arrange 
        var mockTrustsClient = new Mock<ITrustsV4Client>();

        var handler = new ListAllProjectsForUserQueryHandler(mockListAllProjectsQueryService,
            mockTrustsClient.Object,
            mockSender.Object,
            _mockLogger.Object);

        var userDto = fixture.Create<UserDto>();

        mockSender.Setup(sender => sender.Send(It.IsAny<GetUserByEmailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto?>.Success(userDto));

        var mockListAllProjectsForUserQueryModels = fixture.CreateMany<ListAllProjectsQueryModel>(50).ToList();

        var trustDtos = new ObservableCollection<TrustDto>(
            fixture.Build<TrustDto>().With(dto => dto.Ukprn, new Ukprn(new Random().Next()).Value.ToString)
                .CreateMany(10));

        mockTrustsClient.Setup(service => service.GetByUkprnsAllAsync(It.IsAny<IEnumerable<string>>(), default))
            .ReturnsAsync(trustDtos);

        foreach (var projectsQueryModel in mockListAllProjectsForUserQueryModels)
        {
            var incomingTrustUkprn = trustDtos.OrderBy(_ => new Random().Next()).First().Ukprn;
            var outgoingTrustUkprn = trustDtos.OrderBy(_ => new Random().Next()).First().Ukprn;
            Assert.NotNull(projectsQueryModel.Project);
            Assert.NotNull(incomingTrustUkprn);
            Assert.NotNull(outgoingTrustUkprn);
            projectsQueryModel.Project.IncomingTrustUkprn = incomingTrustUkprn;
            projectsQueryModel.Project.OutgoingTrustUkprn = outgoingTrustUkprn;
        }

        var trustList = trustDtos.ToList();

        var expectedQuery = mockListAllProjectsForUserQueryModels.Select(item =>
        {
            Assert.NotNull(item.Project);
            var incomingTrustName = item.Project.FormAMat
                ? item.Project.NewTrustName
                : trustList.FirstOrDefault(t => t.Ukprn! == item.Project.IncomingTrustUkprn)?.Name;
            var outgoingTrustName = trustList.FirstOrDefault(t => t.Ukprn! == item.Project.OutgoingTrustUkprn)?.Name;
            return ListAllProjectsForUserQueryResultModel
                .MapProjectAndEstablishmentToListAllProjectsForUserQueryResultModel(
                    item.Project,
                    item.Establishment!,
                    outgoingTrustName,
                    incomingTrustName);
        });

        var expected = expectedQuery
            .Skip(20).Take(20).ToList();

        mockListAllProjectsQueryService.ListAllProjects(new ProjectFilters(ProjectState.Active, null,
                AssignedToUserId: filter == ProjectUserFilter.AssignedTo ? userDto.Id : null,
                CreatedByUserId: filter == ProjectUserFilter.CreatedBy ? userDto.Id : null),
                orderBy: Arg.Any<OrderProjectQueryBy>())
            .Returns(mockListAllProjectsForUserQueryModels.BuildMock());

        Assert.NotNull(userDto.EntraUserObjectId);

        var query = new ListAllProjectsForUserQuery(ProjectState.Active, null, filter,
            ordering, userDto.EntraUserObjectId)
        { Page = 1 };

        //Act
        var result = await handler.Handle(query, default);

        //Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);

        Assert.Equal(expected.Count, result.Value?.Count);
        for (var i = 0; i < result.Value!.Count; i++)
        {
            Assert.Equivalent(expected[i], result.Value![i]);
        }
    }

    [Theory]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.SignificantDate, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.SignificantDate, OrderByDirection.Descending)]
    public async Task Handle_ShouldReturnCorrectList_WhenAllPagesAreSkipped(
        ProjectUserFilter filter,
        OrderProjectQueryBy ordering,
        [Frozen] IListAllProjectsQueryService mockListAllProjectsQueryService,
        [Frozen] Mock<ISender> mockSender,
        IFixture fixture,
        Mock<ILogger<ListAllProjectsForUserQueryHandler>> _mockLogger)
    {
        //Arrange 
        var mockTrustsClient = new Mock<ITrustsV4Client>();

        var handler = new ListAllProjectsForUserQueryHandler(mockListAllProjectsQueryService, mockTrustsClient.Object,
            mockSender.Object, _mockLogger.Object);

        var userDto = fixture.Create<UserDto>();
        mockSender.Setup(sender => sender.Send(It.IsAny<GetUserByOidQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto?>.Success(userDto));

        var mockListAllProjectsForUserQueryModels = fixture.CreateMany<ListAllProjectsQueryModel>(50);

        var trustDtos = new ObservableCollection<TrustDto>(
            fixture.Build<TrustDto>().With(dto => dto.Ukprn, new Ukprn(new Random().Next()).Value.ToString)
                .CreateMany(10));

        mockTrustsClient.Setup(service => service.GetByUkprnsAllAsync(It.IsAny<IEnumerable<string>>(), default))
            .ReturnsAsync(trustDtos);

        foreach (var projectsQueryModel in mockListAllProjectsForUserQueryModels.ToList())
        {
            var incomingTrustUkprn = trustDtos.OrderBy(_ => new Random().Next()).First().Ukprn;
            var outgoingTrustUkprn = trustDtos.OrderBy(_ => new Random().Next()).First().Ukprn;
            Assert.NotNull(projectsQueryModel.Project);
            Assert.NotNull(incomingTrustUkprn);
            Assert.NotNull(outgoingTrustUkprn);
            projectsQueryModel.Project.IncomingTrustUkprn = incomingTrustUkprn;
            projectsQueryModel.Project.OutgoingTrustUkprn = outgoingTrustUkprn;
        }

        mockListAllProjectsQueryService.ListAllProjects(new ProjectFilters(ProjectState.Active, null,
                AssignedToUserId: filter == ProjectUserFilter.AssignedTo ? userDto.Id : null,
                CreatedByUserId: filter == ProjectUserFilter.CreatedBy ? userDto.Id : null),
                orderBy: Arg.Any<OrderProjectQueryBy>())
            .Returns(mockListAllProjectsForUserQueryModels.BuildMock());

        Assert.NotNull(userDto.EntraUserObjectId);
        var query = new ListAllProjectsForUserQuery(ProjectState.Active, userDto.EntraUserObjectId, filter,
            ordering)
        { Page = 50 };

        //Act
        var result = await handler.Handle(query, default);

        //Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value?.Count);
    }

    [Theory]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.SignificantDate, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.SignificantDate, OrderByDirection.Descending)]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.CreatedAt, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.CreatedAt, OrderByDirection.Descending)]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.CompletedAt, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.CompletedAt, OrderByDirection.Descending)]
    public async Task Handle_ShouldReturnUnsuccessful_WhenAnErrorOccurs(
        ProjectUserFilter filter,
        OrderProjectQueryBy ordering,
        [Frozen] IListAllProjectsQueryService mockListAllProjectsQueryService,
        [Frozen] Mock<ISender> mockSender,
        IFixture fixture,
        Mock<ILogger<ListAllProjectsForUserQueryHandler>> _mockLogger)
    {
        //Arrange 
        var mockTrustsClient = new Mock<ITrustsV4Client>();

        var handler = new ListAllProjectsForUserQueryHandler(mockListAllProjectsQueryService, mockTrustsClient.Object,
            mockSender.Object, _mockLogger.Object);

        const string errorMessage = "this is a test";

        var userDto = fixture.Create<UserDto>();
        mockSender.Setup(sender => sender.Send(It.IsAny<GetUserByOidQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errorMessage));
        Assert.NotNull(userDto.EntraUserObjectId);

        var query = new ListAllProjectsForUserQuery(ProjectState.Active, userDto.EntraUserObjectId, filter,
            ordering)
        { Page = 50 };

        //Act
        var result = await handler.Handle(query, default);

        //Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal(errorMessage, result.Error);
    }

    [Theory]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.SignificantDate, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.SignificantDate, OrderByDirection.Descending)]
    public async Task Handle_ShouldSetAssignedToOrCreatedByCorrectly(
     ProjectUserFilter filter,
     OrderProjectQueryBy ordering,
     [Frozen] Mock<IListAllProjectsQueryService> mockListAllProjectsQueryService,
     [Frozen] Mock<ISender> mockSender,
     IFixture fixture)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ListAllProjectsForUserQueryHandler>>();
        var mockTrustsClient = new Mock<ITrustsV4Client>();
        var handler = new ListAllProjectsForUserQueryHandler(
            mockListAllProjectsQueryService.Object,
            mockTrustsClient.Object,
            mockSender.Object,
            mockLogger.Object);

        var userDto = fixture.Create<UserDto>();
        mockSender.Setup(sender => sender.Send(It.IsAny<GetUserByOidQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto?>.Success(userDto));

        var dummyProjects = fixture.CreateMany<ListAllProjectsQueryModel>(3).ToList();
        ProjectFilters? capturedFilters = null;

        mockListAllProjectsQueryService
            .Setup(s => s.ListAllProjects(It.IsAny<ProjectFilters>(), It.IsAny<string>(), It.IsAny<OrderProjectQueryBy>()))
            .Callback<ProjectFilters, string, OrderProjectQueryBy?>((filters, _, __) => capturedFilters = filters)
            .Returns(dummyProjects.BuildMock());

        mockTrustsClient.Setup(service => service.GetByUkprnsAllAsync(It.IsAny<IEnumerable<string>>(), default))
            .ReturnsAsync([]);

        var query = new ListAllProjectsForUserQuery(ProjectState.Active, userDto.EntraUserObjectId!, filter, ordering) { Page = 0 };

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedFilters);

        if (filter == ProjectUserFilter.AssignedTo)
        {
            Assert.Equal(userDto.Id, capturedFilters.AssignedToUserId);
            Assert.Null(capturedFilters.CreatedByUserId);
        }
        else if (filter == ProjectUserFilter.CreatedBy)
        {
            Assert.Equal(userDto.Id, capturedFilters.CreatedByUserId);
            Assert.Null(capturedFilters.AssignedToUserId);
        }
    }

    [Theory]
    [InlineAutoData(ProjectUserFilter.AssignedTo, OrderProjectByField.SignificantDate, OrderByDirection.Ascending)]
    [InlineAutoData(ProjectUserFilter.CreatedBy, OrderProjectByField.SignificantDate, OrderByDirection.Descending)]
    public async Task Handle_ShouldReturnCorrectList_WhenGetByUkprnsAllAsyncIsNotCalled(
       ProjectUserFilter filter,
       OrderProjectQueryBy ordering,
       [Frozen] IListAllProjectsQueryService mockListAllProjectsQueryService,
       [Frozen] Mock<ISender> mockSender,
       IFixture fixture,
       Mock<ILogger<ListAllProjectsForUserQueryHandler>> _mockLogger)
    {
        //Arrange 
        var mockTrustsClient = new Mock<ITrustsV4Client>();

        var handler = new ListAllProjectsForUserQueryHandler(mockListAllProjectsQueryService, mockTrustsClient.Object,
            mockSender.Object, _mockLogger.Object);

        var userDto = fixture.Create<UserDto>();
        mockSender.Setup(sender => sender.Send(It.IsAny<GetUserByOidQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto?>.Success(userDto));

        var mockListAllProjectsForUserQueryModels = fixture.CreateMany<ListAllProjectsQueryModel>(0);

        mockListAllProjectsQueryService.ListAllProjects(new ProjectFilters(ProjectState.Active, null,
                AssignedToUserId: filter == ProjectUserFilter.AssignedTo ? userDto.Id : null,
                CreatedByUserId: filter == ProjectUserFilter.CreatedBy ? userDto.Id : null),
                orderBy: Arg.Any<OrderProjectQueryBy>())
            .Returns(mockListAllProjectsForUserQueryModels.BuildMock());

        Assert.NotNull(userDto.EntraUserObjectId);
        var query = new ListAllProjectsForUserQuery(ProjectState.Active, userDto.EntraUserObjectId, filter,
            ordering)
        { Page = 50 };

        //Act
        var result = await handler.Handle(query, default);

        //Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value?.Count);
        mockTrustsClient.Verify(service => service.GetByUkprnsAllAsync(It.IsAny<IEnumerable<string>>(), default), Times.Never);
    }
}