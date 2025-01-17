namespace ClearDataService.Entities;

public abstract class BaseEntity : BaseEntity<int>;

public abstract class BaseAuditEntity : BaseAuditEntity<int, int>;

public abstract class BaseAuditDeleteEntity : BaseAuditDeleteEntity<int, int>;

[Owned]
public record AuditDetails : AuditDetails<int>;