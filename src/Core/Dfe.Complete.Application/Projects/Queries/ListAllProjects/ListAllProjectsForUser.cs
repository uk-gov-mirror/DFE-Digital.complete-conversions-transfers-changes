using Dfe.AcademiesApi.Client.Contracts;
using Dfe.Complete.Application.Common.Models;
using Dfe.Complete.Application.Projects.Interfaces;
using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Application.Users.Queries.GetUser;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Utils.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dfe.Complete.Application.Projects.Queries.ListAllProjects;

public record ListAllProjectsForUserQuery(
    ProjectState? State,
    string? UserAdId,
    ProjectUserFilter ProjectUserFilter,
    OrderProjectQueryBy? OrderProjectQueryBy,
    string? UserEmail = null)
    : PaginatedRequest<PaginatedResult<List<ListAllProjectsForUserQueryResultModel>>>;

public class ListAllProjectsForUserQueryHandler(
    IListAllProjectsQueryService listAllProjectsQueryService,
    ITrustsV4Client trustsClient,
    ISender sender,
    ILogger<ListAllProjectsForUserQueryHandler> logger)
    : IRequestHandler<ListAllProjectsForUserQuery, PaginatedResult<List<ListAllProjectsForUserQueryResultModel>>>
{
    public async Task<PaginatedResult<List<ListAllProjectsForUserQueryResultModel>>> Handle(
       ListAllProjectsForUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            Result<UserDto?>? user = null;
            if (!string.IsNullOrEmpty(request.UserAdId))
            {
                user = await sender.Send(new GetUserByOidQuery(request.UserAdId), cancellationToken);
            }

            if (!string.IsNullOrEmpty(request.UserEmail))
            {
                user = await sender.Send(new GetUserByEmailQuery(request.UserEmail), cancellationToken);
            }

            if (user == null || !user.IsSuccess || user.Value == null)
                throw new NotFoundException("User not found.");

            var assignedTo = request.ProjectUserFilter == ProjectUserFilter.AssignedTo ? user.Value?.Id : null;
            var createdBy = request.ProjectUserFilter == ProjectUserFilter.CreatedBy ? user.Value?.Id : null;
            var projectsForUser = await listAllProjectsQueryService
                .ListAllProjects(new ProjectFilters(request.State, null, AssignedToUserId: assignedTo, CreatedByUserId: createdBy),
                    orderBy: request.OrderProjectQueryBy)
                .ToListAsync(cancellationToken);

            var allProjectTrustUkPrns = projectsForUser
                .SelectMany(p => new[]
                {
                   p.Project?.IncomingTrustUkprn?.Value.ToString() ?? string.Empty,
                   p.Project?.OutgoingTrustUkprn?.Value.ToString() ?? string.Empty
                })
                .Where(ukPrn => !string.IsNullOrEmpty(ukPrn))
                .Distinct()
                .ToList();

            if (allProjectTrustUkPrns.Count == 0)
            {
                return PaginatedResult<List<ListAllProjectsForUserQueryResultModel>>.Success([], projectsForUser.Count);
            }
            var allTrusts = await trustsClient.GetByUkprnsAllAsync(allProjectTrustUkPrns, cancellationToken);

            var result = projectsForUser
                .Skip(request.Page * request.Count)
                .Take(request.Count)
                .Select(p =>
                {
                    var incomingTrustName = p.Project.FormAMat ? p.Project.NewTrustName : allTrusts?.FirstOrDefault(trust => trust.Ukprn == p.Project?.IncomingTrustUkprn?.Value.ToString())?.Name;
                    return ListAllProjectsForUserQueryResultModel
                        .MapProjectAndEstablishmentToListAllProjectsForUserQueryResultModel(
                            p.Project,
                            p.Establishment!,
                            outgoingTrustName: allTrusts?.FirstOrDefault(trust =>
                                trust.Ukprn == p.Project?.OutgoingTrustUkprn?.Value.ToString())?.Name,
                            incomingTrustName: incomingTrustName);
                })
                .ToList();

            return PaginatedResult<List<ListAllProjectsForUserQueryResultModel>>.Success(result, projectsForUser.Count);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception for {Name} Request - {@Request}", nameof(ListAllProjectsForUserQueryHandler),
                request);
            return PaginatedResult<List<ListAllProjectsForUserQueryResultModel>>.Failure(e.Message);
        }
    }
}