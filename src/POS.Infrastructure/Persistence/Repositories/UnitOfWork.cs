using Microsoft.EntityFrameworkCore;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public UnitOfWork(AppDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ReceiptNumber") == true)
        {
            throw new ReceiptNumberCollisionException();
        }
    }
}
