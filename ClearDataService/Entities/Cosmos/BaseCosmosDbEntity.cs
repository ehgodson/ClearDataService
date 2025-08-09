using ClearDataService.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace ClearDataService.Entities.Cosmos;

public abstract class BaseCosmosDbEntity : ICosmosDbEntity
{
    [Key]
    public string Id { get; set; } = default!;
}

public abstract class BaseCosmosDbAuditEntity : BaseCosmosDbEntity
{
    [Required]
    public AuditDetails Created { get; set; } = null!;
    public AuditDetails? Updated { get; set; }
}

public abstract class BaseCosmosDbAuditDeleteEntity : BaseCosmosDbAuditEntity
{
    public bool IsDeleted { get; set; }
    public AuditDetails? Deleted { get; set; }
}

public record AuditDetails
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public string UserId { get; set; } = default!;
}