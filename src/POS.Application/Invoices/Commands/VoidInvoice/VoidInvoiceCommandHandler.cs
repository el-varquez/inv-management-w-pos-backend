using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Events;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Invoices.Commands.VoidInvoice;

public class VoidInvoiceCommandHandler : IRequestHandler<VoidInvoiceCommand>
{
    private readonly IInvoiceRepository _invoices;
    private readonly IStoreSettingsRepository _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public VoidInvoiceCommandHandler(
        IInvoiceRepository invoices,
        IStoreSettingsRepository settings,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _invoices = invoices;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(VoidInvoiceCommand request, CancellationToken ct)
    {
        await UtangGate.RequireOnAsync(_settings, ct);

        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, ct)
            ?? throw new NotFoundException("Invoice", request.InvoiceId);

        if (invoice.IsVoided)
            throw new DomainException("This invoice is already voided.");

        var now = DateTime.UtcNow;
        invoice.IsVoided = true;
        invoice.VoidedAt = now;
        invoice.VoidedBy = _currentUser.Id;
        invoice.UpdatedAt = now;
        invoice.AddDomainEvent(new InvoiceVoidedEvent(
            invoice.Id,
            invoice.Items.Select(i => (i.ItemId, i.Quantity)).ToList(),
            _currentUser.Id));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
