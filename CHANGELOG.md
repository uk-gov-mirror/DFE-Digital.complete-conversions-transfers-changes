# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/). To see an example from a mature product in the program [see the Complete products changelog that follows the same methodology](https://github.com/DFE-Digital/dfe-complete-conversions-transfers-and-changes/blob/main/CHANGELOG.md).

### Statuses
Added for new features.  
Changed for changes in existing functionality.  
Fixed for any bug fixes.  
Removed for now removed features.  
Deprecated for soon-to-be removed features.  
Security in case of vulnerabilities.  

---

## Unreleased

### Added
- Added MAT Transfer creation end point for prepare to complete 

### Changed
- Throw exception when key_contacts record is missing on contact update tasks

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-11-06.1066...main) for everything awaiting release

---

## [1.27.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-11-06.1066) - 2025-11-06

### Added
- Added project group creation end point
- Added MAT Conversion creation end point for prepare to complete 
- Added `Check and clear Form M` task for transfer projects.
- Added `Closure or transfer declraration` task for transfer projects.
- Added `125 year lease` task for conversion project.
- Added `Confirm the incoming trust has completed all actions` task for transfer projects.

### Changed
- Remove buttons and links to external contacts for users without access

### Removed
- create conversion project end point deleted due to in-app project creations being a workaround 
- create MAT conversion project end point deleted due to in-app project creations being a workaround 
- create transfer project end point deleted due to in-app project creations being a workaround 
- create MAT transfer project end point deleted due to in-app project creations being a workaround 

### Fixed
- Fixed assign project return url issue.

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-31.1038...production-2025-11-06.1066) for everything in the release

---

## [1.26.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-31.1038) - 2025-10-31

### Added
- Added `Confirm the chair of governors' details` task for conversion projects.
- Added `Request a new URN and record for the academy` task for transfer projects.
- Added `Trust modification order task` task for conversion projects.
- Added `Share the information about the opening` task for conversion project.
- Added `Subleases` task
- Added `Tenancy at will` task for conversion project.
- Added `Outgoing Trust CEO contact` task page


### Changed
- Removed validation for academy transfer date and updated the title for transfer projects.

### Fixed

### Security
- Secure cookies should contain the HTTPOnly attribute

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-29.1020...production-2025-10-31.1038) for everything in the release

---

## [1.25.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-29.1020) - 2025-10-29

### Added

- Added `Deed of termination for the master funding agreement` task for transfer project.
- Added `Deed Of Termination For the Church Supplemental Agreement` task for transfer project.
- Added `Delete project` functionality.

### Changed

### Fixed

- Fixed `Significant date` validation issue of `Stakeholder kick-off` task.
- Fixed `no sequence element` issue on `Stakeholder kick-off` task.

### Security

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-27.1000...production-2025-10-29.1020) for everything in the release

---

## [1.24.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-27.1000) - 2025-10-27

### Added

- Added `Complete a notification of changes to funded high needs places form` task for conversion projects.
- Added 'Caching to redis api call'
- Added transfer creation end point for prepare to complete
- Added `Confirm if the bank details for the general annual grant payment need to change` task for transfer project.
- Added  'Confirm the academy name' task for conversion project.
- Added 'Land consent letter task for transfer projects'
- Adding key contact record on confirming project handover and logging error message if key contact is already been added.
- Added `Check accuracy of high needs places information` task for conversion project.

### Fixed

- minor text corrections for 2 of the task pages
- minor text correction for confirm dao revocation page
- Fixed task notes formatting.
- Replaced html extension method with note html tag to have consistent approach for all notes/reasoning  formatting.
- Fixed `LocalAuthority` HTTP Post method by removing local authority id.

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-21.954...production-2025-10-27.1000) for everything in the release

---

## [1.23.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-21.954) - 2025-10-21

### Added
- added <note-body> NoteBodyTagHelper to preserve formatting in text
- Added View External Contact - Show MP

### Changed
- note will now respect limited formatting

### Fixed
- resolve entity tracking issue on note write repository

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-20.943...production-2025-10-21.954) for everything in the release

---

## [1.22.3](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-20.943) - 2025-10-20

### Added

- Added `Check and confirm the academy and trust financial information` task for transfer project.
- Added link for delete project button

### Security

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-17.921...production-2025-10-20.943) for everything in the release

---

## [1.22.2](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-17.921) - 2025-10-17

### Added
- Added `Confirm the headteacher's details` task for both conversion and transfer projects.

### Fixed
- make internal email check case insensitive

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-16.912...production-2025-10-17.921) for everything in the release

---

## [1.22.1](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-16.912) - 2025-10-16

### Changed
- removed checkbox with no corresponding database field

### Fixed
- resolve bad field mappings for master funding agreements on conversion projects

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-15.903...production-2025-10-16.912) for everything in the release

---

## [1.22.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-15.903) - 2025-10-15

### Added
- Added `Confirm the academy's risk protection agreements` task for both conversion and transfer projects.
- Added all task notes identifiers.

### Changed
- Hide "Complete a project" functionality on a project if it's DaO revoked

### Fixed
- Fixed `full date` issue on the project complete notification. 

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-14.892...production-2025-10-15.903) for everything in the release

---

## [1.21.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-14.892) - 2025-10-14

### Added

- Add new prepare to complete end point for adding conversion projects
- Land registry title plans` task for conversion project.
- Added `Master Funding Agreement` task for both conversion and transfer projects.
- Add API end point for editing user
- Add ability to edit a user
- Added `Incoming Trust CEO contact` task page

### Changed
- Allow users to modify `declaration of expenditure certificate date` on the `Receive declaration of expenditure certificate` task for both conversion and transfer project.

### Security
- Fixed reflected Cross-Site Scripting (XSS) vulnerability on cookies page (150001) - added server-side URL validation to prevent malicious script injection via returnUrl parameter

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-07.844...production-2025-10-14.892) for everything in the release

---

## [1.20.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-07.844) - 2025-10-07

### Added
- Added `Land Questionnaire` task for conversion project.
- Added `Main contact` task page
- Added `Church supplemental agreement` task for both conversion and transfer projects.
- Added `Commercial Transfer Agreement` task for conversion and transfer projects.

### Security
- patch reverse tabnabbing vulnerability by including noopener norefferer on external links (target="_blank") - 150222 Reverse Tabnabbing
- add Cross-Origin-Opener-Policy HTTP security header to allow same origin only - 150222 Reverse Tabnabbing

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-10-03.811...production-2025-10-07.844) for everything in the release

---

## [1.19.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-10-03.811) - 2025-10-03

### Added
- Added `Confirm all condition has been met` task for conversion projects.
- Added `Dao Revocation` workflow.
- Configured Cache settings.
- Added `Confirm the date the academy opened` task.
- Added `Confirm the date the academy transferred` task
- Added 'Closing of transfer or conversion project'.
- Add API end point for creating user
- Add ability to create a user

### Fixed
- Fixed broken task note urls.
- Fixed all project by month query for conversion projects if all conditions met is null

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-09-25.771...production-2025-10-03.811) for everything in the release

---

## [1.18.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-09-25.771) - 2025-09-25

### Removed
- removed the 'Add Project' in your project button and disabled access to all related subpages 

### Added
- Ability to edit project information for conversions and transfers
- ProjectGroup is created when a project is edited with a new GRN
- Added Add, Edit and Delete External Contacts for both conversion and transfer projects.
- Added `Confirm this transfer has authority to proceed` task
- Added `Confirm the date the academy transferred` task
- Add service support - view users table

### Changed
- Group reference number links to the group on "About the project"

### Fixed
- GroupReferenceNumberAttribute failed when there was no existing group
- update "Task - Supplemental funding agreement - transfer - incorrect options"
- Fixed `External stakeholder kick off` task wording for both conversion and transfer projects. 
- Fixed Error Summary partial view expect model, pass null wherever not required

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-09-12.746...production-2025-09-25.771) for everything in the release

---

## [1.17.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-09-12.746) - 2025-09-12

### Added
- Added `Receive declaration of expenditure certificate` task for conversion and transfer projects.

### Fixed
- update "Give feedback about service" to use correct link
- update /privacy and /accessibility to Allow Anonymous

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-09-09.731...production-2025-09-12.746) for everything in the release

---

## [1.16.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-09-09.731) - 2025-09-09

### Added
- Added `Redact and send document` task for both conversion and transfer projects.
- Added `Confirm proposed capacity of the academy` task for conversion project.
- Added `Supplemental Funding Agreement` task data for both conversion and transfer projects.

### Security
- prevent inactive users from signing in

### Changed
- Complete project button will hide when users don't have access
- Sort "Your projects in progress" in ascending date order

### Fixed
- Complete project post no longer throws antiforgery error
- Correct wording on handover complete confirmation page

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-09-03.710...production-2025-09-09.731) for everything in the release

---

## [1.15.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-09-03.710) - 2025-09-03

### Changed
- Postcode validation now insensitive
- UK phone number validation to include a wider range
- Updated the implementation of `StakeholderKickoffTaskModel`

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-09-01.697...production-2025-09-03.710) for everything in the release

---

## [1.14.1](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-09-01.697) - 2025-09-01

### Added
- Enabled `DB retry` logic on failure.

### Fixed
- hotfix - fetch only active users when attaching claims from database roles
- Fixed `Signed Secretary state` checkbox data on the `Deed of novation and variation` task page.

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-09-01.688...production-2025-09-01.697) for everything in the release

---

## [1.14.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-09-01.688) - 2025-09-01

### Added
- Added `Article of association` task data for both conversion and trasnfer projects.
- Added 'Deed of novation and variation' task for transfer projects.
- Added 'Deed of variation' task for both conversion and transfer projects. 

### Fixed
- GetProjectByUrn doesn't return notes
- Incoming and Outgoing trust information missing from project header
- HOTFIX: Get user by active directory ID only returns active user

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-08-26.669...production-2025-09-01.688) for everything in the release

---

## [1.13.1](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-08-26.669) - 2025-08-26

### Security
- update "CanViewYourProjects" to depend exclusively on "assign_to_project"

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-08-20.658...production-2025-08-26.669) for everything in the release

---

## [1.13.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-08-20.658) - 2025-08-20

### Added
- Added `Conversion` and `Transfer` task list with their statuses.
- Project groups: new API endpoints
  - `/v1/ProjectGroup/List` – list project groups (with establishments)
  - `/v1/ProjectGroup/Details` – project group details by id
- Frontend routes and pages for project groups
    - `/groups` – project groups list
    - `/groups/{groupId}` – project group details
- Added `Handover with Regional Delivery Officer` task page

### Fixed
- local authority name missing on delete local authority dialog
- add missing incoming trust details on project page

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-08-12.625...production-2025-08-20.658) for everything in the release

---

## [1.12.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-08-12.625) - 2025-08-12

### Added
- Added Project Significant History Dates `/projects/{projectId}/date-history`
- Added `/projects/{projectId}/external-contacts` page
- Added `/privacy` page

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-08-05.608...production-2025-08-12.625) for everything in the release

---

## [1.11.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-08-05.608) - 2025-08-05

### Added
- Add handover page  (`/projects/all/handover`)
- Add handover project check page  (`/projects/all/handover/{projectId}/check`)
- Add handover project add detail & confirmation page  (`/projects/all/handover/{projectId}/new`)
- Added `App Insight` for tracking users clicks and page views

### Changed
- Restructured the task page to be more generic
- Update CRUD endpoints to account for TaskIdentifier

### Fixed
- Fixed cookies page's URL by including query string.
- Fixed `Local Authority` service support endpoints
- Fixed all trust list page by filtering to incoming ukprn instead of MAT and non MAT projects
- Fixed _ProjectLayout to correctly show the unassigned banner based on whether the user is service support or assigned to the project

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-07-24.577...production-2025-08-05.608) for everything in the release

---

## [1.10.2](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-07-24.577) - 2025-07-24

### Fixed
- HOTFIX: antiforgery issue when accepting cookies from Ruby app

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-07-22.558...production-2025-07-24.577) for everything in the release

---

## [1.10.1](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-07-22.558) - 2025-07-22

### Fixed
- HOTFIX: Reports tab now points to exports and dotnet app will handle redirect

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-07-22.553...production-2025-07-22.558) for everything in the release

---

## [1.10.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-07-22.553) - 2025-07-22

### Added
- New route `/projects/all/reports`
- Add reports landing page under All projects > Reports (`/projects/all/reports`)
- New route `/projects/{projectId}/date-history`
- New route `/projects/{projectId}/date-history/new`
- New route `/projects/{projectId}/date-history/reason`
- New route `/projects/{projectId}/date-history/reasons/later`

### Changed  
- Add a redirect from projects/{id} to projects/{id}/tasks
- Reduce log level from error to warning when project routes receive bad GUID or project not found 
- Add a redirect from `/projects/all/export` to `/projects/all/reports`

### Fixed
- Remove double pagination on Service Support > Local authorities
- Resolve "About the Project" academy crash

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-07-14.525...production-2025-07-22.553) for everything in the release

---

## [1.9.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-07-14.525) - 2025-07-14

### Added  
- Add notes repository, along with create, read, update and delete queries/commands
- Add project notes page (`/projects/{projectId}/notes`)
- Add project notes editing page (`/projects/{projectId}/notes/{noteId}/edit`)
- Add project notes creation page (`/projects/{projectId}/notes/new`)
- Add ability to delete note (`/projects/{projectId}/notes/{noteId}/delete`)
- Attach user ID from DB as custom claim

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-07-10.519...production-2025-07-14.525) for everything in the release

---

## [1.8.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-07-10.519) - 2025-07-10

### Added
- New route /projects/service-support/without-academy-urn
- Service support Conversion URNs (/projects/service-support/without-academy-urn)
- New route /projects/{projectId}/academy-urn
- Service support Create Academy URN (/projects/{projectId}/academy-urn) 
- Added `Statistics` page
- Added Project Significant History Dates `/projects/{projectId}/date-history`

### Fixed
- Throw exception if redis configs are not present and redis is enabled.

### Security
- Use only custom antiforgery for cookies due to requiring anonymous access

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-06-27.463...production-2025-07-10.519) for everything in the release

---

## [1.7.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-06-27.463) - 2025-06-27

### Added
- Internal contacts page and edit pages `/projects/{projectId}/internal-contacts`
- app settings for test environment

### Fixed
- footer links for production
- privacy link
- show 'service not working' on unexpected error
- Fixed trust not found issue.
- Updated body message on `Page not found` page.

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-06-24.451...production-2025-06-27.463) for everything in the release
---

## [1.6.2](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-06-24.451) - 2025-06-24

### Fixed
- Notification banner was not showing as cookie banner was clearing TempData

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-06-23.444...production-2025-06-24.451) for everything in the release
---

## [1.6.1](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-06-23.444) - 2025-06-23

### Fixed  
- Fixed incoming trust displaying as `None`
- update exports tab to point at correct page
- Fixed project creation path.

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-06-17.413...production-2025-06-23.444) for everything in the release
---

## [1.6.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-06-17.413) - 2025-05-30

### Fixed  
- Updated pagination query parameter from `pageNumber` to `page` to match Ruby app
- Optimised queries behind the "By local authority" page
- Show a `Page Not Found` error if the requested page number exceeds the total number of available pages.
- Fixed the search functionality to return only projects with status values of 0, 1, or 3 (Active, Completed or DAO revoked)
- Fixed unable to set cookies issue if request is coming from ruby app

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-05-30.320...production-2025-06-17.413) for everything in the release

---

## [1.5.4](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-05-30.320) - 2025-05-30
### Added  
- `PolicyCheckTagHelper` added to conditionally hide elements based on policy
- Query builder in the infrastructure layer to help support custom queries

### Changed  
- Navigation items previously hidden with `UserTabAccessHelper` now hide on policy

### Fixed  
- Unassigned projects should show "Not yet assigned" under "Assigned To" column for projects on the local authority/trust pages
- Optimised queries behind the "By month" and "For trust" listing pages

### Removed
- `UserTabAccessHelper` class is no longer required. Use policies instead

### Security
- Only correct user groups can now create projects

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-05-22.290...production-2025-05-30.320) for everything in the release

---

## [1.5.3](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-05-22.290) - 2025-05-22
### Added  
 - Enabled error tracking via Application Insights.
 - New route `/projects/team/unassigned`
 - Your team projects "Unassigned" list (`/projects/team/unassigned`)

### Changed  
- Sort all projects by region list alphabetically

### Fixed  
- Note FK Ids are now required
- Separated created and assigned users in project creation

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-05-16.272...production-2025-05-22.290) for everything in the release

---

## [1.5.2](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-05-16.272) - 2025-05-16

### Changed
- Optimise several project listing queries by implementing pagination before retrieving records

### Fixed  
- Fixed identifying "Form A MAT" projects logic
- Removed unnecessary `Assign To` filter while pulling projects from database.
- Resolve accessibility issue causing app header to appear blue instead of white
- Removed `Project Status` filter while pullling search results.  

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-05-14.254...production-2025-05-16.272) for everything in the release

---

## [1.5.1](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-05-14.254) - 2025-05-14

### Changed
 - Change date format to "Month Year" string on local authority projects list
 - Change projects to sort by significant date on local authority projects list

### Fixed  
 - Resolve project pagination issue on "Team projects" > "By User" > User page

### Security
 - Authorization fixed on all API endpoints

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-05-13.244...production-2025-05-14.254) for everything in the release

---

## [1.5.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-05-13.244) - 2025-05-13
### Added  
 - Added "order by field" argument to `GetUserWithProjectsQuery`
 - Added search bar to search projects with active status

### Changed  
 - Merged `ListAllUsersInTeamWithProjectsQuery` into `ListAllUsersWithProjectsQuery` with filter
 - Order "Team projects" > "By User" by significant date
 - Filter "Team projects" > "Handed over" to active projects only

### Fixed  
 - Routing for projects merged (`/conversion-project` and `/transfer-project` become `/project`)
 - "Team projects" > "Handed over" now shows unassigned projects again


See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-05-08.217...production-2025-05-13.244) for everything in the release

---

## [1.4.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-05-08.217) - 2025-05-08
### Added
 - New route `/projects/all/by-month/conversions/{month}/{year}`
 - New route `/projects/all/by-month/transfers/{month}/{year}`
 - New route `/projects/all/by-month/conversions/from/{fromMonth}/{fromYear}/to/{toMonth}/{toYear}`
 - New route `/projects/all/by-month/transfers/from/{fromMonth}/{fromYear}/to/{toMonth}/{toYear}`
 - New route `/projects/{projectId}/tasks`
 - New route `/projects/team/new`
 - New route `/projects/team/handed-over`
 - New route `/projects/team/users`
 - New route `/projects/team/users/{userId}`
 - Your team projects "New" list (`/projects/team/new`)
 - Your team projects "Handed over" list (`/projects/team/handed-over`)
 - Your team projects "By user" list (`/projects/team/users`)
 - Your team projects "By user" > "User" list (`/projects/team/users/{userId}`)
 - Add new `ProjectTeam` extension method `TeamIsRegionalCaseworkServices`, to identify RCS users 
 - Projects added by you (`/projects/yours/added-by`)
 - Projects completed by you (`/projects/yours/completed`)

### Changed
 - Merged ListAllProjectsByFilter into main ListAllProjects query
 - Add an "orderBy" argument to the `ListAllProjectsByFilter` query
 - Allow `ListAllProjectsByFilter` query to handle multiple filters
 - All transfer/conversion projects list use a partial
 - Projects will route to `/project/{projectId}/tasks` from all projects list

### Fixed
 - Project for user list should show month and year (not day)

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-04-24.175...production-2025-05-08.217) for everything in the release

---

## [1.3.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-04-24.175) - 2025-04-25
### Added
 - New route `/projects/team/completed`
 - Your team projects completed list (`/projects/team/completed`)


### Changed
 - Filter out any local authorities with no projects in `ListAllProjectByLocalAuthorities`
 - Include unassigned projects in "All projects" > "By region"

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-04-17.164...production-2025-04-24.175) for everything in the release

---

## [1.2.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-04-17.164) - 2025-04-17
### Added
 - New route `/projects/team/in-progress`
 - Your team projects in progress list (`/projects/team/in-progress`)
 - Added user (`ClaimsPrincipal`) extension to get users team `GetUserTeam`
 - Added API endpoint `/v1/Projects/List/All/LocalAuthority` for fetching projects for local authority
 - Added API endpoint `/v1/Projects/List/All/Region` for fetching projects for region
 - Added API endpoint `/v1/Projects/List/All/Team` for fetching projects for team
 - Added API endpoint `/v1/Projects/List/All/User` for fetching projects for user
 - Added endpoint to the projectsController `ListAllProjectsInTrust`-`/v1/Projects/List/Trust`
 - Added missing "project for region" header
 - New route `/projects/all/in-progress/form-a-multi-academy-trust`
 - New route `/form-a-multi-academy-trust/{reference}`
 - Form a MAT with projects in progress list (`/projects/all/in-progress/form-a-multi-academy-trust`)
 - MAT projects listing related establishments (`/form-a-multi-academy-trust/{reference}`)
 - Added missing "project for region" header

 - User redirection on app load based on their permissions
 - Add navigation items to be more consistent with ruby UI
 - New route `/projects/team/users`
 - Your team projects by user list (`/projects/team/users`)
 - Your team projects by user query `ListAllUsersInTeamWithProjectsQuery`

### Changed
 - Updated route `/accessibility-statement` to `/accessibility`
 - Updated route `/public/cookies` to `/cookies`
 - Updated route `/projects/transfer/new_mat` to `/projects/transfers/new_mat`
 - Updated route `/projects/transfer-projects/new` to `/projects/transfers/new`
 - Updated ListAllProjects in-progress and Count all projects in progress to filter out unassigned projects
 - Don't filter unassigned projects for "All projects by region" -> Region
 - Move tab access logic to a helper `UserTabAccessHelper`

### Fixed
 - Correctly identify test env based on environment name being "Test" (previously looking for "Staging")
 - Added WireMock support back
 - Show 404 page when get projects for region `/projects/all/regions/{region}` has a "bad" region in path param
 - null `AssignedTo` in `ListAllProjects` throws an unexpected error
 - Show 404 page when get projects for region `/projects/all/regions/{region}` has a "bad" region in path param
 - null `AssignedTo` in `ListAllProjects` throws an unexpected error

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/production-2025-04-01.120-manual...production-2025-04-17.164) for everything in the release

---

## [1.1.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/releases/tag/production-2025-04-01.120-manual) - 2025-04-01
### Added
 - Another 'sync' release to bring the changelog up to date

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/development-2025-03-05.78...production-2025-04-01.120-manual) for everything in the release

---

## [1.0.0](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/082ba69cfa1b5b098d5dd5e2c804e8f5c58c2a00...development-2025-03-05.78) - 2025-03-28

### Added
 - Initial changelog setup to match the current production state.
 - Captures prior production releases retroactively, for syncing purposes.

See the [full commit history](https://github.com/DFE-Digital/complete-conversions-transfers-changes/compare/082ba69cfa1b5b098d5dd5e2c804e8f5c58c2a00...development-2025-03-05.78) for everything in the release

### Added
 - New route (About the project page) `/projects/{urn or ukprn}/information`
