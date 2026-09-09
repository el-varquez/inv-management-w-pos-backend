using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Settings.Commands.SetAcceptUtang;

public class SetAcceptUtangCommandHandler : IRequestHandler<SetAcceptUtangCommand>
{
    private readonly IStoreSettingsRepository _settings;
    private readonly IUtangRepository _utang;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public SetAcceptUtangCommandHandler(
        IStoreSettingsRepository settings,
        IUtangRepository utang,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _settings = settings;
        _utang = utang;
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetAcceptUtangCommand request, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
        if (settings is null)
        {
            settings = new StoreSettings();
            await _settings.AddAsync(settings, ct);
        }

        if (!request.Accept)
        {
            var owed = (await _utang.GetAllSukiBalancesAsync(ct))
                .Where(b => b.Balance > 0m)
                .Sum(b => b.Balance);
            if (owed > 0m)
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
                    throw new DomainException(
                        "Sukis still owe money — confirm with admin credentials to turn utang off.");

                var admin = await _users.GetByUsernameAsync(request.Username.Trim().ToLower(), ct);
                if (admin is null
                    || admin.Role != "Admin"
                    || !admin.IsActive
                    || string.IsNullOrEmpty(admin.PasswordHash)
                    || !_passwordHasher.Verify(request.Password, admin.PasswordHash))
                    throw new DomainException("Those credentials don't belong to an active admin account.");
            }
        }

        settings.AcceptUtang = request.Accept;
        settings.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
