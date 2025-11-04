using AutoMapper;
using Dfe.Complete.Application.Common.Models;
using Dfe.Complete.Application.Projects.Models;
using Dfe.Complete.Domain.Entities;
using Dfe.Complete.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dfe.Complete.Application.Users.Queries.GetUser;

public record GetUserByOidQuery(string ObjectId) : IRequest<Result<UserDto?>>;

public class GetUserByOidQueryHandler(ICompleteRepository<User> userRepository, IMapper mapper, ILogger<GetUserByOidQueryHandler> logger) : IRequestHandler<GetUserByOidQuery, Result<UserDto?>>
{
    public async Task<Result<UserDto?>> Handle(GetUserByOidQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userRepository.FindAsync(u =>
                u.EntraUserObjectId == request.ObjectId
                && u.DeactivatedAt == null, cancellationToken);

            var userDto = mapper.Map<UserDto?>(user);

            return Result<UserDto?>.Success(userDto);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception for {Name} Request - {@Request}", nameof(GetUserByOidQueryHandler), request);
            return Result<UserDto?>.Failure(e.Message);
        }
    }
}