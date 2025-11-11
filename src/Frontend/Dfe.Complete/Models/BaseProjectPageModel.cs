using Dfe.AcademiesApi.Client.Contracts;
using Dfe.Complete.Application.DaoRevoked.Models;
using Dfe.Complete.Application.DaoRevoked.Queries;
using Dfe.Complete.Application.KeyContacts.Models;
using Dfe.Complete.Application.KeyContacts.Queries;
using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Application.Projects.Queries.GetConversionTasksData;
using Dfe.Complete.Application.Projects.Queries.GetProject;
using Dfe.Complete.Application.Projects.Queries.GetTransferTasksData;
using Dfe.Complete.Application.Services.AcademiesApi;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.Extensions;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Extensions;
using Dfe.Complete.Utils.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dfe.Complete.Models;

public abstract class BaseProjectPageModel(ISender sender, ILogger logger) : PageModel
{
    protected readonly ISender Sender = sender;
    protected ILogger Logger = logger;

    [BindProperty(SupportsGet = true, Name = "projectId")]
    public string ProjectId { get; set; }

    public ProjectDto Project { get; set; }

    public DaoRevocationDto? DaoRevocation { get; set; }

    public EstablishmentDto Establishment { get; set; }

    public TrustDto? IncomingTrust { get; set; }

    public TrustDto? OutgoingTrust { get; set; }

    public ProjectTeam CurrentUserTeam { get; set; }

    public TransferTaskDataDto TransferTaskData { get; private set; } = null!;
    public ConversionTaskDataDto ConversionTaskData { get; private set; } = null!;
    public KeyContactDto KeyContacts { get; private set; } = null!;

    public bool UserHasEditAccess() =>
        User.GetUserId() == Project.AssignedToId || CurrentUserTeam.TeamIsServiceSupport();

    public async Task UpdateCurrentProject()
    {
        var success = Guid.TryParse(ProjectId, out var guid);

        if (!success)
        {
            Logger.LogWarning("{ProjectId} is not a valid Guid.", ProjectId);
            return;
        }

        var query = new GetProjectByIdQuery(new ProjectId(guid));
        var result = await Sender.Send(query);
        if (!result.IsSuccess || result.Value == null)
        {
            Logger.LogWarning("Project {ProjectId} does not exist.", ProjectId);
            return;
        }

        Project = result.Value;
    }

    protected async Task SetEstablishmentAsync()
    {
        var establishmentQuery = new GetEstablishmentByUrnRequest(Project.Urn.Value.ToString());
        var establishmentResult = await Sender.Send(establishmentQuery);

        if (!establishmentResult.IsSuccess || establishmentResult.Value == null)
        {
            throw new NotFoundException($"Establishment {Project.Urn.Value} does not exist.");
        }

        Establishment = establishmentResult.Value;
    }
    protected async Task SetDaoRevocationIfProjectIsDaoRevoked()
    {
        if (Project.State is not ProjectState.DaoRevoked)
        {
            return;
        }
        var daoRevocationQuery = new GetDaoRevocationByProjectIdQuery(Project.Id);
        var daoRevocationResult = await Sender.Send(daoRevocationQuery);

        if (!daoRevocationResult.IsSuccess || daoRevocationResult.Value == null)
        {
            throw new NotFoundException($"Dao revocation for project {Project.Id.Value} does not exist.");
        }

        DaoRevocation = daoRevocationResult.Value;
    }
    protected async Task SetIncomingTrustAsync()
    {
        if (!Project.FormAMat && Project.IncomingTrustUkprn != null)
        {
            var incomingTrustQuery = new GetTrustByUkprnRequest(Project.IncomingTrustUkprn.Value.ToString());
            var incomingTrustResult = await Sender.Send(incomingTrustQuery);

            if (!incomingTrustResult.IsSuccess || incomingTrustResult.Value == null)
            {
                throw new NotFoundException($"Trust {Project.IncomingTrustUkprn.Value} does not exist.");
            }

            IncomingTrust = incomingTrustResult.Value;
        }
    }

    protected async Task SetOutgoingTrustAsync()
    {
        if (Project.Type == ProjectType.Transfer && Project.OutgoingTrustUkprn != null)
        {
            var outgoingtrustQuery = new GetTrustByUkprnRequest(Project.OutgoingTrustUkprn.Value.ToString());
            var outgoingTrustResult = await Sender.Send(outgoingtrustQuery);

            if (!outgoingTrustResult.IsSuccess || outgoingTrustResult.Value == null)
            {
                throw new NotFoundException($"Trust {Project.OutgoingTrustUkprn.Value} does not exist.");
            }

            OutgoingTrust = outgoingTrustResult.Value;
        }
    }

    protected async Task SetCurrentUserTeamAsync()
    {
        CurrentUserTeam = await User.GetUserTeam(Sender);
    }

    public virtual async Task<IActionResult> OnGetAsync()
    {
        await UpdateCurrentProject();

        if (Project == null)
            return NotFound();

        await SetEstablishmentAsync();

        await SetIncomingTrustAsync();

        await SetOutgoingTrustAsync();

        await SetCurrentUserTeamAsync();

        await SetDaoRevocationIfProjectIsDaoRevoked();

        return Page();
    }

    public string FormatRouteWithProjectId(string route) => string.Format(route, ProjectId);

    protected async Task GetProjectTaskDataAsync()
    {
        if (Project.TasksDataId != null)
        {
            if (Project.Type == ProjectType.Transfer)
            {
                var result = await Sender.Send(new GetTransferTasksDataByIdQuery(Project.TasksDataId));
                if (result.IsSuccess && result.Value != null)
                {
                    TransferTaskData = result.Value;
                }
            }
            if (Project.Type == ProjectType.Conversion)
            {
                var result = await Sender.Send(new GetConversionTasksDataByIdQuery(Project.TasksDataId));
                if (result.IsSuccess && result.Value != null)
                {
                    ConversionTaskData = result.Value;
                }
            }
        }
    }
    protected async Task GetKeyContactForProjectsAsync()
    {
        var contactsResult = await Sender.Send(new GetKeyContactsForProjectQuery(new ProjectId(Guid.Parse(ProjectId))));
        if (contactsResult.Value == null)
        {
            throw new NotFoundException($"Key contacts for project {ProjectId} doesn't exist");
        }

        KeyContacts = contactsResult.Value;
    }
}