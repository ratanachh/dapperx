namespace DapperX.Generator.Builders;

using Microsoft.CodeAnalysis;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

internal static class MetadataBuilder
{
    public static EntityModel? Build(INamedTypeSymbol entity, SourceProductionContext ctx)
    {
        var tableAttr = SyntaxHelper.GetAttribute(entity, SyntaxHelper.TableAttr);
        var tableName = SyntaxHelper.GetConstructorArg<string>(tableAttr, 0)
                        ?? SyntaxHelper.ToSnakeCase(entity.Name);
        var schema = tableAttr is null ? null : SyntaxHelper.GetNamedArg<string>(tableAttr, "Schema");

        var isImmutable = SyntaxHelper.HasAttribute(entity, SyntaxHelper.ImmutableAttr);
        var softDeleteAttr = ResolveSoftDeleteAttribute(entity);
        var softDeleteCol = softDeleteAttr is null ? null
            : (SyntaxHelper.GetNamedArg<string>(softDeleteAttr, "Column") ?? "is_deleted");
        var deletedAtCol = softDeleteAttr is null ? null
            : SyntaxHelper.GetNamedArg<string>(softDeleteAttr, "DeletedAtColumn");
        SoftDeleteModel? softDeleteModel = softDeleteCol is null
            ? null
            : new SoftDeleteModel { Column = softDeleteCol, DeletedAtColumn = deletedAtCol };

        // Tenant
        string? tenantIdColumn = null;

        // Collect properties (including inherited from MappedSuperclass)
        var properties = new List<PropertyModel>();
        var relationships = new List<RelationshipModel>();
        var elementCollections = new List<ElementCollectionModel>();
        CollectMembers(entity, properties, relationships, elementCollections, ref tenantIdColumn, ctx);

        var idCount = properties.Count(p => p.IsId);
        var hasEmbeddedId = SyntaxHelper.HasAttribute(entity, SyntaxHelper.EmbeddedIdAttr);
        var hasIdClass = SyntaxHelper.HasAttribute(entity, SyntaxHelper.IdClassAttr);
        var hasCompositeKey = idCount > 1 || hasEmbeddedId || hasIdClass;

        if (idCount == 0 && !hasEmbeddedId && !hasIdClass)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingId,
                entity.Locations.FirstOrDefault(), entity.Name));
            return null;
        }

        // Auditing
        var auditingModel = BuildAuditingModel(properties);

        // Secondary tables
        var secondaryTables = BuildSecondaryTables(entity, properties);

        // Global filters (entity + inherited from MappedSuperclass)
        var globalFilters = ResolveGlobalFilters(entity);

        // Named queries
        var namedQueries = SyntaxHelper.GetAttributes(entity, SyntaxHelper.NamedQueryAttr)
            .Select(a => new NamedQueryModel
            {
                Name = SyntaxHelper.GetConstructorArg<string>(a, 0) ?? string.Empty,
                Query = SyntaxHelper.GetConstructorArg<string>(a, 1) ?? string.Empty,
            }).ToList();

        var entityListenerTypes = BuildEntityListeners(entity);
        var entityListeners = entityListenerTypes.Select(l => l.TypeFqn).ToList();

        var embeddedSites = ExpandEmbeddedColumns(entity, properties, ctx);

        var derivedQueryPaths = DerivedQueryPathBuilder.Build(
            new EntityModel { ClassName = entity.Name, TableName = tableName, Schema = schema, Properties = properties },
            entity,
            properties,
            relationships);

        var namedEntityGraphs = BuildNamedEntityGraphs(entity);

        var idProp = properties.FirstOrDefault(p => p.IsId);
        SequenceModel? sequence = null;
        if (idProp?.IdGenerationStrategy == "Sequence" && !string.IsNullOrEmpty(idProp.SequenceGeneratorName))
            sequence = ResolveSequence(entity, idProp.SequenceGeneratorName);

        var associationOverrides = SyntaxHelper.GetAttributes(entity, SyntaxHelper.AssociationOverrideAttr)
            .Select(a => new AssociationOverrideModel
            {
                RelationshipPropertyName = SyntaxHelper.GetConstructorArg<string>(a, 0) ?? string.Empty,
                OverrideJoinColumn = SyntaxHelper.GetConstructorArg<string>(a, 1) ?? string.Empty,
            })
            .Where(o => !string.IsNullOrEmpty(o.RelationshipPropertyName) && !string.IsNullOrEmpty(o.OverrideJoinColumn))
            .ToList();

        foreach (var rel in relationships)
        {
            var ov = associationOverrides.FirstOrDefault(o =>
                string.Equals(o.RelationshipPropertyName, rel.PropertyName, StringComparison.Ordinal));
            if (ov is not null)
                rel.ForeignKeyColumn = ov.OverrideJoinColumn;
        }

        TenancyModel? tenancyModel = tenantIdColumn is null
            ? null
            : new TenancyModel { TenantIdColumn = tenantIdColumn };

        CompositeKeyModel? compositeKey = null;
        if (hasCompositeKey)
            compositeKey = CompositeKeyMetadataBuilder.Build(entity, properties, ctx);

        return new EntityModel
        {
            Namespace = entity.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            ClassName = entity.Name,
            TableName = tableName,
            Schema = schema,
            IsImmutable = isImmutable,
            SoftDeleteColumn = softDeleteCol,
            DeletedAtColumn = deletedAtCol,
            SoftDelete = softDeleteModel,
            TenantIdColumn = tenantIdColumn,
            Tenancy = tenancyModel,
            Properties = properties,
            Relationships = relationships,
            SecondaryTables = secondaryTables,
            GlobalFilters = globalFilters,
            NamedQueries = namedQueries,
            EntityListeners = entityListeners,
            EntityListenerTypes = entityListenerTypes,
            Auditing = auditingModel,
            LifecycleMethods = BuildLifecycleMethods(entity),
            DerivedQueryPaths = derivedQueryPaths,
            HasCompositeKey = hasCompositeKey,
            CompositeKey = compositeKey,
            ElementCollections = elementCollections,
            NamedEntityGraphs = namedEntityGraphs,
            Sequence = sequence,
            AssociationOverrides = associationOverrides,
            EmbeddedSites = embeddedSites,
            Formulas = properties
                .Where(p => p.Formula is not null)
                .Select(p => new FormulaModel
                {
                    PropertyName = p.PropertyName,
                    Sql = p.Formula!,
                    ColumnAlias = p.ColumnName,
                })
                .ToList(),
        };
    }

    private static SequenceModel? ResolveSequence(INamedTypeSymbol entity, string generatorName)
    {
        foreach (var symbol in new ISymbol[] { entity, entity.ContainingAssembly })
        {
            foreach (var attr in SyntaxHelper.GetAttributes(symbol, SyntaxHelper.SequenceGeneratorAttr))
            {
                var name = SyntaxHelper.GetConstructorArg<string>(attr, 0);
                if (string.Equals(name, generatorName, StringComparison.Ordinal))
                {
                    return new SequenceModel
                    {
                        Name = name ?? generatorName,
                        SequenceName = SyntaxHelper.GetConstructorArg<string>(attr, 1) ?? generatorName,
                    };
                }
            }
        }

        return null;
    }

    private static List<LifecycleMethodModel> BuildLifecycleMethods(INamedTypeSymbol entity)
    {
        var methods = new List<LifecycleMethodModel>();
        foreach (var member in entity.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.MethodKind != MethodKind.Ordinary || member.IsStatic)
                continue;
            if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, entity))
                continue;

            if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostLoadAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostLoad });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PrePersistAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PrePersist });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostPersistAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostPersist });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PreUpdateAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PreUpdate });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostUpdateAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostUpdate });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PreRemoveAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PreRemove });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostRemoveAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostRemove });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PrePersistBatchAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PrePersistBatch });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostPersistBatchAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostPersistBatch });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PreUpdateBatchAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PreUpdateBatch });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostUpdateBatchAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostUpdateBatch });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PreRemoveBatchAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PreRemoveBatch });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostRemoveBatchAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostRemoveBatch });
        }
        return methods;
    }

    private static void CollectMembers(
        INamedTypeSymbol type,
        List<PropertyModel> properties,
        List<RelationshipModel> relationships,
        List<ElementCollectionModel> elementCollections,
        ref string? tenantIdColumn,
        SourceProductionContext ctx)
    {
        // Walk base types for MappedSuperclass
        if (type.BaseType is not null
            && SyntaxHelper.HasAttribute(type.BaseType, SyntaxHelper.MappedSuperclassAttr))
            CollectMembers(type.BaseType, properties, relationships, elementCollections, ref tenantIdColumn, ctx);

        foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public) continue;

            if (SyntaxHelper.HasAttribute(member, SyntaxHelper.TenantIdAttr))
            {
                tenantIdColumn = SyntaxHelper.ToSnakeCase(member.Name);
            }

            if (SyntaxHelper.HasAttribute(member, SyntaxHelper.EmbeddedAttr))
                continue;

            if (SyntaxHelper.HasAttribute(member, SyntaxHelper.ElementCollectionAttr))
            {
                var ecModel = BuildElementCollectionModel(member, type);
                if (ecModel is not null)
                    elementCollections.Add(ecModel);
                continue;
            }

            var propModel = BuildPropertyModel(member, ctx);
            if (propModel is null) continue;

            // Check if it's a relationship property
            var relModel = BuildRelationshipModel(member);
            if (relModel is not null)
                relationships.Add(relModel);
            else
                properties.Add(propModel);
        }
    }

    private static PropertyModel? BuildPropertyModel(IPropertySymbol prop, SourceProductionContext ctx)
    {
        if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.TransientAttr))
            return null; // Excluded entirely

        var colAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.ColumnAttr);
        var columnName = SyntaxHelper.GetNamedArg<string>(colAttr, "Name")
                         ?? SyntaxHelper.ToSnakeCase(prop.Name);
        var secondaryTable = SyntaxHelper.GetNamedArg<string>(colAttr, "Table");

        var isId = SyntaxHelper.HasAttribute(prop, SyntaxHelper.IdAttr);
        var isVersion = SyntaxHelper.HasAttribute(prop, SyntaxHelper.VersionAttr);
        var isSortable = SyntaxHelper.HasAttribute(prop, SyntaxHelper.SortableAttr);
        var isLazy = SyntaxHelper.GetNamedArg<int>(colAttr, "Fetch") == 1; // FetchType.Lazy = 1

        var formulaAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.FormulaAttr);
        var formula = SyntaxHelper.GetConstructorArg<string>(formulaAttr, 0);

        var converterAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.ConverterAttr);
        string? converterTypeName = null;
        string? converterColumnClrTypeName = null;
        if (converterAttr?.ConstructorArguments.FirstOrDefault().Value is INamedTypeSymbol convSym)
        {
            converterTypeName = convSym.ToDisplayString();
            converterColumnClrTypeName = ResolveConverterColumnClrType(convSym);
        }

        var enumeratedAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.EnumeratedAttr);
        if (enumeratedAttr is not null && prop.Type is INamedTypeSymbol enumType
            && enumType.TypeKind == TypeKind.Enum)
        {
            var enumTypeArg = enumeratedAttr.ConstructorArguments.FirstOrDefault().Value;
            var asString = enumTypeArg is int i && i == 0;
            converterTypeName = asString
                ? $"DapperX.Runtime.Converters.EnumToStringConverter<{enumType.ToDisplayString()}>"
                : $"DapperX.Runtime.Converters.EnumToIntConverter<{enumType.ToDisplayString()}>";
            converterColumnClrTypeName = asString ? "string" : "int";
        }

        var ctAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.ColumnTransformerAttr);
        ColumnTransformerModel? ctModel = null;
        if (ctAttr is not null)
            ctModel = new ColumnTransformerModel
            {
                Read = SyntaxHelper.GetNamedArg<string>(ctAttr, "Read"),
                Write = SyntaxHelper.GetNamedArg<string>(ctAttr, "Write"),
            };

        // Validate [Formula] + [Generated] conflict
        var generatedAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.GeneratedAttr);
        if (formula is not null && generatedAttr is not null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.FormulaOnGeneratedColumn,
                prop.Locations.FirstOrDefault(), prop.Name, prop.ContainingType.Name));
        }
        // Validate [ColumnTransformer] + [Converter] conflict
        if (ctAttr is not null && converterAttr is not null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.ColumnTransformerAndConverter,
                prop.Locations.FirstOrDefault(), prop.Name, prop.ContainingType.Name));
        }

        var auditingRole = AuditingRole.None;
        if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.CreatedDateAttr)) auditingRole = AuditingRole.CreatedDate;
        else if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.LastModifiedDateAttr)) auditingRole = AuditingRole.LastModifiedDate;
        else if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.CreatedByAttr)) auditingRole = AuditingRole.CreatedBy;
        else if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.LastModifiedByAttr)) auditingRole = AuditingRole.LastModifiedBy;

        string? genStrategy = null;
        string? seqGenName = null;
        if (isId)
        {
            var genAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.GeneratedValueAttr);
            if (genAttr is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingGeneratedValue,
                    prop.Locations.FirstOrDefault(), prop.Name, prop.ContainingType.Name));
            }
            else
            {
                var strategyVal = genAttr.ConstructorArguments.FirstOrDefault().Value;
                genStrategy = strategyVal switch { 0 => "Identity", 1 => "Sequence", 2 => "Uuid", 3 => "Assigned", _ => "Identity" };
                seqGenName = SyntaxHelper.GetNamedArg<string>(genAttr, "Generator");
            }
        }

        var insertable = SyntaxHelper.GetNamedArg<bool?>(colAttr, "Insertable") ?? true;
        var updatable = SyntaxHelper.GetNamedArg<bool?>(colAttr, "Updatable") ?? true;
        var nullable = SyntaxHelper.GetNamedArg<bool?>(colAttr, "Nullable") ?? true;

        // [Generated] overrides insertable/updatable
        string? generatedTime = null;
        if (generatedAttr is not null)
        {
            var gt = generatedAttr.ConstructorArguments.FirstOrDefault().Value;
            generatedTime = gt switch { 0 => "Insert", 1 => "Always", _ => "Insert" };
            insertable = false;
            updatable = false;
        }

        switch (auditingRole)
        {
            case AuditingRole.CreatedDate:
            case AuditingRole.LastModifiedDate:
                insertable = false;
                updatable = false;
                break;
            case AuditingRole.CreatedBy:
                insertable = true;
                updatable = false;
                break;
            case AuditingRole.LastModifiedBy:
                insertable = true;
                updatable = true;
                break;
        }

        if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.TenantIdAttr))
            updatable = false;

        if (formula is not null)
        {
            insertable = false;
            updatable = false;
        }

        if (ctModel?.Read is not null && ctModel.Write is null)
        {
            insertable = false;
            updatable = false;
        }

        return new PropertyModel
        {
            PropertyName = prop.Name,
            ColumnName = columnName,
            SecondaryTable = secondaryTable,
            ClrTypeName = prop.Type.ToDisplayString(),
            IsId = isId,
            IsVersion = isVersion,
            IsSortable = isSortable,
            Insertable = insertable,
            Updatable = updatable,
            Nullable = nullable,
            IsLazyLoaded = isLazy,
            Formula = formula,
            ConverterTypeName = converterTypeName,
            ConverterColumnClrTypeName = converterColumnClrTypeName,
            ColumnTransformer = ctModel,
            GeneratedTime = generatedTime,
            AuditingRole = auditingRole,
            IdGenerationStrategy = genStrategy,
            SequenceGeneratorName = seqGenName,
        };
    }

    private static string? ResolveConverterColumnClrType(INamedTypeSymbol converterType)
    {
        var iface = converterType.AllInterfaces.FirstOrDefault(i =>
            i.Name == "IValueConverter" && i.TypeArguments.Length == 2);
        return iface?.TypeArguments[1].ToDisplayString();
    }

    private static RelationshipModel? BuildRelationshipModel(IPropertySymbol prop)
    {
        string? kind = null;
        if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.OneToManyAttr)) kind = "OneToMany";
        else if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.ManyToOneAttr)) kind = "ManyToOne";
        else if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.OneToOneAttr)) kind = "OneToOne";
        else if (SyntaxHelper.HasAttribute(prop, SyntaxHelper.ManyToManyAttr)) kind = "ManyToMany";
        if (kind is null) return null;

        var jcAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.JoinColumnAttr);
        var jtAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.JoinTableAttr);
        var obAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.OrderByAttr);
        var ocAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.OrderColumnAttr);
        var mkAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.MapKeyAttr);
        var isPkJoin = SyntaxHelper.HasAttribute(prop, SyntaxHelper.PrimaryKeyJoinColumnAttr);

        var fkColumn = SyntaxHelper.GetConstructorArg<string>(jcAttr, 0)
                       ?? SyntaxHelper.ToSnakeCase(prop.Name) + "_id";

        var oneToManyAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.OneToManyAttr);
        var manyToManyAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.ManyToManyAttr);
        var mappedBy = oneToManyAttr is null ? null : SyntaxHelper.GetNamedArg<string>(oneToManyAttr, "MappedBy");
        var cascadeFlags = CascadeHelper.ParseFromRelationshipAttribute(oneToManyAttr)
            | CascadeHelper.ParseFromRelationshipAttribute(manyToManyAttr);
        var (elementFqn, isLazyCollection, isLazyMap, mapKeyClrType) = ResolveCollectionElementType(prop);
        var isLazyReference = false;
        if (kind == "ManyToOne" && prop.Type is INamedTypeSymbol navType)
        {
            if (navType.OriginalDefinition.ToDisplayString() == "DapperX.Relations.Lazy.LazyReference<T>")
            {
                elementFqn = FormatType(navType.TypeArguments[0]);
                isLazyReference = true;
            }
            else if (navType.TypeKind == TypeKind.Class)
                elementFqn = FormatType(navType);
        }
        else if (elementFqn is null && prop.Type is INamedTypeSymbol navType2 && navType2.TypeKind == TypeKind.Class)
            elementFqn = FormatType(navType2);

        return new RelationshipModel
        {
            PropertyName = prop.Name,
            Kind = kind,
            TargetEntity = elementFqn,
            ForeignKeyColumn = fkColumn,
            MappedBy = mappedBy,
            CascadeFlags = cascadeFlags,
            IsPrimaryKeyJoin = isPkJoin,
            IsLazyCollection = isLazyCollection,
            IsLazyMap = isLazyMap,
            IsLazyReference = isLazyReference,
            MapKeyClrTypeName = mapKeyClrType,
            MapKeyColumn = SyntaxHelper.GetConstructorArg<string>(mkAttr, 0),
            JoinTable = SyntaxHelper.GetConstructorArg<string>(jtAttr, 0),
            JoinTableFk = SyntaxHelper.GetNamedArg<string>(jtAttr, "JoinColumn"),
            JoinTableInverseFk = SyntaxHelper.GetNamedArg<string>(jtAttr, "InverseJoinColumn"),
            OrderByClause = SyntaxHelper.GetConstructorArg<string>(obAttr, 0),
            OrderColumnName = SyntaxHelper.GetConstructorArg<string>(ocAttr, 0),
        };
    }

    private static (string? ElementFqn, bool IsLazyCollection, bool IsLazyMap, string? MapKeyClrType)
        ResolveCollectionElementType(IPropertySymbol prop)
    {
        if (prop.Type is not INamedTypeSymbol type)
            return (null, false, false, null);

        var def = type.OriginalDefinition.ToDisplayString();

        if (def == "DapperX.Relations.Lazy.LazyCollection<T>")
            return (FormatType(type.TypeArguments[0]), true, false, null);

        if (def == "DapperX.Relations.Lazy.LazyMap<TKey, TValue>")
            return (FormatType(type.TypeArguments[1]), false, true, FormatType(type.TypeArguments[0]));

        if (type.TypeArguments.Length == 1)
            return (FormatType(type.TypeArguments[0]), false, false, null);

        return (null, false, false, null);
    }

    private static string? FormatType(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static AuditingModel? BuildAuditingModel(IEnumerable<PropertyModel> properties)
    {
        var list = properties.Where(p => p.AuditingRole != AuditingRole.None).ToList();
        if (!list.Any()) return null;
        return new AuditingModel
        {
            CreatedDateProperty = list.FirstOrDefault(p => p.AuditingRole == AuditingRole.CreatedDate)?.PropertyName,
            LastModifiedDateProperty = list.FirstOrDefault(p => p.AuditingRole == AuditingRole.LastModifiedDate)?.PropertyName,
            CreatedByProperty = list.FirstOrDefault(p => p.AuditingRole == AuditingRole.CreatedBy)?.PropertyName,
            LastModifiedByProperty = list.FirstOrDefault(p => p.AuditingRole == AuditingRole.LastModifiedBy)?.PropertyName,
        };
    }

    private static List<EmbeddedModel> ExpandEmbeddedColumns(
        INamedTypeSymbol entity,
        List<PropertyModel> properties,
        SourceProductionContext ctx)
    {
        var sites = new List<EmbeddedModel>();
        foreach (var member in entity.GetMembers().OfType<IPropertySymbol>())
        {
            if (!SyntaxHelper.HasAttribute(member, SyntaxHelper.EmbeddedAttr))
                continue;
            if (member.Type is not INamedTypeSymbol embedType)
                continue;

            if (!SyntaxHelper.HasAttribute(embedType, SyntaxHelper.EmbeddableAttr))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.EmbeddedTypeNotEmbeddable,
                    member.Locations.FirstOrDefault(),
                    member.Name,
                    entity.Name,
                    embedType.ToDisplayString()));
                continue;
            }

            var prefix = member.Name;
            var overrides = member.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == SyntaxHelper.AttributeOverrideAttr)
                .Select(a => (
                    Property: SyntaxHelper.GetConstructorArg<string>(a, 0) ?? string.Empty,
                    Column: SyntaxHelper.GetConstructorArg<string>(a, 1) ?? string.Empty))
                .Where(x => !string.IsNullOrEmpty(x.Property))
                .ToDictionary(x => x.Property, x => x.Column, StringComparer.Ordinal);

            var innerNames = new List<string>();
            foreach (var inner in embedType.GetMembers().OfType<IPropertySymbol>())
            {
                if (inner.IsStatic || inner.DeclaredAccessibility != Accessibility.Public)
                    continue;
                if (SyntaxHelper.HasAttribute(inner, SyntaxHelper.TransientAttr))
                    continue;

                var columnName = overrides.TryGetValue(inner.Name, out var overridden)
                    ? overridden
                    : SyntaxHelper.ToSnakeCase(prefix) + "_" + SyntaxHelper.ToSnakeCase(inner.Name);

                var innerModel = BuildPropertyModel(inner, ctx);
                if (innerModel is null)
                    continue;

                innerNames.Add(inner.Name);
                properties.Add(new PropertyModel
                {
                    PropertyName = prefix + inner.Name,
                    ColumnName = columnName,
                    ClrTypeName = innerModel.ClrTypeName,
                    IsEmbeddedColumn = true,
                    EmbeddedOwner = prefix,
                    EmbeddedInner = inner.Name,
                    IsSortable = innerModel.IsSortable,
                    Insertable = innerModel.Insertable,
                    Updatable = innerModel.Updatable,
                    Nullable = innerModel.Nullable,
                    ConverterTypeName = innerModel.ConverterTypeName,
                    ConverterColumnClrTypeName = innerModel.ConverterColumnClrTypeName,
                    ColumnTransformer = innerModel.ColumnTransformer,
                    Formula = innerModel.Formula,
                });
            }

            sites.Add(new EmbeddedModel
            {
                PropertyName = prefix,
                EmbeddableTypeName = embedType.Name,
                EmbeddableTypeFqn = embedType.ToDisplayString(),
                Overrides = overrides.Select(kv => (kv.Key, kv.Value)).ToList(),
                InnerPropertyNames = innerNames,
            });
        }

        return sites;
    }

    private static List<EntityListenerModel> BuildEntityListeners(INamedTypeSymbol entity)
    {
        var listeners = new List<EntityListenerModel>();
        foreach (var attr in SyntaxHelper.GetAttributes(entity, SyntaxHelper.EntityListenersAttr))
        {
            foreach (var arg in attr.ConstructorArguments)
            {
                foreach (var value in arg.Values)
                {
                    if (value.Value is not INamedTypeSymbol listenerType)
                        continue;
                    listeners.Add(new EntityListenerModel
                    {
                        TypeFqn = listenerType.ToDisplayString(),
                        Methods = BuildListenerLifecycleMethods(listenerType),
                    });
                }
            }
        }
        return listeners;
    }

    private static List<LifecycleMethodModel> BuildListenerLifecycleMethods(INamedTypeSymbol listenerType)
    {
        var methods = new List<LifecycleMethodModel>();
        foreach (var member in listenerType.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.MethodKind != MethodKind.Ordinary || member.IsStatic)
                continue;

            if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostLoadAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostLoad });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PrePersistAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PrePersist });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostPersistAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostPersist });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PreUpdateAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PreUpdate });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostUpdateAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostUpdate });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PreRemoveAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PreRemove });
            else if (SyntaxHelper.HasAttribute(member, SyntaxHelper.PostRemoveAttr))
                methods.Add(new LifecycleMethodModel { MethodName = member.Name, Kind = LifecycleKind.PostRemove });
        }
        return methods;
    }

    private static List<SecondaryTableModel> BuildSecondaryTables(INamedTypeSymbol entity, IEnumerable<PropertyModel> properties)
    {
        var stAttrs = SyntaxHelper.GetAttributes(entity, SyntaxHelper.SecondaryTableAttr).ToList();
        if (!stAttrs.Any()) return [];

        return stAttrs.Select(a =>
        {
            var tableName = SyntaxHelper.GetConstructorArg<string>(a, 0) ?? string.Empty;
            var pkJoinCol = SyntaxHelper.GetNamedArg<string>(a, "PrimaryKeyJoinColumn") ?? string.Empty;
            var propsInTable = properties
                .Where(p => string.Equals(p.SecondaryTable, tableName, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.PropertyName)
                .ToList();
            return new SecondaryTableModel
            {
                TableName = tableName,
                PrimaryKeyJoinColumn = pkJoinCol,
                PropertyNames = propsInTable,
            };
        }).ToList();
    }

    private static ElementCollectionModel? BuildElementCollectionModel(IPropertySymbol prop, INamedTypeSymbol entity)
    {
        var ctAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.CollectionTableAttr);
        var tableName = SyntaxHelper.GetConstructorArg<string>(ctAttr, 0) ?? string.Empty;
        var joinColumn = SyntaxHelper.GetNamedArg<string>(ctAttr, "JoinColumn");
        if (string.IsNullOrWhiteSpace(joinColumn))
            joinColumn = SyntaxHelper.ToSnakeCase(entity.Name) + "_id";

        var colAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.ColumnAttr);
        var ocAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.OrderColumnAttr);
        var orderColumn = SyntaxHelper.GetNamedArg<string>(ocAttr, "Name")
                          ?? SyntaxHelper.GetConstructorArg<string>(ocAttr, 0);

        var (elementFqn, _, _, _) = ResolveCollectionElementType(prop);
        if (elementFqn is null && prop.Type is INamedTypeSymbol navType && navType.TypeKind == TypeKind.Class)
            elementFqn = FormatType(navType.TypeArguments.FirstOrDefault() ?? navType);

        var elementType = prop.Type is INamedTypeSymbol lazyType && lazyType.TypeArguments.Length > 0
            ? lazyType.TypeArguments[0]
            : null;

        var isEmbeddable = elementType is INamedTypeSymbol namedElement
            && SyntaxHelper.HasAttribute(namedElement, SyntaxHelper.EmbeddableAttr);

        IReadOnlyList<string> valueColumns;
        IReadOnlyList<string> valuePropertyNames;
        if (isEmbeddable && elementType is INamedTypeSymbol embedType)
        {
            var overrides = prop.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == SyntaxHelper.AttributeOverrideAttr)
                .Select(a => (
                    Property: SyntaxHelper.GetConstructorArg<string>(a, 0) ?? string.Empty,
                    Column: SyntaxHelper.GetConstructorArg<string>(a, 1) ?? string.Empty))
                .Where(x => !string.IsNullOrEmpty(x.Property))
                .ToDictionary(x => x.Property, x => x.Column, StringComparer.Ordinal);

            var embedProps = embedType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
                .Where(p => !SyntaxHelper.HasAttribute(p, SyntaxHelper.TransientAttr))
                .ToList();
            valuePropertyNames = embedProps.Select(p => p.Name).ToList();
            valueColumns = embedProps
                .Select(p => overrides.TryGetValue(p.Name, out var col)
                    ? col
                    : SyntaxHelper.ToSnakeCase(p.Name))
                .ToList();
        }
        else
        {
            var valueColumn = SyntaxHelper.GetNamedArg<string>(colAttr, "Name")
                              ?? SyntaxHelper.ToSnakeCase(prop.Name);
            valueColumns = [valueColumn];
            valuePropertyNames = [];
        }

        return new ElementCollectionModel
        {
            PropertyName = prop.Name,
            CollectionTable = tableName,
            JoinColumn = joinColumn,
            ElementTypeName = elementFqn ?? "object",
            IsEmbeddable = isEmbeddable,
            ValueColumns = valueColumns,
            ValuePropertyNames = valuePropertyNames,
            OrderColumnName = orderColumn,
        };
    }

    private static List<NamedEntityGraphModel> BuildNamedEntityGraphs(INamedTypeSymbol entity)
    {
        var standaloneSubGraphs = SyntaxHelper.GetAttributes(entity, SyntaxHelper.SubGraphAttr)
            .Select(attr => (
                GraphName: SyntaxHelper.GetNamedArg<string>(attr, "GraphName"),
                Relationship: SyntaxHelper.GetConstructorArg<string>(attr, 0) ?? string.Empty,
                Nodes: SyntaxHelper.GetStringArrayNamedArg(attr, "AttributeNodes")))
            .Where(s => !string.IsNullOrEmpty(s.Relationship))
            .ToList();

        return SyntaxHelper.GetAttributes(entity, SyntaxHelper.NamedEntityGraphAttr)
            .Select(attr =>
            {
                var name = SyntaxHelper.GetConstructorArg<string>(attr, 0) ?? string.Empty;
                var attributeNodes = SyntaxHelper.GetStringArrayNamedArg(attr, "AttributeNodes");
                var subGraphs = SyntaxHelper.GetStringArrayNamedArg(attr, "SubGraphs")
                    .Select(rel => new SubGraphModel
                    {
                        RelationshipProperty = rel,
                        AttributeNodes = [],
                    })
                    .ToList();

                foreach (var sg in standaloneSubGraphs)
                {
                    if (!string.IsNullOrEmpty(sg.GraphName)
                        && !string.Equals(sg.GraphName, name, StringComparison.Ordinal))
                        continue;
                    subGraphs.Add(new SubGraphModel
                    {
                        RelationshipProperty = sg.Relationship,
                        AttributeNodes = sg.Nodes,
                    });
                }

                return new NamedEntityGraphModel
                {
                    Name = name,
                    AttributeNodes = attributeNodes,
                    SubGraphs = subGraphs,
                };
            })
            .Where(g => !string.IsNullOrEmpty(g.Name))
            .ToList();
    }

    /// <summary>Collects [GlobalFilter] from the entity and MappedSuperclass ancestors; subclass filters override by name.</summary>
    private static List<GlobalFilterModel> ResolveGlobalFilters(INamedTypeSymbol entity)
    {
        var byName = new Dictionary<string, GlobalFilterModel>(StringComparer.Ordinal);
        var chain = new List<INamedTypeSymbol>();
        var current = entity;
        while (true)
        {
            chain.Add(current);
            var baseType = current.BaseType;
            if (baseType is null || baseType.SpecialType == SpecialType.System_Object)
                break;
            if (!SyntaxHelper.HasAttribute(baseType, SyntaxHelper.MappedSuperclassAttr))
                break;
            current = baseType;
        }

        // Base-first so entity declarations override superclass filters with the same name.
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            foreach (var attr in SyntaxHelper.GetAttributes(chain[i], SyntaxHelper.GlobalFilterAttr))
            {
                var name = SyntaxHelper.GetConstructorArg<string>(attr, 0) ?? string.Empty;
                if (string.IsNullOrEmpty(name))
                    continue;
                byName[name] = new GlobalFilterModel
                {
                    Name = name,
                    Condition = SyntaxHelper.GetConstructorArg<string>(attr, 1) ?? string.Empty,
                };
            }
        }

        return byName.Values.ToList();
    }

    /// <summary>Resolves [SoftDelete] on the entity or an ancestor [MappedSuperclass].</summary>
    private static AttributeData? ResolveSoftDeleteAttribute(INamedTypeSymbol entity)
    {
        var current = entity;
        while (true)
        {
            var attr = SyntaxHelper.GetAttribute(current, SyntaxHelper.SoftDeleteAttr);
            if (attr is not null)
                return attr;

            var baseType = current.BaseType;
            if (baseType is null || baseType.SpecialType == SpecialType.System_Object)
                return null;

            if (!SyntaxHelper.HasAttribute(baseType, SyntaxHelper.MappedSuperclassAttr))
                return null;

            current = baseType;
        }
    }
}
