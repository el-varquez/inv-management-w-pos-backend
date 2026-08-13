using MediatR;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Queries.GetCashiers;

public class GetCashiersQueryHandler : IRequestHandler<GetCashiersQuery, CashierListDto>
{
    private readonly IUserRepository _userRepository;

    public GetCashiersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<CashierListDto> Handle(GetCashiersQuery request, CancellationToken ct)
    {
        var cashiers = await _userRepository.GetCashiersAsync(ct);

        var dtos = cashiers
            .Select(c => new CashierDto(c.Id, c.Name, c.Email, c.IsActive, c.CreatedAt))
            .ToList();

        var activeCount = dtos.Count(c => c.IsActive);

        return new CashierListDto(dtos, activeCount);
    }
}
