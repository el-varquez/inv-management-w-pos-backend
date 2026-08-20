using MediatR;
using POS.Domain.Entities;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Settings.Commands.UpdateStoreSettings;

public class UpdateStoreSettingsCommandHandler : IRequestHandler<UpdateStoreSettingsCommand>
{
    private readonly IStoreSettingsRepository _settings;
    private readonly IShiftRepository _shifts;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStoreSettingsCommandHandler(
        IStoreSettingsRepository settings, IShiftRepository shifts, IUnitOfWork unitOfWork)
    {
        _settings = settings;
        _shifts = shifts;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateStoreSettingsCommand request, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);

        if (request.TrackEWalletFloat != (settings?.TrackEWalletFloat ?? false))
        {
            var open = await _shifts.GetOpenAsync(ct);
            if (open is not null)
                throw new DomainException(
                    $"Shift #{open.Number} is still open — close it with an X read before changing e-wallet tracking.");
        }

        if (settings is null)
        {
            settings = new StoreSettings();
            await _settings.AddAsync(settings, ct);
        }

        settings.StoreName = request.StoreName.Trim();
        settings.Address = request.Address.Trim();
        settings.ReceiptFooter = request.ReceiptFooter.Trim();
        settings.AcceptUtang = request.AcceptUtang;
        settings.DefaultUtangMarkup = request.DefaultUtangMarkup;
        settings.TrackEWalletFloat = request.TrackEWalletFloat;
        settings.EWalletFeeItemId = request.EWalletFeeItemId;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
