using MediatR;
using POS.Application.Common;

namespace POS.Application.Shifts.Queries.GetShiftRead;

public record GetShiftReadQuery(Guid ShiftId) : IRequest<ShiftReadDto>;
