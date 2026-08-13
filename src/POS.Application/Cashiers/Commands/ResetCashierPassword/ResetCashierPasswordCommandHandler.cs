using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Cashiers.Commands.ResetCashierPassword;

public class ResetCashierPasswordCommandHandler : IRequestHandler<ResetCashierPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ResetCashierPasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ResetCashierPasswordCommand request, CancellationToken ct)
    {
        var cashier = await _userRepository.GetCashierByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Cashier", request.Id);

        cashier.PasswordHash = _passwordHasher.Hash(request.Password);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
