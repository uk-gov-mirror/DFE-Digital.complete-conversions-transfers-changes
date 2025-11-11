using Dfe.Complete.Application.Projects.Commands.UpdateProject;
using Dfe.Complete.Application.Users.Queries.GetUser;
using Dfe.Complete.Constants;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Extensions;
using Dfe.Complete.Models;
using Dfe.Complete.Services.Interfaces;
using Dfe.Complete.Utils.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Dfe.Complete.Pages.Projects.ProjectDetails.Conversion
{
    public class ConversionProjectDetailsModel(ISender sender, IErrorService errorService, ILogger<ConversionProjectDetailsModel> _logger) : BaseProjectDetailsPageModel(sender, errorService, _logger)
    {
        [BindProperty]
        [Required(ErrorMessage =
            "Select directive academy order or academy order, whichever has been used for this conversion")]
        [Display(Name = "Directive Academy Order")]
        public bool? DirectiveAcademyOrder { get; set; }

        public override async Task<IActionResult> OnGetAsync()
        {
            var baseResult = await base.OnGetAsync();
            if (baseResult is not PageResult) return baseResult;

            if (Project.Type != Domain.Enums.ProjectType.Conversion)
                throw new NotFoundException($"Project {ProjectId} is not a conversion project.");

            if (!UserHasEditAccess())
            {
                TempData.SetNotification(
                    NotificationType.Error,
                    "Important",
                    "You are not authorised to perform this action."
                );

                return Redirect(string.Format(RouteConstants.ProjectAbout, ProjectId));
            }

            DirectiveAcademyOrder = Project.DirectiveAcademyOrder ?? false;

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


            var updateProjectCommand = new UpdateConversionProjectCommand(
                ProjectId: new ProjectId(Guid.Parse(ProjectId)),
                IncomingTrustUkprn: new Ukprn(IncomingTrustUkprn!.ToInt()),
                NewTrustReferenceNumber: NewTrustReferenceNumber,
                GroupReferenceNumber: GroupReferenceNumber,
                AdvisoryBoardDate: AdvisoryBoardDate.HasValue
                    ? DateOnly.FromDateTime(AdvisoryBoardDate.Value)
                    : default,
                AdvisoryBoardConditions: AdvisoryBoardConditions ?? string.Empty,
                EstablishmentSharepointLink: EstablishmentSharepointLink ?? string.Empty,
                IncomingTrustSharepointLink: IncomingTrustSharepointLink ?? string.Empty,
                IsHandingToRCS: IsHandingToRCS ?? false,
                DirectiveAcademyOrder: DirectiveAcademyOrder ?? false,
                TwoRequiresImprovement: TwoRequiresImprovement ?? false,
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
