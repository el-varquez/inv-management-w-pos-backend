using MediatR;
using POS.Application.Common;
using POS.Application.Common.Models;
using POS.Application.Utang.Commands.CreateSuki;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Queries.GetSukis;

public class GetSukisQueryHandler
    : IRequestHandler<GetSukisQuery, PagedResult<SukiDto>>
{
    private readonly IUtangRepository _utang;
    private readonly IStoreSettingsRepository _settings;

    public GetSukisQueryHandler(IUtangRepository utang, IStoreSettingsRepository settings)
    {
        _utang = utang;
        _settings = settings;
    }

    public async Task<PagedResult<SukiDto>> Handle(
        GetSukisQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var (rows, total) = await _utang.GetSukisPagedAsync(
            request.Term, page, pageSize, ct);
        var reminderDays = (await _settings.GetAsync(ct))?.UtangReminderDays ?? 7;
        var today = DateTime.Now;

        var dtos = rows
            .Select(r =>
            {
                var reminder = UtangReminder.Of(r.Invoices, r.Payments, today, reminderDays);
                return new SukiDto(
                    r.Suki.Id, r.Suki.Name, r.Suki.Phone, r.Balance,
                    reminder.DebtSince, reminder.LastPaidAt,
                    reminder.DaysSincePayment, reminder.PaymentOverdue);
            })
            .ToList();

        return new PagedResult<SukiDto>(dtos, page, pageSize, total);
    }
}
