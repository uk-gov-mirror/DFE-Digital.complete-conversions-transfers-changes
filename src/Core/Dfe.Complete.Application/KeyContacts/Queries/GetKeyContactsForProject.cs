using AutoMapper;
using Dfe.Complete.Application.Common.Models;
using Dfe.Complete.Application.KeyContacts.Models;
using Dfe.Complete.Application.Projects.Interfaces;
using Dfe.Complete.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dfe.Complete.Application.KeyContacts.Queries;

public record GetKeyContactsForProjectQuery(ProjectId ProjectId) : IRequest<Result<KeyContactDto?>>;

public class GetKeyContactsForProjectQueryHandler(IKeyContactReadRepository keyContactsRepository, IMapper mapper)
    : IRequestHandler<GetKeyContactsForProjectQuery, Result<KeyContactDto?>>
{
    public async Task<Result<KeyContactDto?>> Handle(GetKeyContactsForProjectQuery request, CancellationToken cancellationToken)
    {
        var keyContacts = await keyContactsRepository.KeyContacts.FirstOrDefaultAsync(contact => contact.ProjectId != null && contact.ProjectId == request.ProjectId,
                cancellationToken);

        return Result<KeyContactDto?>.Success(mapper.Map<KeyContactDto>(keyContacts));
    }
}