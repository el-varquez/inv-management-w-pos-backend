using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Tests.Fakes;

public class FakeCurrentUser : ICurrentUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = "Admin";
    public Guid? TenantId { get; set; }
}
