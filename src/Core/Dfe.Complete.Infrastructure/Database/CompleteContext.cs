using Dfe.Complete.Domain.Entities;
using Dfe.Complete.Domain.Enums;
using Dfe.Complete.Domain.ValueObjects;
using Dfe.Complete.Infrastructure.Database.Interceptors;
using Dfe.Complete.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Complete.Infrastructure.Database;

public partial class CompleteContext : DbContext
{
    private readonly IConfiguration? _configuration;
    const string DefaultSchema = "complete";
    private readonly IServiceProvider _serviceProvider = null!;

    public CompleteContext()
    {
    }

    public CompleteContext(DbContextOptions<CompleteContext> options, IConfiguration configuration, IServiceProvider serviceProvider)
        : base(options)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<ConversionTasksData> ConversionTasksData { get; set; }

    public virtual DbSet<DaoRevocation> DaoRevocations { get; set; }

    public virtual DbSet<DaoRevocationReason> DaoRevocationReasons { get; set; }

    public virtual DbSet<GiasEstablishment> GiasEstablishments { get; set; }

    public virtual DbSet<GiasGroup> GiasGroups { get; set; }

    public virtual DbSet<KeyContact> KeyContacts { get; set; }

    public virtual DbSet<LocalAuthority> LocalAuthorities { get; set; }

    public virtual DbSet<Note> Notes { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectGroup> ProjectGroups { get; set; }

    public virtual DbSet<SignificantDateHistory> SignificantDateHistories { get; set; }

    public virtual DbSet<SignificantDateHistoryReason> SignificantDateHistoryReasons { get; set; }

    public virtual DbSet<TransferTasksData> TransferTasksData { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration!.GetConnectionString("DefaultConnection");
            optionsBuilder.UseCompleteSqlServer(connectionString!, _configuration!.GetValue("EnableSQLRetryOnFailure", false));
        }

        var mediator = _serviceProvider.GetRequiredService<IMediator>();
        optionsBuilder.AddInterceptors(new DomainEventDispatcherInterceptor(mediator));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(ConfigureProject);
        modelBuilder.Entity<User>(ConfigureUser);
        modelBuilder.Entity<Contact>(ConfigureContact);
        modelBuilder.Entity<ConversionTasksData>(ConfigureConversionTasksData);
        modelBuilder.Entity<TransferTasksData>(ConfigureTransferTasksData);
        modelBuilder.Entity<DaoRevocation>(ConfigureDaoRevocation);
        modelBuilder.Entity<DaoRevocationReason>(ConfigureDaoRevocationReason);
        modelBuilder.Entity<GiasEstablishment>(ConfigureGiasEstablishment);
        modelBuilder.Entity<GiasGroup>(ConfigureGiasGroup);
        modelBuilder.Entity<KeyContact>(ConfigureKeyContact);
        modelBuilder.Entity<LocalAuthority>(ConfigureLocalAuthority);
        modelBuilder.Entity<Note>(ConfigureNote);
        modelBuilder.Entity<ProjectGroup>(ConfigureProjectGroup);
        modelBuilder.Entity<SignificantDateHistory>(ConfigureSignificantDateHistory);
        modelBuilder.Entity<SignificantDateHistoryReason>(ConfigureSignificantDateHistoryReason);

        OnModelCreatingPartial(modelBuilder);
    }

    private static void ConfigureProject(EntityTypeBuilder<Project> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("projects", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .ValueGeneratedOnAdd()
            .HasConversion(
                v => v!.Value,
                v => new ProjectId(v))
            .HasColumnName("id");

        projectConfiguration.Property(e => e.AcademyUrn)
            .HasColumnName("academy_urn")
            .HasConversion(
                v => v!.Value,
                v => new Urn(v));
        projectConfiguration.Property(e => e.AdvisoryBoardConditions).HasColumnName("advisory_board_conditions");
        projectConfiguration.Property(e => e.AdvisoryBoardDate).HasColumnName("advisory_board_date");
        projectConfiguration.Property(e => e.AllConditionsMet)
            .HasDefaultValue(false)
            .HasColumnName("all_conditions_met");
        projectConfiguration.Property(e => e.AssignedAt)
            .HasPrecision(6)
            .HasColumnName("assigned_at");
        projectConfiguration.Property(e => e.AssignedToId)
            .HasColumnName("assigned_to_id")
            .HasConversion(
                v => v!.Value,
                v => new UserId(v));
        projectConfiguration.Property(e => e.CaseworkerId)
            .HasColumnName("caseworker_id")
            .HasConversion(
                v => v!.Value,
                v => new UserId(v));
        projectConfiguration.Property(e => e.CompletedAt)
            .HasPrecision(6)
            .HasColumnName("completed_at");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.DirectiveAcademyOrder)
            .HasDefaultValue(false)
            .HasColumnName("directive_academy_order");
        projectConfiguration.Property(e => e.EstablishmentMainContactId)
            .HasColumnName("establishment_main_contact_id")
            .HasConversion(
                v => v!.Value,
                v => new ContactId(v));
        projectConfiguration.Property(e => e.EstablishmentSharepointLink).HasColumnName("establishment_sharepoint_link");
        projectConfiguration.Property(e => e.GroupId)
            .HasColumnName("group_id")
            .HasConversion(
                v => v!.Value,
                v => new ProjectGroupId(v));
        projectConfiguration.Property(e => e.IncomingTrustMainContactId)
            .HasColumnName("incoming_trust_main_contact_id")
            .HasConversion(
                v => v!.Value,
                v => new ContactId(v));
        projectConfiguration.Property(e => e.IncomingTrustSharepointLink).HasColumnName("incoming_trust_sharepoint_link");
        projectConfiguration.Property(e => e.IncomingTrustUkprn)
            .HasColumnName("incoming_trust_ukprn")
            .HasConversion(
                v => v!.Value,
                v => new Ukprn(v));
        projectConfiguration.Property(e => e.LocalAuthorityMainContactId)
            .HasColumnName("local_authority_main_contact_id")
            .HasConversion(
                v => v!.Value,
                v => new ContactId(v));
        projectConfiguration.Property(e => e.MainContactId)
            .HasColumnName("main_contact_id")
            .HasConversion(
                v => v!.Value,
                v => new ContactId(v));
        projectConfiguration.Property(e => e.NewTrustName)
            .HasMaxLength(4000)
            .HasColumnName("new_trust_name");
        projectConfiguration.Property(e => e.NewTrustReferenceNumber)
            .HasMaxLength(4000)
            .HasColumnName("new_trust_reference_number");
        projectConfiguration.Property(e => e.OutgoingTrustMainContactId)
            .HasColumnName("outgoing_trust_main_contact_id")
            .HasConversion(
                v => v!.Value,
                v => new ContactId(v));
        projectConfiguration.Property(e => e.OutgoingTrustSharepointLink).HasColumnName("outgoing_trust_sharepoint_link");
        projectConfiguration.Property(e => e.OutgoingTrustUkprn)
            .HasColumnName("outgoing_trust_ukprn")
            .HasConversion(
                v => v!.Value,
                v => new Ukprn(v));
        projectConfiguration.Property(e => e.PrepareId).HasColumnName("prepare_id");
        projectConfiguration.Property(e => e.Region)
            .HasMaxLength(4000)
            .HasColumnName("region")
            .HasConversion(
                r => r.GetCharValue(),
                regionDbValue => regionDbValue.ToEnumFromChar<Region>());
        projectConfiguration.Property(e => e.RegionalDeliveryOfficerId)
            .HasColumnName("regional_delivery_officer_id")
            .HasConversion(
                v => v!.Value,
                v => new UserId(v));
        projectConfiguration.Property(e => e.SignificantDate).HasColumnName("significant_date");
        projectConfiguration.Property(e => e.SignificantDateProvisional)
            .HasDefaultValue(true)
            .HasColumnName("significant_date_provisional");
        projectConfiguration.Property(e => e.State).HasColumnName("state");
        projectConfiguration.Property(e => e.TasksDataId)
            .HasColumnName("tasks_data_id")
            .HasConversion(
                v => v!.Value,
                v => new TaskDataId(v));
        projectConfiguration.Property(e => e.TasksDataType)
            .HasMaxLength(4000)
            .HasColumnName("tasks_data_type")
            .HasConversion(
                tasksType => tasksType.ToDescription(),
                tasksTypeDbValue => tasksTypeDbValue.FromDescriptionValue<TaskType>());
        projectConfiguration.Property(e => e.Team)
            .HasMaxLength(4000)
            .HasColumnName("team")
            .HasConversion(
                team => team.ToDescription(),
                teamDbValue => teamDbValue.FromDescriptionValue<ProjectTeam>());
        projectConfiguration.Property(e => e.TwoRequiresImprovement)
            .HasDefaultValue(null)
            .HasColumnName("two_requires_improvement");
        projectConfiguration.Property(e => e.Type)
            .HasMaxLength(4000)
            .HasColumnName("type")
            .HasConversion(
                projectType => projectType.ToDescription(),
                projectTypeDbValue => projectTypeDbValue.FromDescriptionValue<ProjectType>());
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
        projectConfiguration.Property(e => e.Urn).HasColumnName("urn")
            .HasConversion(
                v => v!.Value,
                v => new Urn(v));

        projectConfiguration.Property(e => e.Urn)
           .HasConversion(
               v => v!.Value,
               v => new Urn(v));

        projectConfiguration.Property(e => e.IncomingTrustUkprn)
          .HasConversion(
              v => v!.Value,
              v => new Ukprn(v));

        projectConfiguration.Property(e => e.RegionalDeliveryOfficerId)
          .HasConversion(
              v => v!.Value,
              v => new UserId(v));

        projectConfiguration.Property(e => e.CaseworkerId)
          .HasConversion(
              v => v!.Value,
              v => new UserId(v));

        projectConfiguration.Property(e => e.AssignedToId)
          .HasConversion(
              v => v!.Value,
              v => new UserId(v));

        projectConfiguration.Property(e => e.AcademyUrn)
          .HasConversion(
              v => v!.Value,
              v => new Urn(v));

        projectConfiguration.Property(e => e.OutgoingTrustUkprn)
          .HasConversion(
              v => v!.Value,
              v => new Ukprn(v));

        projectConfiguration.Property(e => e.MainContactId)
          .HasConversion(
              v => v!.Value,
              v => new ContactId(v));

        projectConfiguration.Property(e => e.EstablishmentMainContactId)
          .HasConversion(
              v => v!.Value,
              v => new ContactId(v));

        projectConfiguration.Property(e => e.IncomingTrustMainContactId)
          .HasConversion(
              v => v!.Value,
              v => new ContactId(v));

        projectConfiguration.Property(e => e.OutgoingTrustMainContactId)
          .HasConversion(
              v => v!.Value,
              v => new ContactId(v));

        projectConfiguration.Property(e => e.LocalAuthorityId)
            .HasColumnName("local_authority_id")
            .HasConversion(
                v => v.Value,
                v => new LocalAuthorityId(v));

        projectConfiguration.HasOne(d => d.AssignedTo).WithMany(p => p.ProjectAssignedTos)
            .HasForeignKey(d => d.AssignedToId)
            .HasConstraintName("fk_rails_9cf9d80ba9");

        projectConfiguration.HasOne(d => d.Caseworker).WithMany(p => p.ProjectCaseworkers)
            .HasForeignKey(d => d.CaseworkerId)
            .HasConstraintName("fk_rails_246548228c");

        projectConfiguration.HasOne(d => d.RegionalDeliveryOfficer).WithMany(p => p.ProjectRegionalDeliveryOfficers)
            .HasForeignKey(d => d.RegionalDeliveryOfficerId)
            .HasConstraintName("fk_rails_bba1c6b145");

        projectConfiguration.HasOne(p => p.LocalAuthority)
            .WithMany()
            .HasForeignKey(p => p.LocalAuthorityId)
            .HasConstraintName("fk_rails_eddab2651f");

        projectConfiguration.HasMany(d => d.Notes).WithOne(p => p.Project).HasForeignKey(p => p.ProjectId);

        projectConfiguration.HasOne(p => p.GiasEstablishment)
            .WithMany()
            .HasForeignKey(p => p.Urn)
            .HasPrincipalKey(g => g.Urn);
    }

    private static void ConfigureUser(EntityTypeBuilder<User> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("users", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
                .HasConversion(
                    v => v!.Value,
                    v => new UserId(v))
            .HasColumnName("id");

        projectConfiguration.Property(e => e.ActiveDirectoryUserGroupIds)
            .HasMaxLength(4000)
            .HasColumnName("active_directory_user_group_ids");
        projectConfiguration.Property(e => e.ActiveDirectoryUserId)
            .HasMaxLength(4000)
            .HasColumnName("active_directory_user_id");
        projectConfiguration.Property(e => e.AddNewProject).HasColumnName("add_new_project");
        projectConfiguration.Property(e => e.AssignToProject)
            .HasDefaultValue(false)
            .HasColumnName("assign_to_project");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.DeactivatedAt)
            .HasPrecision(6)
            .HasColumnName("deactivated_at");
        projectConfiguration.Property(e => e.Email)
            .HasMaxLength(4000)
            .HasColumnName("email");
        projectConfiguration.Property(e => e.FirstName)
            .HasMaxLength(4000)
            .HasColumnName("first_name");
        projectConfiguration.Property(e => e.LastName)
            .HasMaxLength(4000)
            .HasColumnName("last_name");
        projectConfiguration.Property(e => e.LatestSession)
            .HasPrecision(6)
            .HasColumnName("latest_session");
        projectConfiguration.Property(e => e.ManageConversionUrns)
            .HasDefaultValue(false)
            .HasColumnName("manage_conversion_urns");
        projectConfiguration.Property(e => e.ManageLocalAuthorities)
            .HasDefaultValue(false)
            .HasColumnName("manage_local_authorities");
        projectConfiguration.Property(e => e.ManageTeam)
            .HasDefaultValue(false)
            .HasColumnName("manage_team");
        projectConfiguration.Property(e => e.ManageUserAccounts)
            .HasDefaultValue(false)
            .HasColumnName("manage_user_accounts");
        projectConfiguration.Property(e => e.Team)
            .HasMaxLength(4000)
            .HasColumnName("team");
        projectConfiguration.Property(e => e.EntraUserObjectId)
            .HasMaxLength(4000)
            .HasColumnName("entra_user_object_id");
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");

        projectConfiguration
            .HasMany(c => c.Notes)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId);

        projectConfiguration
            .HasMany(c => c.ProjectAssignedTos)
            .WithOne(e => e.AssignedTo)
            .HasForeignKey(e => e.AssignedToId);

        projectConfiguration
            .HasIndex(e => e.EntraUserObjectId)
            .IsUnique()
            .HasDatabaseName("UQ_users_entra_user_object_id")
            .HasFilter("[entra_user_object_id] IS NOT NULL");
    }

    private static void ConfigureContact(EntityTypeBuilder<Contact> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("contacts", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                    v => v!.Value,
                    v => new ContactId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.Category).HasColumnName("category");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.Email)
            .HasMaxLength(4000)
            .HasColumnName("email");
        projectConfiguration.Property(e => e.EstablishmentUrn).HasColumnName("establishment_urn");
        projectConfiguration.Property(e => e.LocalAuthorityId)
            .HasColumnName("local_authority_id")
            .HasConversion(
                    v => v!.Value,
                    v => new LocalAuthorityId(v));
        projectConfiguration.Property(e => e.Name)
            .HasMaxLength(4000)
            .HasColumnName("name");
        projectConfiguration.Property(e => e.OrganisationName)
            .HasMaxLength(4000)
            .HasColumnName("organisation_name");
        projectConfiguration.Property(e => e.Phone)
            .HasMaxLength(4000)
            .HasColumnName("phone");
        projectConfiguration.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .HasConversion(
                    v => v!.Value,
                    v => new ProjectId(v));
        projectConfiguration.Property(e => e.Title)
            .HasMaxLength(4000)
            .HasColumnName("title");
        projectConfiguration.Property(e => e.Type)
            .HasMaxLength(4000)
            .HasColumnName("type")
            .HasConversion(
                contactType => contactType.ToDescription(),
                contactTypeDbValue => contactTypeDbValue.FromDescriptionValue<ContactType>());
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");

        projectConfiguration.HasOne(d => d.Project).WithMany(p => p.Contacts)
            .HasForeignKey(d => d.ProjectId)
            .HasConstraintName("fk_rails_b0485f0dbc");
    }

    private static void ConfigureConversionTasksData(EntityTypeBuilder<ConversionTasksData> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("conversion_tasks_data", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .ValueGeneratedOnAdd()
            .HasConversion(
                v => v!.Value,
                v => new TaskDataId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.AcademyDetailsName)
            .HasMaxLength(4000)
            .HasColumnName("academy_details_name");
        projectConfiguration.Property(e => e.ArticlesOfAssociationCleared).HasColumnName("articles_of_association_cleared");
        projectConfiguration.Property(e => e.ArticlesOfAssociationNotApplicable).HasColumnName("articles_of_association_not_applicable");
        projectConfiguration.Property(e => e.ArticlesOfAssociationReceived).HasColumnName("articles_of_association_received");
        projectConfiguration.Property(e => e.ArticlesOfAssociationSaved).HasColumnName("articles_of_association_saved");
        projectConfiguration.Property(e => e.ArticlesOfAssociationSent).HasColumnName("articles_of_association_sent");
        projectConfiguration.Property(e => e.ArticlesOfAssociationSigned).HasColumnName("articles_of_association_signed");
        projectConfiguration.Property(e => e.CheckAccuracyOfHigherNeedsConfirmNumber).HasColumnName("check_accuracy_of_higher_needs_confirm_number");
        projectConfiguration.Property(e => e.CheckAccuracyOfHigherNeedsConfirmPublishedNumber).HasColumnName("check_accuracy_of_higher_needs_confirm_published_number");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementCleared).HasColumnName("church_supplemental_agreement_cleared");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementNotApplicable).HasColumnName("church_supplemental_agreement_not_applicable");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementReceived).HasColumnName("church_supplemental_agreement_received");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSaved).HasColumnName("church_supplemental_agreement_saved");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSent).HasColumnName("church_supplemental_agreement_sent");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSigned).HasColumnName("church_supplemental_agreement_signed");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSignedDiocese).HasColumnName("church_supplemental_agreement_signed_diocese");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSignedSecretaryState).HasColumnName("church_supplemental_agreement_signed_secretary_state");
        projectConfiguration.Property(e => e.CommercialTransferAgreementAgreed)
            .HasDefaultValue(false)
            .HasColumnName("commercial_transfer_agreement_agreed");
        projectConfiguration.Property(e => e.CommercialTransferAgreementQuestionsChecked)
            .HasDefaultValue(false)
            .HasColumnName("commercial_transfer_agreement_questions_checked");
        projectConfiguration.Property(e => e.CommercialTransferAgreementQuestionsReceived)
            .HasDefaultValue(false)
            .HasColumnName("commercial_transfer_agreement_questions_received");
        projectConfiguration.Property(e => e.CommercialTransferAgreementSaved)
            .HasDefaultValue(false)
            .HasColumnName("commercial_transfer_agreement_saved");
        projectConfiguration.Property(e => e.CommercialTransferAgreementSigned)
            .HasDefaultValue(false)
            .HasColumnName("commercial_transfer_agreement_signed");
        projectConfiguration.Property(e => e.CompleteNotificationOfChangeCheckDocument).HasColumnName("complete_notification_of_change_check_document");
        projectConfiguration.Property(e => e.CompleteNotificationOfChangeNotApplicable).HasColumnName("complete_notification_of_change_not_applicable");
        projectConfiguration.Property(e => e.CompleteNotificationOfChangeSendDocument).HasColumnName("complete_notification_of_change_send_document");
        projectConfiguration.Property(e => e.CompleteNotificationOfChangeTellLocalAuthority).HasColumnName("complete_notification_of_change_tell_local_authority");
        projectConfiguration.Property(e => e.ConfirmDateAcademyOpenedDateOpened).HasColumnName("confirm_date_academy_opened_date_opened");
        projectConfiguration.Property(e => e.ConversionGrantCheckVendorAccount).HasColumnName("conversion_grant_check_vendor_account");
        projectConfiguration.Property(e => e.ConversionGrantNotApplicable).HasColumnName("conversion_grant_not_applicable");
        projectConfiguration.Property(e => e.ConversionGrantPaymentForm).HasColumnName("conversion_grant_payment_form");
        projectConfiguration.Property(e => e.ConversionGrantSendInformation).HasColumnName("conversion_grant_send_information");
        projectConfiguration.Property(e => e.ConversionGrantSharePaymentDate).HasColumnName("conversion_grant_share_payment_date");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.DeedOfVariationCleared).HasColumnName("deed_of_variation_cleared");
        projectConfiguration.Property(e => e.DeedOfVariationNotApplicable).HasColumnName("deed_of_variation_not_applicable");
        projectConfiguration.Property(e => e.DeedOfVariationReceived).HasColumnName("deed_of_variation_received");
        projectConfiguration.Property(e => e.DeedOfVariationSaved).HasColumnName("deed_of_variation_saved");
        projectConfiguration.Property(e => e.DeedOfVariationSent).HasColumnName("deed_of_variation_sent");
        projectConfiguration.Property(e => e.DeedOfVariationSigned).HasColumnName("deed_of_variation_signed");
        projectConfiguration.Property(e => e.DeedOfVariationSignedSecretaryState).HasColumnName("deed_of_variation_signed_secretary_state");
        projectConfiguration.Property(e => e.DirectionToTransferCleared).HasColumnName("direction_to_transfer_cleared");
        projectConfiguration.Property(e => e.DirectionToTransferNotApplicable).HasColumnName("direction_to_transfer_not_applicable");
        projectConfiguration.Property(e => e.DirectionToTransferReceived).HasColumnName("direction_to_transfer_received");
        projectConfiguration.Property(e => e.DirectionToTransferSaved).HasColumnName("direction_to_transfer_saved");
        projectConfiguration.Property(e => e.DirectionToTransferSigned).HasColumnName("direction_to_transfer_signed");
        projectConfiguration.Property(e => e.HandoverMeeting).HasColumnName("handover_meeting");
        projectConfiguration.Property(e => e.HandoverNotApplicable).HasColumnName("handover_not_applicable");
        projectConfiguration.Property(e => e.HandoverNotes).HasColumnName("handover_notes");
        projectConfiguration.Property(e => e.HandoverReview).HasColumnName("handover_review");
        projectConfiguration.Property(e => e.LandQuestionnaireCleared).HasColumnName("land_questionnaire_cleared");
        projectConfiguration.Property(e => e.LandQuestionnaireReceived).HasColumnName("land_questionnaire_received");
        projectConfiguration.Property(e => e.LandQuestionnaireSaved).HasColumnName("land_questionnaire_saved");
        projectConfiguration.Property(e => e.LandQuestionnaireSigned).HasColumnName("land_questionnaire_signed");
        projectConfiguration.Property(e => e.LandRegistryCleared).HasColumnName("land_registry_cleared");
        projectConfiguration.Property(e => e.LandRegistryReceived).HasColumnName("land_registry_received");
        projectConfiguration.Property(e => e.LandRegistrySaved).HasColumnName("land_registry_saved");
        projectConfiguration.Property(e => e.MasterFundingAgreementCleared).HasColumnName("master_funding_agreement_cleared");
        projectConfiguration.Property(e => e.MasterFundingAgreementNotApplicable).HasColumnName("master_funding_agreement_not_applicable");
        projectConfiguration.Property(e => e.MasterFundingAgreementReceived).HasColumnName("master_funding_agreement_received");
        projectConfiguration.Property(e => e.MasterFundingAgreementSaved).HasColumnName("master_funding_agreement_saved");
        projectConfiguration.Property(e => e.MasterFundingAgreementSent).HasColumnName("master_funding_agreement_sent");
        projectConfiguration.Property(e => e.MasterFundingAgreementSigned).HasColumnName("master_funding_agreement_signed");
        projectConfiguration.Property(e => e.MasterFundingAgreementSignedSecretaryState).HasColumnName("master_funding_agreement_signed_secretary_state");
        projectConfiguration.Property(e => e.OneHundredAndTwentyFiveYearLeaseEmail).HasColumnName("one_hundred_and_twenty_five_year_lease_email");
        projectConfiguration.Property(e => e.OneHundredAndTwentyFiveYearLeaseNotApplicable).HasColumnName("one_hundred_and_twenty_five_year_lease_not_applicable");
        projectConfiguration.Property(e => e.OneHundredAndTwentyFiveYearLeaseReceive).HasColumnName("one_hundred_and_twenty_five_year_lease_receive");
        projectConfiguration.Property(e => e.OneHundredAndTwentyFiveYearLeaseSaveLease).HasColumnName("one_hundred_and_twenty_five_year_lease_save_lease");
        projectConfiguration.Property(e => e.ProposedCapacityOfTheAcademyNotApplicable).HasColumnName("proposed_capacity_of_the_academy_not_applicable");
        projectConfiguration.Property(e => e.ProposedCapacityOfTheAcademyReceptionToSixYears)
            .HasMaxLength(4000)
            .HasColumnName("proposed_capacity_of_the_academy_reception_to_six_years");
        projectConfiguration.Property(e => e.ProposedCapacityOfTheAcademySevenToElevenYears)
            .HasMaxLength(4000)
            .HasColumnName("proposed_capacity_of_the_academy_seven_to_eleven_years");
        projectConfiguration.Property(e => e.ProposedCapacityOfTheAcademyTwelveOrAboveYears)
            .HasMaxLength(4000)
            .HasColumnName("proposed_capacity_of_the_academy_twelve_or_above_years");
        projectConfiguration.Property(e => e.ReceiveGrantPaymentCertificateCheckCertificate).HasColumnName("receive_grant_payment_certificate_check_certificate");
        projectConfiguration.Property(e => e.ReceiveGrantPaymentCertificateDateReceived).HasColumnName("receive_grant_payment_certificate_date_received");
        projectConfiguration.Property(e => e.ReceiveGrantPaymentCertificateSaveCertificate).HasColumnName("receive_grant_payment_certificate_save_certificate");
        projectConfiguration.Property(e => e.ReceiveGrantPaymentCertificateNotApplicable).HasColumnName("receive_grant_payment_certificate_not_applicable");
        projectConfiguration.Property(e => e.RedactAndSendRedact).HasColumnName("redact_and_send_redact");
        projectConfiguration.Property(e => e.RedactAndSendSaveRedaction).HasColumnName("redact_and_send_save_redaction");
        projectConfiguration.Property(e => e.RedactAndSendSendRedaction).HasColumnName("redact_and_send_send_redaction");
        projectConfiguration.Property(e => e.RedactAndSendSendSolicitors).HasColumnName("redact_and_send_send_solicitors");
        projectConfiguration.Property(e => e.RiskProtectionArrangementOption)
            .HasMaxLength(4000)
            .HasColumnName("risk_protection_arrangement_option")
            .HasConversion(
                rpaOption => rpaOption.ToDescription(),
                rpaOptionDbValue => rpaOptionDbValue.FromDescriptionValue<RiskProtectionArrangementOption>());
        projectConfiguration.Property(e => e.RiskProtectionArrangementReason)
            .HasMaxLength(4000)
            .HasColumnName("risk_protection_arrangement_reason");
        projectConfiguration.Property(e => e.SchoolCompletedEmailed).HasColumnName("school_completed_emailed");
        projectConfiguration.Property(e => e.SchoolCompletedSaved).HasColumnName("school_completed_saved");
        projectConfiguration.Property(e => e.ShareInformationEmail).HasColumnName("share_information_email");
        projectConfiguration.Property(e => e.SponsoredSupportGrantInformTrust).HasColumnName("sponsored_support_grant_inform_trust");
        projectConfiguration.Property(e => e.SponsoredSupportGrantNotApplicable).HasColumnName("sponsored_support_grant_not_applicable");
        projectConfiguration.Property(e => e.SponsoredSupportGrantPaymentAmount).HasColumnName("sponsored_support_grant_payment_amount");
        projectConfiguration.Property(e => e.SponsoredSupportGrantPaymentForm).HasColumnName("sponsored_support_grant_payment_form");
        projectConfiguration.Property(e => e.SponsoredSupportGrantSendInformation).HasColumnName("sponsored_support_grant_send_information");
        projectConfiguration.Property(e => e.SponsoredSupportGrantType)
            .HasMaxLength(4000)
            .HasColumnName("sponsored_support_grant_type");
        projectConfiguration.Property(e => e.StakeholderKickOffCheckProvisionalConversionDate).HasColumnName("stakeholder_kick_off_check_provisional_conversion_date");
        projectConfiguration.Property(e => e.StakeholderKickOffIntroductoryEmails).HasColumnName("stakeholder_kick_off_introductory_emails");
        projectConfiguration.Property(e => e.StakeholderKickOffLocalAuthorityProforma).HasColumnName("stakeholder_kick_off_local_authority_proforma");
        projectConfiguration.Property(e => e.StakeholderKickOffMeeting).HasColumnName("stakeholder_kick_off_meeting");
        projectConfiguration.Property(e => e.StakeholderKickOffSetupMeeting).HasColumnName("stakeholder_kick_off_setup_meeting");
        projectConfiguration.Property(e => e.SubleasesCleared).HasColumnName("subleases_cleared");
        projectConfiguration.Property(e => e.SubleasesEmailSigned).HasColumnName("subleases_email_signed");
        projectConfiguration.Property(e => e.SubleasesNotApplicable).HasColumnName("subleases_not_applicable");
        projectConfiguration.Property(e => e.SubleasesReceiveSigned).HasColumnName("subleases_receive_signed");
        projectConfiguration.Property(e => e.SubleasesReceived).HasColumnName("subleases_received");
        projectConfiguration.Property(e => e.SubleasesSaveSigned).HasColumnName("subleases_save_signed");
        projectConfiguration.Property(e => e.SubleasesSaved).HasColumnName("subleases_saved");
        projectConfiguration.Property(e => e.SubleasesSigned).HasColumnName("subleases_signed");
        projectConfiguration.Property(e => e.SupplementalFundingAgreementCleared).HasColumnName("supplemental_funding_agreement_cleared");
        projectConfiguration.Property(e => e.SupplementalFundingAgreementReceived).HasColumnName("supplemental_funding_agreement_received");
        projectConfiguration.Property(e => e.SupplementalFundingAgreementSaved).HasColumnName("supplemental_funding_agreement_saved");
        projectConfiguration.Property(e => e.SupplementalFundingAgreementSent).HasColumnName("supplemental_funding_agreement_sent");
        projectConfiguration.Property(e => e.SupplementalFundingAgreementSigned).HasColumnName("supplemental_funding_agreement_signed");
        projectConfiguration.Property(e => e.SupplementalFundingAgreementSignedSecretaryState).HasColumnName("supplemental_funding_agreement_signed_secretary_state");
        projectConfiguration.Property(e => e.TenancyAtWillEmailSigned).HasColumnName("tenancy_at_will_email_signed");
        projectConfiguration.Property(e => e.TenancyAtWillNotApplicable).HasColumnName("tenancy_at_will_not_applicable");
        projectConfiguration.Property(e => e.TenancyAtWillReceiveSigned).HasColumnName("tenancy_at_will_receive_signed");
        projectConfiguration.Property(e => e.TenancyAtWillSaveSigned).HasColumnName("tenancy_at_will_save_signed");
        projectConfiguration.Property(e => e.TrustModificationOrderCleared).HasColumnName("trust_modification_order_cleared");
        projectConfiguration.Property(e => e.TrustModificationOrderNotApplicable).HasColumnName("trust_modification_order_not_applicable");
        projectConfiguration.Property(e => e.TrustModificationOrderReceived).HasColumnName("trust_modification_order_received");
        projectConfiguration.Property(e => e.TrustModificationOrderSaved).HasColumnName("trust_modification_order_saved");
        projectConfiguration.Property(e => e.TrustModificationOrderSentLegal).HasColumnName("trust_modification_order_sent_legal");
        projectConfiguration.Property(e => e.UpdateEsfaUpdate).HasColumnName("update_esfa_update");
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
    }

    private static void ConfigureTransferTasksData(EntityTypeBuilder<TransferTasksData> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("transfer_tasks_data", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new TaskDataId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.ArticlesOfAssociationCleared).HasColumnName("articles_of_association_cleared");
        projectConfiguration.Property(e => e.ArticlesOfAssociationNotApplicable).HasColumnName("articles_of_association_not_applicable");
        projectConfiguration.Property(e => e.ArticlesOfAssociationReceived).HasColumnName("articles_of_association_received");
        projectConfiguration.Property(e => e.ArticlesOfAssociationSaved).HasColumnName("articles_of_association_saved");
        projectConfiguration.Property(e => e.ArticlesOfAssociationSent).HasColumnName("articles_of_association_sent");
        projectConfiguration.Property(e => e.ArticlesOfAssociationSigned).HasColumnName("articles_of_association_signed");
        projectConfiguration.Property(e => e.BankDetailsChangingYesNo)
            .HasDefaultValue(false)
            .HasColumnName("bank_details_changing_yes_no");
        projectConfiguration.Property(e => e.CheckAndConfirmFinancialInformationAcademySurplusDeficit)
            .HasMaxLength(4000)
            .HasColumnName("check_and_confirm_financial_information_academy_surplus_deficit");
        projectConfiguration.Property(e => e.CheckAndConfirmFinancialInformationNotApplicable).HasColumnName("check_and_confirm_financial_information_not_applicable");
        projectConfiguration.Property(e => e.CheckAndConfirmFinancialInformationTrustSurplusDeficit)
            .HasMaxLength(4000)
            .HasColumnName("check_and_confirm_financial_information_trust_surplus_deficit");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementCleared).HasColumnName("church_supplemental_agreement_cleared");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementNotApplicable).HasColumnName("church_supplemental_agreement_not_applicable");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementReceived).HasColumnName("church_supplemental_agreement_received");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSavedAfterSigningBySecretaryState).HasColumnName("church_supplemental_agreement_saved_after_signing_by_secretary_state");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSavedAfterSigningByTrustDiocese).HasColumnName("church_supplemental_agreement_saved_after_signing_by_trust_diocese");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSignedDiocese).HasColumnName("church_supplemental_agreement_signed_diocese");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSignedIncomingTrust).HasColumnName("church_supplemental_agreement_signed_incoming_trust");
        projectConfiguration.Property(e => e.ChurchSupplementalAgreementSignedSecretaryState).HasColumnName("church_supplemental_agreement_signed_secretary_state");
        projectConfiguration.Property(e => e.ClosureOrTransferDeclarationCleared).HasColumnName("closure_or_transfer_declaration_cleared");
        projectConfiguration.Property(e => e.ClosureOrTransferDeclarationNotApplicable).HasColumnName("closure_or_transfer_declaration_not_applicable");
        projectConfiguration.Property(e => e.ClosureOrTransferDeclarationReceived).HasColumnName("closure_or_transfer_declaration_received");
        projectConfiguration.Property(e => e.ClosureOrTransferDeclarationSaved).HasColumnName("closure_or_transfer_declaration_saved");
        projectConfiguration.Property(e => e.ClosureOrTransferDeclarationSent).HasColumnName("closure_or_transfer_declaration_sent");
        projectConfiguration.Property(e => e.CommercialTransferAgreementConfirmAgreed).HasColumnName("commercial_transfer_agreement_confirm_agreed");
        projectConfiguration.Property(e => e.CommercialTransferAgreementConfirmSigned).HasColumnName("commercial_transfer_agreement_confirm_signed");
        projectConfiguration.Property(e => e.CommercialTransferAgreementQuestionsChecked)
            .HasDefaultValue(false)
            .HasColumnName("commercial_transfer_agreement_questions_checked");
        projectConfiguration.Property(e => e.CommercialTransferAgreementQuestionsReceived)
            .HasDefaultValue(false)
            .HasColumnName("commercial_transfer_agreement_questions_received");
        projectConfiguration.Property(e => e.CommercialTransferAgreementSaveConfirmationEmails).HasColumnName("commercial_transfer_agreement_save_confirmation_emails");
        projectConfiguration.Property(e => e.ConditionsMetBaselineSheetApproved).HasColumnName("conditions_met_baseline_sheet_approved");
        projectConfiguration.Property(e => e.ConditionsMetCheckAnyInformationChanged).HasColumnName("conditions_met_check_any_information_changed");
        projectConfiguration.Property(e => e.ConfirmDateAcademyTransferredDateTransferred).HasColumnName("confirm_date_academy_transferred_date_transferred");
        projectConfiguration.Property(e => e.ConfirmIncomingTrustHasCompletedAllActionsEmailed).HasColumnName("confirm_incoming_trust_has_completed_all_actions_emailed");
        projectConfiguration.Property(e => e.ConfirmIncomingTrustHasCompletedAllActionsSaved).HasColumnName("confirm_incoming_trust_has_completed_all_actions_saved");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.DeclarationOfExpenditureCertificateCorrect)
            .HasDefaultValue(false)
            .HasColumnName("declaration_of_expenditure_certificate_correct");
        projectConfiguration.Property(e => e.DeclarationOfExpenditureCertificateDateReceived).HasColumnName("declaration_of_expenditure_certificate_date_received");
        projectConfiguration.Property(e => e.DeclarationOfExpenditureCertificateNotApplicable)
            .HasDefaultValue(false)
            .HasColumnName("declaration_of_expenditure_certificate_not_applicable");
        projectConfiguration.Property(e => e.DeclarationOfExpenditureCertificateSaved)
            .HasDefaultValue(false)
            .HasColumnName("declaration_of_expenditure_certificate_saved");
        projectConfiguration.Property(e => e.DeedOfNovationAndVariationCleared).HasColumnName("deed_of_novation_and_variation_cleared");
        projectConfiguration.Property(e => e.DeedOfNovationAndVariationReceived).HasColumnName("deed_of_novation_and_variation_received");
        projectConfiguration.Property(e => e.DeedOfNovationAndVariationSaveAfterSign).HasColumnName("deed_of_novation_and_variation_save_after_sign");
        projectConfiguration.Property(e => e.DeedOfNovationAndVariationSaved).HasColumnName("deed_of_novation_and_variation_saved");
        projectConfiguration.Property(e => e.DeedOfNovationAndVariationSignedIncomingTrust).HasColumnName("deed_of_novation_and_variation_signed_incoming_trust");
        projectConfiguration.Property(e => e.DeedOfNovationAndVariationSignedOutgoingTrust).HasColumnName("deed_of_novation_and_variation_signed_outgoing_trust");
        projectConfiguration.Property(e => e.DeedOfNovationAndVariationSignedSecretaryState).HasColumnName("deed_of_novation_and_variation_signed_secretary_state");
        projectConfiguration.Property(e => e.DeedOfTerminationForTheMasterFundingAgreementCleared).HasColumnName("deed_of_termination_for_the_master_funding_agreement_cleared");
        projectConfiguration.Property(e => e.DeedOfTerminationForTheMasterFundingAgreementContactFinancialReportingTeam).HasColumnName("deed_of_termination_for_the_master_funding_agreement_contact_financial_reporting_team");
        projectConfiguration.Property(e => e.DeedOfTerminationForTheMasterFundingAgreementNotApplicable).HasColumnName("deed_of_termination_for_the_master_funding_agreement_not_applicable");
        projectConfiguration.Property(e => e.DeedOfTerminationForTheMasterFundingAgreementReceived).HasColumnName("deed_of_termination_for_the_master_funding_agreement_received");
        projectConfiguration.Property(e => e.DeedOfTerminationForTheMasterFundingAgreementSavedAcademyAndOutgoingTrustSharepoint).HasColumnName("deed_of_termination_for_the_master_funding_agreement_saved_academy_and_outgoing_trust_sharepoint");
        projectConfiguration.Property(e => e.DeedOfTerminationForTheMasterFundingAgreementSavedInAcademySharepointFolder).HasColumnName("deed_of_termination_for_the_master_funding_agreement_saved_in_academy_sharepoint_folder");
        projectConfiguration.Property(e => e.DeedOfTerminationForTheMasterFundingAgreementSigned).HasColumnName("deed_of_termination_for_the_master_funding_agreement_signed");
        projectConfiguration.Property(e => e.DeedOfTerminationForTheMasterFundingAgreementSignedSecretaryState).HasColumnName("deed_of_termination_for_the_master_funding_agreement_signed_secretary_state");
        projectConfiguration.Property(e => e.DeedOfVariationCleared).HasColumnName("deed_of_variation_cleared");
        projectConfiguration.Property(e => e.DeedOfVariationNotApplicable).HasColumnName("deed_of_variation_not_applicable");
        projectConfiguration.Property(e => e.DeedOfVariationReceived).HasColumnName("deed_of_variation_received");
        projectConfiguration.Property(e => e.DeedOfVariationSaved).HasColumnName("deed_of_variation_saved");
        projectConfiguration.Property(e => e.DeedOfVariationSent).HasColumnName("deed_of_variation_sent");
        projectConfiguration.Property(e => e.DeedOfVariationSigned).HasColumnName("deed_of_variation_signed");
        projectConfiguration.Property(e => e.DeedOfVariationSignedSecretaryState).HasColumnName("deed_of_variation_signed_secretary_state");
        projectConfiguration.Property(e => e.DeedTerminationChurchAgreementCleared).HasColumnName("deed_termination_church_agreement_cleared");
        projectConfiguration.Property(e => e.DeedTerminationChurchAgreementNotApplicable).HasColumnName("deed_termination_church_agreement_not_applicable");
        projectConfiguration.Property(e => e.DeedTerminationChurchAgreementReceived).HasColumnName("deed_termination_church_agreement_received");
        projectConfiguration.Property(e => e.DeedTerminationChurchAgreementSaved).HasColumnName("deed_termination_church_agreement_saved");
        projectConfiguration.Property(e => e.DeedTerminationChurchAgreementSavedAfterSigningBySecretaryState).HasColumnName("deed_termination_church_agreement_saved_after_signing_by_secretary_state");
        projectConfiguration.Property(e => e.DeedTerminationChurchAgreementSignedDiocese).HasColumnName("deed_termination_church_agreement_signed_diocese");
        projectConfiguration.Property(e => e.DeedTerminationChurchAgreementSignedOutgoingTrust).HasColumnName("deed_termination_church_agreement_signed_outgoing_trust");
        projectConfiguration.Property(e => e.DeedTerminationChurchAgreementSignedSecretaryState).HasColumnName("deed_termination_church_agreement_signed_secretary_state");
        projectConfiguration.Property(e => e.FinancialSafeguardingGovernanceIssues)
            .HasDefaultValue(false)
            .HasColumnName("financial_safeguarding_governance_issues");
        projectConfiguration.Property(e => e.FormMCleared).HasColumnName("form_m_cleared");
        projectConfiguration.Property(e => e.FormMNotApplicable).HasColumnName("form_m_not_applicable");
        projectConfiguration.Property(e => e.FormMReceivedFormM).HasColumnName("form_m_received_form_m");
        projectConfiguration.Property(e => e.FormMReceivedTitlePlans).HasColumnName("form_m_received_title_plans");
        projectConfiguration.Property(e => e.FormMSaved).HasColumnName("form_m_saved");
        projectConfiguration.Property(e => e.FormMSigned).HasColumnName("form_m_signed");
        projectConfiguration.Property(e => e.HandoverMeeting).HasColumnName("handover_meeting");
        projectConfiguration.Property(e => e.HandoverNotApplicable).HasColumnName("handover_not_applicable");
        projectConfiguration.Property(e => e.HandoverNotes).HasColumnName("handover_notes");
        projectConfiguration.Property(e => e.HandoverReview).HasColumnName("handover_review");
        projectConfiguration.Property(e => e.InadequateOfsted)
            .HasDefaultValue(false)
            .HasColumnName("inadequate_ofsted");
        projectConfiguration.Property(e => e.LandConsentLetterDrafted).HasColumnName("land_consent_letter_drafted");
        projectConfiguration.Property(e => e.LandConsentLetterNotApplicable).HasColumnName("land_consent_letter_not_applicable");
        projectConfiguration.Property(e => e.LandConsentLetterSaved).HasColumnName("land_consent_letter_saved");
        projectConfiguration.Property(e => e.LandConsentLetterSent).HasColumnName("land_consent_letter_sent");
        projectConfiguration.Property(e => e.LandConsentLetterSigned).HasColumnName("land_consent_letter_signed");
        projectConfiguration.Property(e => e.MasterFundingAgreementCleared).HasColumnName("master_funding_agreement_cleared");
        projectConfiguration.Property(e => e.MasterFundingAgreementNotApplicable).HasColumnName("master_funding_agreement_not_applicable");
        projectConfiguration.Property(e => e.MasterFundingAgreementReceived).HasColumnName("master_funding_agreement_received");
        projectConfiguration.Property(e => e.MasterFundingAgreementSaved).HasColumnName("master_funding_agreement_saved");
        projectConfiguration.Property(e => e.MasterFundingAgreementSigned).HasColumnName("master_funding_agreement_signed");
        projectConfiguration.Property(e => e.MasterFundingAgreementSignedSecretaryState).HasColumnName("master_funding_agreement_signed_secretary_state");
        projectConfiguration.Property(e => e.OutgoingTrustToClose)
            .HasDefaultValue(false)
            .HasColumnName("outgoing_trust_to_close");
        projectConfiguration.Property(e => e.RedactAndSendDocumentsRedact).HasColumnName("redact_and_send_documents_redact");
        projectConfiguration.Property(e => e.RedactAndSendDocumentsSaved).HasColumnName("redact_and_send_documents_saved");
        projectConfiguration.Property(e => e.RedactAndSendDocumentsSendToEsfa).HasColumnName("redact_and_send_documents_send_to_esfa");
        projectConfiguration.Property(e => e.RedactAndSendDocumentsSendToFundingTeam).HasColumnName("redact_and_send_documents_send_to_funding_team");
        projectConfiguration.Property(e => e.RedactAndSendDocumentsSendToSolicitors).HasColumnName("redact_and_send_documents_send_to_solicitors");
        projectConfiguration.Property(e => e.RequestNewUrnAndRecordComplete).HasColumnName("request_new_urn_and_record_complete");
        projectConfiguration.Property(e => e.RequestNewUrnAndRecordGive).HasColumnName("request_new_urn_and_record_give");
        projectConfiguration.Property(e => e.RequestNewUrnAndRecordNotApplicable).HasColumnName("request_new_urn_and_record_not_applicable");
        projectConfiguration.Property(e => e.RequestNewUrnAndRecordReceive).HasColumnName("request_new_urn_and_record_receive");
        projectConfiguration.Property(e => e.RpaPolicyConfirm).HasColumnName("rpa_policy_confirm");
        projectConfiguration.Property(e => e.SponsoredSupportGrantNotApplicable)
            .HasDefaultValue(false)
            .HasColumnName("sponsored_support_grant_not_applicable");
        projectConfiguration.Property(e => e.SponsoredSupportGrantType)
            .HasMaxLength(4000)
            .HasColumnName("sponsored_support_grant_type");
        projectConfiguration.Property(e => e.StakeholderKickOffIntroductoryEmails).HasColumnName("stakeholder_kick_off_introductory_emails");
        projectConfiguration.Property(e => e.StakeholderKickOffMeeting).HasColumnName("stakeholder_kick_off_meeting");
        projectConfiguration.Property(e => e.StakeholderKickOffSetupMeeting).HasColumnName("stakeholder_kick_off_setup_meeting");
        projectConfiguration.Property(e => e.SupplementalFundingAgreementCleared).HasColumnName("supplemental_funding_agreement_cleared");
        projectConfiguration.Property(e => e.SupplementalFundingAgreementReceived).HasColumnName("supplemental_funding_agreement_received");
        projectConfiguration.Property(e => e.SupplementalFundingAgreementSaved).HasColumnName("supplemental_funding_agreement_saved");
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
    }

    private static void ConfigureDaoRevocation(EntityTypeBuilder<DaoRevocation> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("dao_revocations", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new DaoRevocationId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.DateOfDecision).HasColumnName("date_of_decision");
        projectConfiguration.Property(e => e.DecisionMakersName)
            .HasMaxLength(4000)
            .HasColumnName("decision_makers_name");
        projectConfiguration.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .HasConversion(
                v => v!.Value,
                v => new ProjectId(v));
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
    }

    private static void ConfigureDaoRevocationReason(EntityTypeBuilder<DaoRevocationReason> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("dao_revocation_reasons", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new DaoRevocationReasonId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.DaoRevocationId)
            .HasColumnName("dao_revocation_id")
            .HasConversion(
                v => v!.Value,
                v => new DaoRevocationId(v));
        projectConfiguration.Property(e => e.ReasonType)
            .HasMaxLength(4000)
            .HasColumnName("reason_type");
    }

    private static void ConfigureGiasEstablishment(EntityTypeBuilder<GiasEstablishment> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.HasAlternateKey(e => e.Urn)
            .HasName("AK_GiasEstablishments_Urn");

        projectConfiguration.ToTable("gias_establishments", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new GiasEstablishmentId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.AddressAdditional)
            .HasMaxLength(4000)
            .HasColumnName("address_additional");
        projectConfiguration.Property(e => e.AddressCounty)
            .HasMaxLength(4000)
            .HasColumnName("address_county");
        projectConfiguration.Property(e => e.AddressLocality)
            .HasMaxLength(4000)
            .HasColumnName("address_locality");
        projectConfiguration.Property(e => e.AddressPostcode)
            .HasMaxLength(4000)
            .HasColumnName("address_postcode");
        projectConfiguration.Property(e => e.AddressStreet)
            .HasMaxLength(4000)
            .HasColumnName("address_street");
        projectConfiguration.Property(e => e.AddressTown)
            .HasMaxLength(4000)
            .HasColumnName("address_town");
        projectConfiguration.Property(e => e.AgeRangeLower).HasColumnName("age_range_lower");
        projectConfiguration.Property(e => e.AgeRangeUpper).HasColumnName("age_range_upper");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.DioceseCode)
            .HasMaxLength(4000)
            .HasColumnName("diocese_code");
        projectConfiguration.Property(e => e.DioceseName)
            .HasMaxLength(4000)
            .HasColumnName("diocese_name");
        projectConfiguration.Property(e => e.EstablishmentNumber)
            .HasMaxLength(4000)
            .HasColumnName("establishment_number");
        projectConfiguration.Property(e => e.LocalAuthorityCode)
            .HasMaxLength(4000)
            .HasColumnName("local_authority_code");
        projectConfiguration.Property(e => e.LocalAuthorityName)
            .HasMaxLength(4000)
            .HasColumnName("local_authority_name");
        projectConfiguration.Property(e => e.Name)
            .HasMaxLength(4000)
            .HasColumnName("name");
        projectConfiguration.Property(e => e.OpenDate).HasColumnName("open_date");
        projectConfiguration.Property(e => e.ParliamentaryConstituencyCode)
            .HasMaxLength(4000)
            .HasColumnName("parliamentary_constituency_code");
        projectConfiguration.Property(e => e.ParliamentaryConstituencyName)
            .HasMaxLength(4000)
            .HasColumnName("parliamentary_constituency_name");
        projectConfiguration.Property(e => e.PhaseCode)
            .HasMaxLength(4000)
            .HasColumnName("phase_code");
        projectConfiguration.Property(e => e.PhaseName)
            .HasMaxLength(4000)
            .HasColumnName("phase_name");
        projectConfiguration.Property(e => e.RegionCode)
            .HasMaxLength(4000)
            .HasColumnName("region_code");
        projectConfiguration.Property(e => e.RegionName)
            .HasMaxLength(4000)
            .HasColumnName("region_name");
        projectConfiguration.Property(e => e.StatusName)
            .HasMaxLength(4000)
            .HasColumnName("status_name");
        projectConfiguration.Property(e => e.TypeCode)
            .HasMaxLength(4000)
            .HasColumnName("type_code");
        projectConfiguration.Property(e => e.TypeName)
            .HasMaxLength(4000)
            .HasColumnName("type_name");
        projectConfiguration.Property(e => e.Ukprn)
            .HasColumnName("ukprn")
            .HasConversion(
                v => v!.Value,
                v => new Ukprn(v));
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
        projectConfiguration.Property(e => e.Url)
            .HasMaxLength(4000)
            .HasColumnName("url");
        projectConfiguration.Property(e => e.Urn)
            .HasColumnName("urn")
            .HasConversion(
                v => v!.Value,
                v => new Urn(v));

        projectConfiguration.HasIndex(e => e.Urn)
            .IsUnique()
            .HasDatabaseName("IX_GiasEstablishments_Urn");
    }

    private static void ConfigureGiasGroup(EntityTypeBuilder<GiasGroup> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("gias_groups", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new GiasGroupId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.AddressAdditional)
            .HasMaxLength(4000)
            .HasColumnName("address_additional");
        projectConfiguration.Property(e => e.AddressCounty)
            .HasMaxLength(4000)
            .HasColumnName("address_county");
        projectConfiguration.Property(e => e.AddressLocality)
            .HasMaxLength(4000)
            .HasColumnName("address_locality");
        projectConfiguration.Property(e => e.AddressPostcode)
            .HasMaxLength(4000)
            .HasColumnName("address_postcode");
        projectConfiguration.Property(e => e.AddressStreet)
            .HasMaxLength(4000)
            .HasColumnName("address_street");
        projectConfiguration.Property(e => e.AddressTown)
            .HasMaxLength(4000)
            .HasColumnName("address_town");
        projectConfiguration.Property(e => e.CompaniesHouseNumber)
            .HasMaxLength(4000)
            .HasColumnName("companies_house_number");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.GroupIdentifier)
            .HasMaxLength(4000)
            .HasColumnName("group_identifier");
        projectConfiguration.Property(e => e.OriginalName)
            .HasMaxLength(4000)
            .HasColumnName("original_name");
        projectConfiguration.Property(e => e.Ukprn).HasColumnName("ukprn");
        projectConfiguration.Property(e => e.UniqueGroupIdentifier).HasColumnName("unique_group_identifier");
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
    }

    private static void ConfigureKeyContact(EntityTypeBuilder<KeyContact> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("key_contacts", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new KeyContactId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.ChairOfGovernorsId)
            .HasColumnName("chair_of_governors_id")
            .HasConversion(
                v => v!.Value,
                v => new ContactId(v));
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.HeadteacherId)
            .HasColumnName("headteacher_id")
            .HasConversion(
                v => v!.Value,
                v => new ContactId(v));
        projectConfiguration.Property(e => e.IncomingTrustCeoId)
            .HasColumnName("incoming_trust_ceo_id")
            .HasConversion(
                v => v!.Value,
                v => new ContactId(v));
        projectConfiguration.Property(e => e.OutgoingTrustCeoId)
            .HasColumnName("outgoing_trust_ceo_id")
            .HasConversion(
                v => v!.Value,
                v => new ContactId(v));
        projectConfiguration.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .HasConversion(
                v => v!.Value,
                v => new ProjectId(v));
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
    }

    private static void ConfigureLocalAuthority(EntityTypeBuilder<LocalAuthority> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("local_authorities", "complete");

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new LocalAuthorityId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.Address1)
            .HasMaxLength(4000)
            .HasColumnName("address_1");
        projectConfiguration.Property(e => e.Address2)
            .HasMaxLength(4000)
            .HasColumnName("address_2");
        projectConfiguration.Property(e => e.Address3)
            .HasMaxLength(4000)
            .HasColumnName("address_3");
        projectConfiguration.Property(e => e.AddressCounty)
            .HasMaxLength(4000)
            .HasColumnName("address_county");
        projectConfiguration.Property(e => e.AddressPostcode)
            .HasMaxLength(4000)
            .HasColumnName("address_postcode");
        projectConfiguration.Property(e => e.AddressTown)
            .HasMaxLength(4000)
            .HasColumnName("address_town");
        projectConfiguration.Property(e => e.Code)
            .HasMaxLength(4000)
            .HasColumnName("code");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.Name)
            .HasMaxLength(4000)
            .HasColumnName("name");
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
    }

    private static void ConfigureNote(EntityTypeBuilder<Note> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.HasOne(e => e.Project)
            .WithMany(e => e.Notes)
            .HasForeignKey(e => e.ProjectId);

        projectConfiguration.ToTable("notes", "complete");

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v.Value,
                v => new NoteId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.Body).HasColumnName("body");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.NotableId).HasColumnName("notable_id");
        projectConfiguration.Property(e => e.NotableType)
            .HasMaxLength(4000)
            .HasColumnName("notable_type");
        projectConfiguration.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .HasConversion(
                v => v.Value,
                v => new ProjectId(v));
        projectConfiguration.Property(e => e.TaskIdentifier)
            .HasMaxLength(4000)
            .HasColumnName("task_identifier");
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
        projectConfiguration.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                v => v.Value,
                v => new UserId(v));

        projectConfiguration.HasOne(d => d.Project).WithMany(p => p.Notes)
            .HasForeignKey(d => d.ProjectId)
            .HasConstraintName("fk_rails_99e097b079");

        projectConfiguration.HasOne(d => d.User).WithMany(p => p.Notes)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("fk_rails_7f2323ad43");
    }

    private static void ConfigureProjectGroup(EntityTypeBuilder<ProjectGroup> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("project_groups", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new ProjectGroupId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.GroupIdentifier)
            .HasMaxLength(4000)
            .HasColumnName("group_identifier");
        projectConfiguration.Property(e => e.TrustUkprn)
            .HasColumnName("trust_ukprn")
            .HasConversion(
                v => v!.Value,
                v => new Ukprn(v));
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
    }

    private static void ConfigureSignificantDateHistory(EntityTypeBuilder<SignificantDateHistory> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("significant_date_histories", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new SignificantDateHistoryId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.PreviousDate).HasColumnName("previous_date");
        projectConfiguration.Property(e => e.ProjectId)
            .HasColumnName("project_id")
            .HasConversion(
                v => v!.Value,
                v => new ProjectId(v));
        projectConfiguration.Property(e => e.RevisedDate).HasColumnName("revised_date");
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
        projectConfiguration.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                v => v!.Value,
                v => new UserId(v));
    }

    private static void ConfigureSignificantDateHistoryReason(EntityTypeBuilder<SignificantDateHistoryReason> projectConfiguration)
    {
        projectConfiguration.HasKey(e => e.Id);

        projectConfiguration.ToTable("significant_date_history_reasons", DefaultSchema);

        projectConfiguration.Property(e => e.Id)
            .HasConversion(
                v => v!.Value,
                v => new SignificantDateHistoryReasonId(v))
            .HasColumnName("id");
        projectConfiguration.Property(e => e.CreatedAt)
            .HasPrecision(6)
            .HasColumnName("created_at");
        projectConfiguration.Property(e => e.ReasonType)
            .HasMaxLength(4000)
            .HasColumnName("reason_type");
        projectConfiguration.Property(e => e.SignificantDateHistoryId)
            .HasColumnName("significant_date_history_id")
            .HasConversion(
                v => v!.Value,
                v => new SignificantDateHistoryId(v));
        projectConfiguration.Property(e => e.UpdatedAt)
            .HasPrecision(6)
            .HasColumnName("updated_at");
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
