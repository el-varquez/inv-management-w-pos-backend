using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.DeleteSuki;

public class DeleteSukiCommandHandler : IRequestHandler<DeleteSukiCommand>
{
    private readonly IUtangRepository _utang;
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSukiCommandHandler(
        IUtangRepository utang,
        IStoreSettingsRepository settings,
        IUnitOfWork unitOfWork)
    {
        _utang = utang;
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSukiCommand request, CancellationToken ct)
    {
        await UtangGate.RequireOnAsync(_settings, ct);

        var suki = await _utang.GetSukiByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Suki", request.Id);

        if (await _utang.HasLedgerHistoryAsync(suki.Id, ct))
            throw new DomainException(
                $"{suki.Name} has ledger history and can't be deleted.");

        await _utang.DeleteSukiAsync(suki.Id, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
