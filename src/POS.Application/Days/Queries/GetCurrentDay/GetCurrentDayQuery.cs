using MediatR;
using POS.Application.Common;

namespace POS.Application.Days.Queries.GetCurrentDay;

public record GetCurrentDayQuery : IRequest<DayReadDto?>;
