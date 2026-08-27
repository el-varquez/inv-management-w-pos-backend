using MediatR;
using POS.Application.Common.Interfaces;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Application.Utang.Commands.UpdateSuki;

public class UpdateSukiCommandHandler : IRequestHandler<UpdateSukiCommand>
{
    private readonly IUtangRepository _utang;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSukiCommandHandler(IUtangRepository utang, IUnitOfWork unitOfWork)
    {
        _utang = utang;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateSukiCommand request, CancellationToken ct)
    {
        var suki = await _utang.GetSukiByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Suki", request.Id);

        suki.Name = request.Name.Trim();
        suki.Phone = string.IsNullOrWhiteSpace(request.Phone)
            ? null
            : request.Phone.Trim();

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
