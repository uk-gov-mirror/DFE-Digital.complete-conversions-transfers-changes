using AutoFixture;
using AutoFixture.Xunit2;
using Dfe.Complete.Application.Projects.Interfaces;
using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Application.Projects.Queries.ListAllProjects;
using Dfe.Complete.Tests.Common.Customizations.Models;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations;
using MockQueryable;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dfe.Complete.Application.Tests.QueryHandlers.Project
{
    public class ListEstablishmentsInMatQueryHandlerTests
    {
        [Theory]
        [CustomAutoData(
            typeof(OmitCircularReferenceCustomization),
            typeof(ListAllProjectsQueryModelCustomization),
            typeof(DateOnlyCustomization))]
        public async Task Handle_ShouldReturnMatchingProjects_WhenProjectsWithReferenceNumberExist(
            [Frozen] IListAllProjectsQueryService listAllProjectsQueryService,
            ListEstablishmentsInMatQueryHandler handler,
            IFixture fixture)
        {
            // Arrange
            var referenceNumber = "TR123";
            var trustName = "Test MAT 123";

            var matchingProjects = fixture
                .Build<ListAllProjectsQueryModel>()
                .CreateMany(3)
                .Select(p =>
                {
                    p.Project.NewTrustReferenceNumber = referenceNumber;
                    p.Project.NewTrustName = trustName;
                    p.Project.IncomingTrustUkprn = null;
                    return p;
                })
                .ToList();

            var mockProjects = matchingProjects.BuildMock();

            listAllProjectsQueryService
                .ListAllProjects(new ProjectFilters(null, null, NewTrustReferenceNumber: referenceNumber))
                .Returns(mockProjects);

            var query = new ListEstablishmentsInMatQuery(referenceNumber);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(referenceNumber, result.Value.Identifier);
            Assert.Equal(trustName, result.Value.TrustName);
            Assert.Equal(matchingProjects.Count, result.Value.ProjectModels.Count());
            Assert.All(result.Value.ProjectModels, p => Assert.Equal(referenceNumber, p.NewTrustReferenceNumber));
        }

        [Theory]
        [CustomAutoData(
            typeof(OmitCircularReferenceCustomization),
            typeof(ListAllProjectsQueryModelCustomization),
            typeof(DateOnlyCustomization))]
        public async Task Handle_ShouldReturnFailure_WhenNoProjectsWithReferenceNumberExist(
            [Frozen] IListAllProjectsQueryService listAllProjectsQueryService,
            ListEstablishmentsInMatQueryHandler handler)
        {
            // Arrange
            var referenceNumber = "TR999";
            var emptyList = new List<ListAllProjectsQueryModel>().BuildMock();

            listAllProjectsQueryService
                .ListAllProjects(new ProjectFilters(null, null, NewTrustReferenceNumber: referenceNumber))
                .Returns(emptyList);

            var query = new ListEstablishmentsInMatQuery(referenceNumber);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("No projects found", result.Error);
        }

        [Theory]
        [CustomAutoData]
        public async Task Handle_ShouldReturnFailure_WhenExceptionIsThrown(
            [Frozen] IListAllProjectsQueryService listAllProjectsQueryService,
            ListEstablishmentsInMatQueryHandler handler)
        {
            // Arrange
            var expectedError = "Test failure";
            var query = new ListEstablishmentsInMatQuery("TR123");

            listAllProjectsQueryService
                .ListAllProjects(new ProjectFilters(null, null, NewTrustReferenceNumber: "TR123"))
                .Throws(new Exception(expectedError));

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedError, result.Error);
        }
    }
}
