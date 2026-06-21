namespace POS.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid Id { get; }
    string Role { get; }
    Guid? TenantId { get; }
}