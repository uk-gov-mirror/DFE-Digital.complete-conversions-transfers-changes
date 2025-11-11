using Dfe.Complete.Models;
using Dfe.Complete.Pages.Projects.ProjectView;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dfe.Complete.Pages.Projects.TaskList
{
    public class TaskListModel(ISender sender, ILogger<TaskListModel> _logger) : ProjectLayoutModel(sender, _logger, TaskListNavigation)
    {
        public TransferTaskListViewModel TransferTaskList { get; set; } = null!;
        public ConversionTaskListViewModel ConversionTaskList { get; set; } = null!;

        public override async Task<IActionResult> OnGetAsync()
        {
            await UpdateCurrentProject();
            await SetEstablishmentAsync();
            await GetProjectTaskDataAsync();
            await SetIncomingTrustAsync();
            await SetOutgoingTrustAsync();
            await GetKeyContactForProjectsAsync();
            await SetCurrentUserTeamAsync();
            await SetDaoRevocationIfProjectIsDaoRevoked();

            TransferTaskList = TransferTaskListViewModel.Create(TransferTaskData, Project, KeyContacts);
            ConversionTaskList = ConversionTaskListViewModel.Create(ConversionTaskData, Project, KeyContacts);
            return Page();
        }
    }
}
