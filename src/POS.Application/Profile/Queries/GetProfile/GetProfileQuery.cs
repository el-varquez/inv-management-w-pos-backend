using MediatR;

namespace POS.Application.Profile.Queries.GetProfile;

public record GetProfileQuery() : IRequest<ProfileAccountDto>;

public record ProfileAccountDto(Guid Id, string Name, string Email, string Role);
