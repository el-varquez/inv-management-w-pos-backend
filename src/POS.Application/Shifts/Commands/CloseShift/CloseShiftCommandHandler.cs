using MediatR;
using POS.Application.Common;
using POS.Application.Common.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Shifts.Commands.CloseShift;

public class CloseShiftCommandHandler : IRequestHandler<CloseShiftCommand>
{
    private readonly IShiftRepository _shifts;
    private readonly ISaleRepository _sales;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IPaymentMethodRepository _methods;

    public CloseShiftCommandHandler(
        IShiftRepository shifts,
        ISaleRepository sales,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IPaymentMethodRepository methods)
    {
        _shifts = shifts;
        _sales = sales;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _methods = methods;
    }

    public async Task Handle(CloseShiftCommand request, CancellationToken ct)
    {
        var shift = await _shifts.GetByIdAsync(request.ShiftId, ct)
            ?? throw new NotFoundException("Shift", request.ShiftId);

        if (shift.Status == ShiftStatus.Closed)
            throw new DomainException($"Shift #{shift.Number} is already closed.");

        var sales = await _sales.GetByShiftAsync(shift.Id, ct);
        var movements = await _shifts.GetMovementsAsync(shift.Id, ct);
        var methods = await _methods.GetAllAsync(ct);

        var movementsNet = movements.Where(m => !m.IsVoided).Sum(m => m.Amount);
        var methodSales = methods
            .Where(m => m.IsActive || sales.Any(t => t.PaymentMethodId == m.Id))
            .Select(m => new MethodSalesDto(m.Id, m.Name,
                PaidSales.Net(sales.Where(t => t.PaymentMethodId == m.Id))))
            .ToList();
        var expectedCash = shift.StartingCash + PaidSales.Net(sales) + movementsNet;

        var closedAt = DateTime.UtcNow;

        var snapshot = new XReadSnapshot
        {
            NetSales = PaidSales.Net(sales),
            TransactionCount = PaidSales.Count(sales),
            Refunds = PaidSales.Refunds(sales),
            RefundCount = PaidSales.RefundCount(sales),
            DrawerMovementsNet = movementsNet,
            ExpectedCash = expectedCash,
            CountedCash = request.CountedCash,
            CashVariance = request.CountedCash - expectedCash
        };

        shift.Snapshot = snapshot;
        shift.Status = ShiftStatus.Closed;
        shift.ClosedAt = closedAt;
        shift.ClosedBy = _currentUser.Id;
        shift.UpdatedAt = closedAt;
        var methodSalesRows = methodSales
            .Select(m => new ShiftMethodSales
            {
                ShiftId = shift.Id,
                PaymentMethodId = m.PaymentMethodId,
                MethodName = m.Name,
                Amount = m.Amount
            })
            .ToList();

        await _shifts.UpdateAsync(shift, ct);
        await _shifts.AddMethodSalesAsync(methodSalesRows, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
