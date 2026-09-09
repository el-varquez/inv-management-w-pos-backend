using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.UpdateSuki;

public class UpdateSukiCommandHandler : IRequestHandler<UpdateSukiCommand>
{
    private readonly IUtangRepository _utang;
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSukiCommandHandler(
        IUtangRepository utang,
        IStoreSettingsRepository settings,
        IUnitOfWork unitOfWork)
    {
        _utang = utang;
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateSukiCommand request, CancellationToken ct)
    {
        await UtangGate.RequireOnAsync(_settings, ct);

        var suki = await _utang.GetSukiByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Suki", request.Id);

        suki.Name = request.Name.Trim();
        suki.Phone = string.IsNullOrWhiteSpace(request.Phone)
            ? null
            : request.Phone.Trim();

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
