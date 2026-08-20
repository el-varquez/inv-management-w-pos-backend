using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Days.Commands.ReopenDay;

public class ReopenDayCommandHandler : IRequestHandler<ReopenDayCommand>
{
    private readonly IBusinessDayRepository _days;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ReopenDayCommandHandler(
        IBusinessDayRepository days,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _days = days;
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReopenDayCommand request, CancellationToken ct)
    {
        var day = await _days.GetByIdAsync(request.DayId, ct)
            ?? throw new NotFoundException("Day", request.DayId);

        var (recent, _) = await _days.GetPagedAsync(1, 1, ct);
        if (recent.FirstOrDefault()?.Id != day.Id)
            throw new DomainException("Only the most recent day can be reopened.");

        if (day.Status != DayStatus.Closed)
            throw new DomainException($"Day #{day.Number} is still open — there is no Z read to undo.");

        if (day.OpenedAt.ToLocalTime().Date != DateTime.UtcNow.ToLocalTime().Date)
            throw new DomainException(
                $"Day #{day.Number} belongs to a previous date — a Z read can only be undone on the day it happened.");

        var admin = await _users.GetByUsernameAsync(request.Username.Trim().ToLower(), ct);
        if (admin is null
            || admin.Role != "Admin"
            || !admin.IsActive
            || string.IsNullOrEmpty(admin.PasswordHash)
            || !_passwordHasher.Verify(request.Password, admin.PasswordHash))
            throw new DomainException("Those credentials don't belong to an active admin account.");

        var now = DateTime.UtcNow;
        day.Snapshot = null;
        day.Status = DayStatus.Open;
        day.ClosedAt = null;
        day.ClosedBy = null;
        day.ClosedLate = false;
        day.ReopenedAt = now;
        day.ReopenedBy = admin.Id;
        day.ReopenReason = request.Reason.Trim();
        day.UpdatedAt = now;

        await _days.UpdateAsync(day, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
