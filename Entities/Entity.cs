using ClearDataService.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClearDataService.Entities;

public abstract class BaseEntity<T> : IEntity<T> where T : struct
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public T Id { get; }
}

public abstract class BaseAuditEntity<T, TUserId> : BaseEntity<T> where T : struct where TUserId : struct
{
    [Required]
    public AuditDetails<TUserId> Created { get; set; } = null!;
    public AuditDetails<TUserId>? Updated { get; set; }
}

public abstract class BaseAuditDeleteEntity<T, TUserId> : BaseAuditEntity<T, TUserId> where T : struct where TUserId : struct
{
    public bool IsDeleted { get; set; }
    public AuditDetails<TUserId>? Deleted { get; set; }
}

[Owned]
public record AuditDetails<T> where T : struct
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public T UserId { get; set; }
}