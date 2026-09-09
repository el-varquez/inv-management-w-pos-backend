using MediatR;
using POS.Application.Common;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Queries.GetSukiLedger;

public class GetSukiLedgerQueryHandler
    : IRequestHandler<GetSukiLedgerQuery, SukiLedgerDto>
{
    private readonly IUtangRepository _utang;
    private readonly IInvoiceRepository _invoices;
    private readonly IStoreSettingsRepository _settings;

    public GetSukiLedgerQueryHandler(
        IUtangRepository utang,
        IInvoiceRepository invoices,
        IStoreSettingsRepository settings)
    {
        _utang = utang;
        _invoices = invoices;
        _settings = settings;
    }

    public async Task<SukiLedgerDto> Handle(
        GetSukiLedgerQuery request, CancellationToken ct)
    {
        var suki = await _utang.GetSukiByIdAsync(request.SukiId, ct)
            ?? throw new NotFoundException("Suki", request.SukiId);

        var invoices = await _invoices.GetBySukiAsync(suki.Id, ct);
        var payments = await _utang.GetPaymentsBySukiAsync(suki.Id, ct);
        var reminderDays = (await _settings.GetAsync(ct))?.UtangReminderDays ?? 7;
        var reminder = UtangReminder.Of(invoices, payments, DateTime.Now, reminderDays);

        var liveInvoices = invoices.Where(i => !i.IsVoided).ToList();
        var livePaid = payments.Where(p => !p.IsVoided).Sum(p => p.Amount);

        var entries = invoices
            .Select(i => new UtangLedgerEntryDto(
                i.Id, "Charge", i.Total, i.MarkupTotal, i.Id, i.InvoiceNumber,
                null, i.IsVoided, null, i.CreatedAt))
            .Concat(payments.Select(p => new UtangLedgerEntryDto(
                p.Id, "Payment", p.Amount, 0m, null, null,
                p.Note ?? "Payment received", p.IsVoided, p.EditedFrom, p.CreatedAt)))
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .ToList();

        return new SukiLedgerDto(
            suki.Id, suki.Name, suki.Phone,
            liveInvoices.Sum(i => i.Total) - livePaid,
            liveInvoices.Sum(i => i.MarkupTotal),
            reminder.DebtSince, reminder.LastPaidAt,
            reminder.DaysSincePayment, reminder.PaymentOverdue,
            entries);
    }
}
