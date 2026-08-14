using MediatR;
using POS.Domain.Entities;
using POS.Domain.Interfaces;

namespace POS.Application.Settings.Commands.UpdateStoreSettings;

public class UpdateStoreSettingsCommandHandler : IRequestHandler<UpdateStoreSettingsCommand>
{
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStoreSettingsCommandHandler(IStoreSettingsRepository settings, IUnitOfWork unitOfWork)
    {
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateStoreSettingsCommand request, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct);
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

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
