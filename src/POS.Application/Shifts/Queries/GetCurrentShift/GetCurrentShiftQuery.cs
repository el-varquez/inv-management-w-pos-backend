using MediatR;
using POS.Application.Common;

namespace POS.Application.Shifts.Queries.GetCurrentShift;

public record GetCurrentShiftQuery : IRequest<ShiftReadDto?>;
