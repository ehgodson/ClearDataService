using ClearDataService.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace ClearDataService.Entities.Sql;

public abstract class BaseSqlDbEntity<T> : ISqlDbEntity<T>
{
    [Key]
    public required T Id { get; set; } = default!;
}

public abstract class BaseSqlDbAuditEntity<T, TUserId> : BaseSqlDbEntity<T>
{
    [Required]
    public required AuditDetails<TUserId> Created { get; set; } = default!;
    public AuditDetails<TUserId>? Updated { get; set; }
}

public abstract class BaseSqlDbAuditDeleteEntity<T, TUserId> : BaseSqlDbAuditEntity<T, TUserId>
{
    public bool IsDeleted { get; set; }
    public AuditDetails<TUserId>? Deleted { get; set; }
}

[Owned]
public record AuditDetails<TUserId>
{
    [Required]
    public required DateTime Date { get; set; }

    [Required]
    public required TUserId UserId { get; set; } = default!;
}