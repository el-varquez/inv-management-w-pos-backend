using System.Net.Http.Headers;
using POS.Domain.Entities;

namespace POS.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

