using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Suki : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid CreatedBy { get; set; }
}
