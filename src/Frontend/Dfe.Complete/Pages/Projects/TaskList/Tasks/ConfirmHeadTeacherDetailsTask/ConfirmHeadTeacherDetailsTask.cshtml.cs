using Dfe.Complete.Application.Contacts.Models;
using Dfe.Complete.Application.Contacts.Queries;
using Dfe.Complete.Application.KeyContacts.Commands;
using Dfe.Complete.Constants;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dfe.Complete.Pages.Projects.TaskList.Tasks.ConfirmHeadTeacherDetailsTask
{
    public class ConfirmHeadTeachersDetailsTaskModel(ISender sender, IAuthorizationService authorizationService, ILogger<ConfirmHeadTeachersDetailsTaskModel> logger)
        : BaseProjectTaskModel(sender, authorizationService, logger, NoteTaskIdentifier.ConfirmHeadTeacherDetails)
    {
        [BindProperty]
        public Guid? HeadTeacherContactId { get; set; }

        [BindProperty]
        public Guid? KeyContactId { get; set; }

        public List<ContactDto>? Contacts { get; set; }

        public override async Task<IActionResult> OnGetAsync()
        {
            await base.OnGetAsync();
            await GetKeyContactForProjectsAsync();

            var contacts = await Sender.Send(new GetContactsForProjectByCategoryQuery(Project.Id, ContactCategory.SchoolOrAcademy));

            HeadTeacherContactId = KeyContacts.HeadteacherId?.Value;
            KeyContactId = KeyContacts.Id?.Value;

            Contacts = contacts?.Value ?? [];

            return Page();
        }
        public async Task<IActionResult> OnPost()
        {
            await Sender.Send(new UpdateHeadTeacherCommand(new KeyContactId(KeyContactId.GetValueOrDefault()), new ContactId(HeadTeacherContactId.GetValueOrDefault())));
            SetTaskSuccessNotification();
            return Redirect(string.Format(RouteConstants.ProjectTaskList, ProjectId));
        }
    }
}
