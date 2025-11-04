using Clear.DataService.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Clear.DataService.Entities.Cosmos;

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
    public AuditUser User { get; set; } = default!;

    public static AuditDetails Create(DateTime date, string id, string name) => new()
    {
        Date = date,
        User = AuditUser.Create(id, name)
    };
}

public record AuditUser
{
    [Required]
    public string Id { get; set; } = default!;

    [Required]
    public string Name { get; set; } = default!;

    public static AuditUser Create(string id, string name) => new()
    {
        Id = id,
        Name = name
    };
}