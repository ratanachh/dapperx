; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DPX001 | DapperX | Error | Missing [Entity]
DPX002 | DapperX | Error | Missing [Id]
DPX003 | DapperX | Error | Missing [GeneratedValue]
DPX004 | DapperX | Error | Invalid [Version] type
DPX005 | DapperX | Error | Missing [SequenceGenerator]
DPX006 | DapperX | Error | Mutating method on [Immutable] entity
DPX007 | DapperX | Error | [PrimaryKeyJoinColumn] requires Assigned key
DPX008 | DapperX | Error | [PrimaryKeyJoinColumn] and [JoinColumn] conflict
DPX009 | DapperX | Error | [Formula] and [Generated] conflict
DPX010 | DapperX | Error | [ColumnTransformer] and [Converter] conflict
DPX011 | DapperX | Error | Missing [CollectionTable]
DPX012 | DapperX | Error | Circular relationship detected
DPX013 | DapperX | Warning | Missing IAuditingProvider
DPX014 | DapperX | Warning | Missing ITenantProvider
DPX015 | DapperX | Warning | Property name conflicts with operator keyword
DPX017 | DapperX | Error | Sequence not supported on SQLite
DPX018 | DapperX | Error | Stored procedures not supported on SQLite
DPX019 | DapperX | Error | Duplicate [Repository] interface for entity
DPX020 | DapperX | Warning | Derived query method name could not be parsed
DPX021 | DapperX | Error | Unknown property in derived query method name
DPX022 | DapperX | Warning | IncludeDeleted requires soft delete
DPX023 | DapperX | Error | Ambiguous derived query method name
DPX024 | DapperX | Error | Regex operator not supported on database provider
DPX025 | DapperX | Error | CPQL translation not implemented
DPX026 | DapperX | Error | Write operation must not use By conditions
DPX027 | DapperX | Error | Write operation requires entity parameter
DPX028 | DapperX | Error | Bulk operation requires entity collection parameter
DPX029 | DapperX | Warning | Regex operator requires SQLite REGEXP extension
DPX030 | DapperX | Error | Bulk id method not supported for composite-key entity
DPX031 | DapperX | Warning | Upsert not supported for composite-key entity
DPX032 | DapperX | Error | OneToMany must use LazyCollection or LazyMap
DPX033 | DapperX | Error | Could not resolve foreign key for batch load
DPX034 | DapperX | Error | LazyMap relationship requires MapKey
DPX035 | DapperX | Warning | Schema not supported on SQLite
DPX036 | DapperX | Error | Multiple result sets not supported on SQLite
DPX037 | DapperX | Error | LockMode not supported on SQLite
DPX038 | DapperX | Error | SQLite query lock not supported
DPX039 | DapperX | Error | Projection property not mapped
DPXCPQL040 | DapperX.CPQL | Error | CPQL soft-delete bypass
DPX043 | DapperX | Error | Unknown named entity graph node
DPX044 | DapperX | Error | Empty [GlobalFilter] condition
DPX045 | DapperX | Error | [SoftDelete] column not mapped
DPX046 | DapperX | Error | [GeneratedValue] not allowed on composite key
DPX047 | DapperX | Warning | CPQL DELETE bypasses soft delete
DPX048 | DapperX | Warning | Pessimistic lock without transaction parameter
DPX049 | DapperX | Warning | MySQL lock timeout unsupported for query lock API
DPX050 | DapperX | Error | [Formula] cannot be used on [Id]
DPX051 | DapperX | Error | [Formula] cannot be used on [Version]
DPX052 | DapperX | Error | [Formula] cannot be used with [Sortable]
DPX053 | DapperX | Error | [Formula] property not allowed in query predicate
DPX054 | DapperX | Error | [Embeddable] cannot have [Id]
DPX055 | DapperX | Error | [Embeddable] cannot have [Table]
DPX056 | DapperX | Error | [Embedded] type must be [Embeddable]
DPX057 | DapperX | Error | Invalid [Converter] type
DPX058 | DapperX | Error | [AssociationOverride] name not found
DPX059 | DapperX | Error | Invalid [Generated] GenerationTime
DPX060 | DapperX | Warning | Global filter parameter name conflict
DPX061 | DapperX | Error | Missing [SecondaryTable] PrimaryKeyJoinColumn
DPX062 | DapperX | Error | Duplicate [SecondaryTable]
DPX063 | DapperX | Error | [Id] on secondary table column
DPX064 | DapperX | Error | [MapKey] column not mapped on child entity
DPX065 | DapperX | Error | LazyMap generic type mismatch
DPX066 | DapperX | Error | [IdClass] key property mismatch
DPX067 | DapperX | Error | [EmbeddedId] type must be [Embeddable]
DPX068 | DapperX | Error | [Id] not allowed on [EmbeddedId] properties
DPX069 | DapperX | Error | Composite key requires multiple key columns
DPX070 | DapperX | Error | Unknown named entity graph subgraph node
DPX071 | DapperX | Error | EntityGraph cannot combine with Include joins
DPX072 | DapperX | Error | Unknown stored procedure parameter
DPX073 | DapperX | Error | Stored procedure return type mismatch
DPX074 | DapperX | Error | Stored procedure output parameter count mismatch
DPX075 | DapperX | Error | Stored procedure result set count mismatch
DPX076 | DapperX | Error | Missing [JoinTable] on [ManyToMany]
DPX077 | DapperX | Error | Missing [JoinTable] JoinColumn
DPX078 | DapperX | Error | Missing [JoinTable] InverseJoinColumn
DPX090 | DapperX | Warning | LogExecutableSql security reminder
DPXCPQL001 | DapperX.CPQL | Error | CPQL parse error
DPXCPQL002 | DapperX.CPQL | Error | CPQL validation
DPXCPQL003 | DapperX.CPQL | Error | CPQL validation
DPXCPQL004 | DapperX.CPQL | Error | CPQL validation
DPXCPQL010 | DapperX.CPQL | Error | CPQL parameter not found
DPXCPQL011 | DapperX.CPQL | Warning | Unused CPQL parameter
DPXCPQL012 | DapperX.CPQL | Error | CPQL validation
DPXCPQL020 | DapperX.CPQL | Error | CPQL translation error
DPXCPQL021 | DapperX.CPQL | Error | CPQL validation
DPXCPQL022 | DapperX.CPQL | Error | CPQL validation
DPXCPQL023 | DapperX.CPQL | Error | CPQL validation
DPXCPQL024 | DapperX.CPQL | Error | CPQL validation
DPXCPQL025 | DapperX.CPQL | Error | CPQL validation
DPXCPQL026 | DapperX.CPQL | Warning | CPQL validation
DPXCPQL027 | DapperX.CPQL | Error | CPQL validation
DPXCPQL028 | DapperX.CPQL | Error | CPQL validation
DPXCPQL029 | DapperX.CPQL | Error | CPQL validation
DPXCPQL030 | DapperX.CPQL | Warning | Unreferenced CTE
DPXCPQL031 | DapperX.CPQL | Error | CPQL validation
DPXCPQL032 | DapperX.CPQL | Error | CPQL validation
DPXCPQL033 | DapperX.CPQL | Error | CPQL validation
DPXCPQL034 | DapperX.CPQL | Error | CPQL validation
DPXCPQL035 | DapperX.CPQL | Error | CPQL validation
