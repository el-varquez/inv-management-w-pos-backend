using MediatR;
using POS.Application.Common;

namespace POS.Application.Days.Queries.GetDayRead;

public record GetDayReadQuery(Guid DayId) : IRequest<DayReadDto>;
