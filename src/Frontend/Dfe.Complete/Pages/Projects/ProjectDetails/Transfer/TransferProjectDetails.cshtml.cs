using Dfe.Complete.Application.Projects.Commands.UpdateProject;
using Dfe.Complete.Application.Projects.Queries.GetTransferTasksData;
using Dfe.Complete.Application.Users.Queries.GetUser;
using Dfe.Complete.Constants;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Extensions;
using Dfe.Complete.Models;
using Dfe.Complete.Services.Interfaces;
using Dfe.Complete.Utils.Exceptions;
using Dfe.Complete.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace Dfe.Complete.Pages.Projects.ProjectDetails.Transfer
{
    public class TransferProjectDetailsModel(ISender sender, IErrorService errorService, ILogger<TransferProjectDetailsModel> _logger) : BaseProjectDetailsPageModel(sender, errorService, _logger)
    {
        [BindProperty]
        [GovukRequired]
        [Ukprn]
        [Required(ErrorMessage = "Enter an outgoing trust UKPRN")]
        [DisplayName("outgoing trust UKPRN")]
        public string? OutgoingTrustUkprn { get; set; }

        [BindProperty]
        [SharePointLink]
        [Required(ErrorMessage = "Enter an outgoing trust SharePoint link")]
        [Display(Name = "Outgoing trust SharePoint link")]
        public string? OutgoingTrustSharepointLink { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "State if the transfer is due to an inadequate Ofsted rating. Choose yes or no")]
        [Display(Name = "Inadequate OfstedRating")]
        public bool? InadequateOfsted { get; set; }

        [BindProperty]
        [Required(ErrorMessage =
            "State if the transfer is due to financial, safeguarding or governance issues. Choose yes or no")]
        [Display(Name = "Issues")]
        public bool? FinancialSafeguardingGovernanceIssues { get; set; }

        [BindProperty]
        [Required(ErrorMessage =
            "State if the outgoing trust will close once this transfer is completed. Choose yes or no")]
        [Display(Name = "Will outgoing trust close")]
        public bool? OutgoingTrustToClose { get; set; }

        private async Task SetTransferTaskDataAsync()
        {
            if (Project.TasksDataId != null)
            {
                var transferTasksDataQuery = new GetTransferTasksDataByIdQuery(Project.TasksDataId);
                var transferTasksData = await Sender.Send(transferTasksDataQuery);
                if (transferTasksData.IsSuccess || transferTasksData.Value != null)
                {
                    var tasksData = transferTasksData.Value!;

                    OutgoingTrustToClose = tasksData.OutgoingTrustToClose ?? false;
                    FinancialSafeguardingGovernanceIssues = tasksData.FinancialSafeguardingGovernanceIssues ?? false;
                    InadequateOfsted = tasksData.InadequateOfsted ?? false;
                }
            }
        }

        public override async Task<IActionResult> OnGetAsync()
        {
            var baseResult = await base.OnGetAsync();
            if (baseResult is not PageResult) return baseResult;

            if (Project.Type != Domain.Enums.ProjectType.Transfer)
                throw new NotFoundException($"Project {ProjectId} is not a transfer project.");

            if (!UserHasEditAccess())
            {
                TempData.SetNotification(
                    NotificationType.Error,
                    "Important",
                    "You are not authorised to perform this action."
                );

                return Redirect(string.Format(RouteConstants.ProjectAbout, ProjectId));
            }

            EstablishmentName = Establishment?.Name;

            OutgoingTrustUkprn = Project.OutgoingTrustUkprn?.ToString();
            IncomingTrustUkprn = Project.IncomingTrustUkprn?.ToString();
            NewTrustReferenceNumber = Project.NewTrustReferenceNumber;

            await SetGroupReferenceNumberAsync();

            AdvisoryBoardDate = Project.AdvisoryBoardDate?.ToDateTime(default);
            AdvisoryBoardConditions = Project.AdvisoryBoardConditions;
            EstablishmentSharepointLink = HttpUtility.UrlDecode(Project.EstablishmentSharepointLink);
            IncomingTrustSharepointLink = HttpUtility.UrlDecode(Project.IncomingTrustSharepointLink);
            OutgoingTrustSharepointLink = HttpUtility.UrlDecode(Project.OutgoingTrustSharepointLink);

            TwoRequiresImprovement = Project.TwoRequiresImprovement ?? false;

            await SetTransferTaskDataAsync();

            IsHandingToRCS = Project.Team == Domain.Enums.ProjectTeam.RegionalCaseWorkerServices;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ErrorService.AddErrors(ModelState);
                return Page();
            }

            var user = await Sender.Send(new GetUserByOidQuery(User.GetUserOid()), cancellationToken);

            if (user is not { IsSuccess: true })
                throw new NotFoundException("No user found.", innerException: new Exception(user?.Error));

            var updateProjectCommand = new UpdateTransferProjectCommand(
                ProjectId: new ProjectId(Guid.Parse(ProjectId)),
                IncomingTrustUkprn: new Ukprn(IncomingTrustUkprn!.ToInt()),
                OutgoingTrustUkprn: new Ukprn(OutgoingTrustUkprn!.ToInt()),
                NewTrustReferenceNumber: NewTrustReferenceNumber,
                GroupReferenceNumber: GroupReferenceNumber,
                AdvisoryBoardDate: AdvisoryBoardDate.HasValue
                    ? DateOnly.FromDateTime(AdvisoryBoardDate.Value)
                    : default,
                AdvisoryBoardConditions: AdvisoryBoardConditions ?? string.Empty,
                EstablishmentSharepointLink: EstablishmentSharepointLink ?? string.Empty,
                IncomingTrustSharepointLink: IncomingTrustSharepointLink ?? string.Empty,
                OutgoingTrustSharepointLink: OutgoingTrustSharepointLink ?? string.Empty,
                TwoRequiresImprovement: TwoRequiresImprovement ?? false,
                InadequateOfsted: InadequateOfsted ?? false,
                FinancialSafeguardingGovernanceIssues: FinancialSafeguardingGovernanceIssues ?? false,
                OutgoingTrustToClose: OutgoingTrustToClose ?? false,
                IsHandingToRCS: IsHandingToRCS ?? false,
                User: user.Value!
            );

            await Sender.Send(updateProjectCommand, cancellationToken);

            TempData.SetNotification(
                NotificationType.Success,
                "Success",
                "Project has been updated successfully");

            return Redirect(string.Format(RouteConstants.ProjectAbout, ProjectId));
        }
    }
}
