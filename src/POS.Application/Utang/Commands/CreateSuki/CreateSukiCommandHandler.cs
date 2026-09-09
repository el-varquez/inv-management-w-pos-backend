using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.CreateSuki;

public class CreateSukiCommandHandler : IRequestHandler<CreateSukiCommand, SukiDto>
{
    private readonly IUtangRepository _utang;
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateSukiCommandHandler(
        IUtangRepository utang,
        IStoreSettingsRepository settings,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _utang = utang;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<SukiDto> Handle(CreateSukiCommand request, CancellationToken ct)
    {
        await UtangGate.RequireOnAsync(_settings, ct);

        var suki = new Suki
        {
            Name = request.Name.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim(),
            CreatedBy = _currentUser.Id
        };

        await _utang.AddSukiAsync(suki, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new SukiDto(suki.Id, suki.Name, suki.Phone, 0m, null, null, null, false);
    }
}
