using Dfe.Complete.Application.Projects.Commands.TaskData;
using Dfe.Complete.Constants;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dfe.Complete.Pages.Projects.TaskList.Tasks.ClosureOrTransferDeclarationTask
{
    public class ClosureOrTransferDeclarationModel(ISender sender, IAuthorizationService authorizationService, ILogger<ClosureOrTransferDeclarationModel> logger)
    : BaseProjectTaskModel(sender, authorizationService, logger, NoteTaskIdentifier.ClosureOrTransferDeclaration)
    {
        [BindProperty]
        public Guid? TasksDataId { get; set; }

        [BindProperty(Name = "notapplicable")]
        public bool? NotApplicable { get; set; }

        [BindProperty(Name = "received")]
        public bool? Received { get; set; }

        [BindProperty(Name = "cleared")]
        public bool? Cleared { get; set; }

        [BindProperty(Name = "saved")]
        public bool? Saved { get; set; }

        [BindProperty(Name = "sent")]
        public bool? Sent { get; set; }


        public override async Task<IActionResult> OnGetAsync()
        {
            await base.OnGetAsync();

            TasksDataId = Project.TasksDataId?.Value;

            NotApplicable = TransferTaskData.ClosureOrTransferDeclarationNotApplicable;
            Received = TransferTaskData.ClosureOrTransferDeclarationReceived;
            Cleared = TransferTaskData.ClosureOrTransferDeclarationCleared;
            Saved = TransferTaskData.ClosureOrTransferDeclarationSaved;
            Sent = TransferTaskData.ClosureOrTransferDeclarationSent;

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            await Sender.Send(new UpdateClosureOrTransferDeclarationTaskCommand(new TaskDataId(TasksDataId.GetValueOrDefault())!, NotApplicable, Received, Cleared, Saved, Sent));
            SetTaskSuccessNotification();
            return Redirect(string.Format(RouteConstants.ProjectTaskList, ProjectId));
        }
    }
}
