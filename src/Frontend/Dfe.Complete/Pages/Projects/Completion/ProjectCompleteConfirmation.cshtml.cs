using Dfe.Complete.Application.Projects.Commands.UpdateProject;
using Dfe.Complete.Constants;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Extensions;
using Dfe.Complete.Models;
using Dfe.Complete.Services.Project;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dfe.Complete.Pages.Projects.Completion;

public class CompleteProjectModel(ISender sender, IProjectService projectService, ILogger<CompleteProjectModel> logger)
: BaseProjectPageModel(sender, logger)
{
    private readonly IProjectService projectService = projectService;

    public async override Task<IActionResult> OnGetAsync()
    {
        if (TempData.ContainsKey("ShowProjectCompleteConfirmation"))
        {
            TempData.Remove("ShowProjectCompleteConfirmation");
            await UpdateCurrentProject();
            return Page();
        }

        return RedirectToProjectPage();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await UpdateCurrentProject();
        await SetCurrentUserTeamAsync();

        if (!UserHasEditAccess())
        {
            TempData.SetNotification(
                NotificationType.Error,
                "Important",
                "You are not authorised to perform this action."
            );

            return Redirect(string.Format(RouteConstants.ProjectAbout, ProjectId));
        }

        await UpdateCurrentProject();
        await GetProjectTaskDataAsync();
        await GetKeyContactForProjectsAsync();

        if (Project.Type == ProjectType.Transfer)
            return await CompleteTransferProjectAsync();

        return await CompleteConversionProjectAsync();
    }

    private async Task<IActionResult> CompleteTransferProjectAsync()
    {
        var taskList = TransferTaskListViewModel.Create(TransferTaskData, Project, KeyContacts);

        if (taskList == null)
        {
            return RedirectToProjectTaskList();
        }

        var validationResult = projectService.GetTransferProjectCompletionValidationResult(Project.SignificantDate, Project.SignificantDateProvisional ?? true, taskList);
        return await HandleProjectCompletionAsync(validationResult);
    }

    private async Task<IActionResult> CompleteConversionProjectAsync()
    {
        var taskList = ConversionTaskListViewModel.Create(ConversionTaskData, Project, KeyContacts);
        var validationResult = projectService.GetConversionProjectCompletionValidationResult(Project.SignificantDate, Project.SignificantDateProvisional ?? true, taskList);

        return await HandleProjectCompletionAsync(validationResult);
    }

    private async Task<IActionResult> HandleProjectCompletionAsync(List<string> validationErrors)
    {
        if (validationErrors.Any())
        {
            StoreValidationErrors(validationErrors);
            return RedirectToProjectTaskListWithValidation();
        }

        await MarkProjectAsCompletedAsync();
        return RedirectToProjectCompletePage();
    }

    private async Task MarkProjectAsCompletedAsync()
    {
        var command = new UpdateProjectCompletedCommand(
            ProjectId: new ProjectId(Guid.Parse(ProjectId))
        );

        await Sender.Send(command);
    }

    private void StoreValidationErrors(List<string> validationErrors)
    {
        TempData.Put("CompleteProjectValidationMessages", validationErrors);
    }

    private IActionResult RedirectToProjectTaskList()
    {
        var url = string.Format(RouteConstants.ProjectTaskList, ProjectId);
        return Redirect(url);
    }

    private IActionResult RedirectToProjectTaskListWithValidation()
    {
        var url = string.Format(RouteConstants.ProjectTaskList, ProjectId) + "?projectCompletionValidation=true";
        return Redirect(url);
    }
    private IActionResult RedirectToProjectCompletePage()
    {
        var url = string.Format(RouteConstants.ProjectComplete, ProjectId);
        TempData.Add("ShowProjectCompleteConfirmation", true);
        return Redirect(url);
    }
    private IActionResult RedirectToProjectPage()
    {
        var url = string.Format(RouteConstants.Project, ProjectId);
        return Redirect(url);
    }
}



