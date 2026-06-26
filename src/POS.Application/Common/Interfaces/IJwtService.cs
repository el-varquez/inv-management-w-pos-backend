using POS.Domain.Entities;

namespace POS.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}