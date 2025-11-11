using AutoFixture;
using Dfe.Complete.Domain.Entities;
using Dfe.Complete.Domain.ValueObjects;

namespace Dfe.Complete.Tests.Common.Customizations.Models;

public class KeyContactCustomization : ICustomization
{
    public KeyContactId? Id { get; set; }
    public ProjectId? ProjectId { get; set; }
    public ContactId? ChairOfGovernorsId { get; set; }
    public ContactId? IncomingTrustCeoId { get; set; }
    public ContactId? OutgoingTrustCeoId { get; set; }
    public ContactId? HeadteacherId { get; set; }

    public void Customize(IFixture fixture)
    {
        fixture.Customize<KeyContactId>(c =>
            c.FromFactory<Guid>(guid => new KeyContactId(guid)));

        fixture.Customize<KeyContact>(composer => composer
                .With(c => c.Id, fixture.Create<KeyContactId>())
                .With(c => c.ChairOfGovernorsId, ChairOfGovernorsId ?? fixture.Create<ContactId>())
                .With(c => c.IncomingTrustCeoId, IncomingTrustCeoId ?? fixture.Create<ContactId>())
                .With(c => c.OutgoingTrustCeoId, OutgoingTrustCeoId ?? fixture.Create<ContactId>())
                .With(c => c.HeadteacherId, HeadteacherId ?? fixture.Create<ContactId>())
                .With(c => c.CreatedAt, DateTime.UtcNow.AddDays(-10))
                .With(c => c.UpdatedAt, DateTime.UtcNow)
                .With(c => c.ProjectId, ProjectId)
        );
    }
}