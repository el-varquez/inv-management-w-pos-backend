using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Profile.Queries.GetProfile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileAccountDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(ICurrentUser currentUser, IUserRepository userRepository)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task<ProfileAccountDto> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(_currentUser.Id, ct)
            ?? throw new NotFoundException("User", _currentUser.Id);

        return new ProfileAccountDto(user.Id, user.Name, user.Email, user.Role);
    }
}
