namespace Dapper.Npa.Generator.Utils;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    private const string Category = "DapperNpa";

    public static readonly DiagnosticDescriptor MissingEntity = new(
        "DPX001", "Missing [Entity]",
        "Class '{0}' uses Dapper Npamapping attributes but is missing [Entity].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingId = new(
        "DPX002", "Missing [Id]",
        "Entity '{0}' has no property marked with [Id].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingGeneratedValue = new(
        "DPX003", "Missing [GeneratedValue]",
        "Property '{0}' on entity '{1}' is marked [Id] but has no [GeneratedValue].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidVersionType = new(
        "DPX004", "Invalid [Version] type",
        "Property '{0}' on entity '{1}' is marked [Version] but its type '{2}' is not supported. Use int, long, DateTime, or DateTimeOffset.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingSequenceGenerator = new(
        "DPX005", "Missing [SequenceGenerator]",
        "Entity '{0}' uses GenerationType.Sequence with generator name '{1}' but no matching [SequenceGenerator] was found.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MutatingMethodOnImmutable = new(
        "DPX006", "Mutating method on [Immutable] entity",
        "Entity '{0}' is marked [Immutable] but declares method '{1}' which requires a mutating repository method.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor PrimaryKeyJoinNotAssigned = new(
        "DPX007", "[PrimaryKeyJoinColumn] requires Assigned key",
        "Entity '{0}' uses [PrimaryKeyJoinColumn] but its [Id] property is not GenerationType.Assigned.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor PrimaryKeyJoinAndJoinColumn = new(
        "DPX008", "[PrimaryKeyJoinColumn] and [JoinColumn] conflict",
        "Property '{0}' on entity '{1}' cannot have both [PrimaryKeyJoinColumn] and [JoinColumn].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor FormulaOnGeneratedColumn = new(
        "DPX009", "[Formula] and [Generated] conflict",
        "Property '{0}' on entity '{1}' cannot have both [Formula] and [Generated].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ColumnTransformerAndConverter = new(
        "DPX010", "[ColumnTransformer] and [Converter] conflict",
        "Property '{0}' on entity '{1}' cannot have both [ColumnTransformer] and [Converter].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingCollectionTable = new(
        "DPX011", "Missing [CollectionTable]",
        "Property '{0}' on entity '{1}' has [ElementCollection] but no [CollectionTable].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CircularReference = new(
        "DPX012", "Circular relationship detected",
        "Entity '{0}' has a circular relationship graph. Convert to a DAG.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingAuditingProvider = new(
        "DPX013", "Missing IAuditingProvider",
        "Entity '{0}' uses [CreatedBy] or [LastModifiedBy] but IAuditingProvider is not registered.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor MissingTenantProvider = new(
        "DPX014", "Missing ITenantProvider",
        "Entity '{0}' uses [TenantId] but ITenantProvider is not registered.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor ReservedPropertyName = new(
        "DPX015", "Property name conflicts with operator keyword",
        "Property '{0}' on entity '{1}' shares a name with derived query keyword '{2}'. Method name derivation may be ambiguous — consider using [Query] with CPQL.",
        Category, DiagnosticSeverity.Warning, true);

    // Pessimistic lock on SQLite: DPX037 (derived LockMode param), DPX038 (IQuery.WithLock) — no DPX016.

    public static readonly DiagnosticDescriptor SequenceNotSupportedOnSqlite = new(
        "DPX017", "Sequence not supported on SQLite",
        "GenerationType.Sequence is not supported with SQLite provider.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StoredProcedureNotSupportedOnSqlite = new(
        "DPX018", "Stored procedures not supported on SQLite",
        "Stored procedures are not supported with SQLite provider.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateRepositoryInterface = new(
        "DPX019", "Duplicate [Repository] interface for entity",
        "Interface '{0}' cannot be used because entity already has repository interface '{1}'.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DerivedMethodNotParsed = new(
        "DPX020", "Derived query method name could not be parsed",
        "Method '{0}' on entity '{1}' could not be parsed as a derived query method name.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor UnknownDerivedQueryProperty = new(
        "DPX021", "Unknown property in derived query method name",
        "Property '{0}' in method '{1}' does not exist on entity '{2}'.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor IncludeDeletedWithoutSoftDelete = new(
        "DPX022", "IncludeDeleted requires soft delete",
        "Method '{0}' declares includeDeleted but entity '{1}' has no [SoftDelete].",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor AmbiguousDerivedQueryPath = new(
        "DPX023", "Ambiguous derived query method name",
        "Method '{0}' on entity '{1}' is ambiguous at path segment '{2}'. Use [Query] with CPQL instead.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RegexNotSupportedOnProvider = new(
        "DPX024", "Regex operator not supported on database provider",
        "Method '{0}' on entity '{1}' uses a Regex derived query operator but provider '{2}' does not support it. Use [Query(NativeQuery = true)] instead.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RegexWarningOnSqlite = new(
        "DPX029", "Regex operator requires SQLite REGEXP extension",
        "Method '{0}' on entity '{1}' uses a Regex derived query operator; SQLite requires the REGEXP extension at runtime.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor CpqlTranslationNotImplemented = new(
        "DPX025", "CPQL translation not implemented",
        "Method '{0}' on entity '{1}' uses [Query] with CPQL; translation is not implemented yet. Set NativeQuery = true or wait for EPIC 5.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor WriteOperationWithConditions = new(
        "DPX026", "Write operation must not use By conditions",
        "Method '{0}' on entity '{1}' is a write operation (Insert/Update) and must not include By conditions in the method name.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor WriteOperationInvalidSignature = new(
        "DPX027", "Write operation requires entity parameter",
        "Method '{0}' on entity '{1}' must declare exactly one parameter of type '{2}'.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor BulkOperationInvalidSignature = new(
        "DPX028", "Bulk operation requires entity collection parameter",
        "Method '{0}' on entity '{1}' with [BulkOperation] must declare a parameter of type IEnumerable<{2}>.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CompositeKeyBulkIdMethod = new(
        "DPX030", "Bulk id method not supported for composite-key entity",
        "Method '{0}' cannot be declared on repository for composite-key entity '{1}'.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CompositeKeyUpsertNotSupported = new(
        "DPX031", "Upsert not supported for composite-key entity",
        "UpsertAsync/UpsertManyAsync are not generated for composite-key entity '{0}'. Use insert and update separately.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor BatchLoadInvalidCollectionType = new(
        "DPX032", "OneToMany must use LazyCollection or LazyMap",
        "OneToMany property '{0}' on entity '{1}' must be typed as LazyCollection<T> or LazyMap<TKey,TValue> for batch loading.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor BatchLoadUnresolvedFk = new(
        "DPX033", "Could not resolve foreign key for batch load",
        "Could not resolve foreign key column for OneToMany property '{0}' on entity '{1}'. Add [JoinColumn] on the mapped ManyToOne side or an FK scalar property on the child.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MapKeyMissing = new(
        "DPX034", "LazyMap relationship requires MapKey",
        "LazyMap OneToMany property '{0}' on entity '{1}' must specify [MapKey(column)].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor SchemaNotSupportedOnSqlite = new(
        "DPX035", "Schema not supported on SQLite",
        "Entity '{0}' uses [Table(Schema = ...)] but SQLite does not support schema-qualified table names; schema value is ignored.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor MultipleResultSetsNotSupportedOnSqlite = new(
        "DPX036", "Multiple result sets not supported on SQLite",
        "Method '{0}' on entity '{1}' returns multiple result sets which SQLite stored procedures do not support.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor LockModeNotSupportedOnSqlite = new(
        "DPX037", "LockMode not supported on SQLite",
        "Method '{0}' on entity '{1}' declares LockMode parameter but SQLite only supports optimistic reads.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor SqliteQueryLockNotSupported = new(
        "DPX038", "SQLite query lock not supported",
        "Entity '{0}' uses SQLite; IQuery.WithLock pessimistic modes are not supported.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ProjectionPropertyNotMapped = new(
        "DPX039", "Projection property not mapped",
        "Projection '{1}' property '{0}' does not map to a column on entity '{2}'.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CpqlSoftDeleteBypass = new(
        "DPXCPQL040", "CPQL soft-delete bypass",
        "CPQL must not reference soft-delete column '{0}' on entity '{1}'; use [Query(NativeQuery = true)] to bypass automatic filters.",
        "Dapper.Npa.CPQL", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnknownEntityGraphNode = new(
        "DPX043", "Unknown named entity graph node",
        "Named entity graph '{0}' on entity '{1}' references unknown relationship property '{2}'.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnknownEntityGraphSubGraphNode = new(
        "DPX070", "Unknown named entity graph subgraph node",
        "Named entity graph '{0}' on entity '{1}' subgraph '{2}' references unknown relationship property '{3}' on the subgraph target entity.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EntityGraphWithInclude = new(
        "DPX071", "EntityGraph cannot combine with Include joins",
        "Method '{0}' on entity '{1}' cannot use an EntityGraph parameter together with navigation joins (Include-style paths in the query).",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StoredProcedureUnknownParameter = new(
        "DPX072", "Unknown stored procedure parameter",
        "Stored procedure '{0}' on method '{1}' references unknown parameter '{2}'.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StoredProcedureReturnTypeMismatch = new(
        "DPX073", "Stored procedure return type mismatch",
        "Method '{0}' on entity '{1}' must return Task<ProcResult<…>> when output parameters are declared, Task<MultiResult<…>> when ResultSets are declared, or Task<IEnumerable<{1}>> for entity result sets.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StoredProcedureOutParameterCountMismatch = new(
        "DPX074", "Stored procedure output parameter count mismatch",
        "Method '{0}' return type ProcResult has {2} type argument(s) but stored procedure declares {3} OutParameters.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor StoredProcedureResultSetCountMismatch = new(
        "DPX075", "Stored procedure result set count mismatch",
        "Method '{0}' declares {2} ResultSets but MultiResult supports exactly two result grids.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EmptyGlobalFilterCondition = new(
        "DPX044", "Empty [GlobalFilter] condition",
        "Global filter '{0}' on entity '{1}' has an empty Condition.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor SoftDeleteColumnNotFound = new(
        "DPX045", "[SoftDelete] column not mapped",
        "Entity '{0}' uses [SoftDelete] with column '{1}' but no mapped property matches that column.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CompositeKeyGeneratedValue = new(
        "DPX046", "[GeneratedValue] not allowed on composite key",
        "Property '{0}' on composite-key entity '{1}' must use GenerationType.Assigned.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CompositeKeyIdClassMismatch = new(
        "DPX066", "[IdClass] key property mismatch",
        "Composite key property '{0}' on key class '{1}' does not match an [Id] property on entity '{2}'.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CompositeKeyNotEmbeddable = new(
        "DPX067", "[EmbeddedId] type must be [Embeddable]",
        "Property '{0}' on entity '{1}' is marked [EmbeddedId] but type '{2}' is not [Embeddable].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CompositeKeyEmbeddedIdInnerId = new(
        "DPX068", "[Id] not allowed on [EmbeddedId] properties",
        "Embeddable '{1}' property '{0}' must not be marked [Id] when used as [EmbeddedId].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CompositeKeyRequiresMultipleIds = new(
        "DPX069", "Composite key requires multiple key columns",
        "Entity '{0}' must declare at least two key columns for a composite primary key.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor CpqlDeleteBypassesSoftDelete = new(
        "DPX047", "CPQL DELETE bypasses soft delete",
        "Named query '{0}' on entity '{1}' uses CPQL DELETE which bypasses [SoftDelete]; use NativeQuery = true only for privileged hard-delete.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor PessimisticLockWithoutTransactionContext = new(
        "DPX048", "Pessimistic lock without transaction parameter",
        "Method '{0}' on entity '{1}' declares LockMode but no transaction parameter; pessimistic locks should run inside an explicit transaction.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor MySqlLockTimeoutUnsupported = new(
        "DPX049", "MySQL lock timeout unsupported for query lock API",
        "Entity '{0}' uses MySQL. IQuery.WithLock(timeoutMs > 0) is treated as NOWAIT because MySQL lock timeout is not emitted by the query lock API.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor FormulaOnId = new(
        "DPX050", "[Formula] cannot be used on [Id]",
        "Property '{0}' on entity '{1}' cannot have both [Formula] and [Id].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor FormulaOnVersion = new(
        "DPX051", "[Formula] cannot be used on [Version]",
        "Property '{0}' on entity '{1}' cannot have both [Formula] and [Version].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor FormulaOnSortable = new(
        "DPX052", "[Formula] cannot be used with [Sortable]",
        "Property '{0}' on entity '{1}' cannot have both [Formula] and [Sortable].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor FormulaNotQueryable = new(
        "DPX053", "[Formula] property not allowed in query predicate",
        "Property '{0}' on entity '{1}' is a [Formula] expression and cannot be used in WHERE, ORDER BY, or CPQL predicates.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EmbeddableHasId = new(
        "DPX054", "[Embeddable] cannot have [Id]",
        "Type '{0}' is marked [Embeddable] but property '{1}' is marked [Id].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EmbeddableHasTable = new(
        "DPX055", "[Embeddable] cannot have [Table]",
        "Type '{0}' is marked [Embeddable] but is also marked [Table].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EmbeddedTypeNotEmbeddable = new(
        "DPX056", "[Embedded] type must be [Embeddable]",
        "Property '{0}' on entity '{1}' is marked [Embedded] but type '{2}' is not marked [Embeddable].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidConverterType = new(
        "DPX057", "Invalid [Converter] type",
        "Property '{0}' on entity '{1}': converter '{2}' must implement IValueConverter<{3}, TColumn> with a public parameterless constructor.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor AssociationOverrideNotFound = new(
        "DPX058", "[AssociationOverride] name not found",
        "Entity '{0}' declares [AssociationOverride] for '{1}' but no matching relationship property exists on the entity or its mapped superclasses.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidGeneratedGenerationTime = new(
        "DPX059", "Invalid [Generated] GenerationTime",
        "Property '{0}' on entity '{1}' has [Generated] with an unsupported GenerationTime.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor GlobalFilterParameterConflict = new(
        "DPX060", "Global filter parameter name conflict",
        "Parameter '@{0}' appears in multiple global filters on entity '{1}' ({2}); use unique parameter names per active filter set.",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor MissingSecondaryTablePrimaryKeyJoinColumn = new(
        "DPX061", "Missing [SecondaryTable] PrimaryKeyJoinColumn",
        "Secondary table '{0}' on entity '{1}' must specify PrimaryKeyJoinColumn.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateSecondaryTable = new(
        "DPX062", "Duplicate [SecondaryTable]",
        "Entity '{0}' declares [SecondaryTable] for '{1}' more than once.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor IdOnSecondaryTable = new(
        "DPX063", "[Id] on secondary table column",
        "Property '{0}' on entity '{1}' is marked [Id] but mapped to secondary table '{2}'; the primary key must live on the primary table only.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MapKeyColumnNotFound = new(
        "DPX064", "[MapKey] column not mapped on child entity",
        "LazyMap property '{2}' on entity '{3}' specifies [MapKey(\"{0}\")] but child entity '{1}' has no mapped column with that name.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MapKeyTypeMismatch = new(
        "DPX065", "LazyMap generic type mismatch",
        "LazyMap property '{2}' on entity '{3}' uses incompatible generic types: map key type '{0}' does not match child column type '{1}'.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ManyToManyMissingJoinTable = new(
        "DPX076", "Missing [JoinTable] on [ManyToMany]",
        "ManyToMany property '{0}' on entity '{1}' must specify [JoinTable(tableName, JoinColumn = ..., InverseJoinColumn = ...)].",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor JoinTableMissingJoinColumn = new(
        "DPX077", "Missing [JoinTable] JoinColumn",
        "JoinTable on ManyToMany property '{0}' of entity '{1}' must specify JoinColumn.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor JoinTableMissingInverseJoinColumn = new(
        "DPX078", "Missing [JoinTable] InverseJoinColumn",
        "JoinTable on ManyToMany property '{0}' of entity '{1}' must specify InverseJoinColumn.",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor LogExecutableSqlSecurityReminder = new(
        "DPX090", "LogExecutableSql security reminder",
        "DapperXOptions.LogExecutableSql substitutes parameter values into SQL for debugging. Never enable in production — values may contain sensitive data.",
        Category, DiagnosticSeverity.Warning, true);
}
