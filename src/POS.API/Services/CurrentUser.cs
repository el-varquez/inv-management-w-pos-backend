using System.Security.Claims;
using POS.Application.Common.Interfaces;

namespace POS.API.Services;

public class CurrentUser : ICurrentUser
{
    public const string TenantClaimType = "tenant_id";

    private readonly IHttpContextAccessor _accessor;
    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid Id => Guid.TryParse(_accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    public string Role => _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public Guid? TenantId =>
        Guid.TryParse(_accessor.HttpContext?.User.FindFirstValue(TenantClaimType), out var tid)
            ? tid
            : null;
}