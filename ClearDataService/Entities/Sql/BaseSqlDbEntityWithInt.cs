namespace Clear.DataService.Entities.Sql;

public abstract class BaseSqlDbEntity : BaseSqlDbEntity<int>;

public abstract class BaseSqlDbAuditEntity : BaseSqlDbAuditEntity<int, int>;

public abstract class BaseSqlDbAuditDeleteEntity : BaseSqlDbAuditDeleteEntity<int, int>;

[Owned]
public record AuditDetails : AuditDetails<int>;