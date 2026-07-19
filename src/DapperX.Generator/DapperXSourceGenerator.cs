namespace DapperX.Generator;

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Builders;
using Emitters;
using Models;
using Utils;
using Generators;
using Validation;

[Generator]
public sealed class DapperXSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entityClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                transform: static (ctx, ct) => GetEntityClassFqn(ctx, ct))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        var repoInterfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is InterfaceDeclarationSyntax,
                transform: static (ctx, ct) => GetRepositoryInterface(ctx, ct))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        var providerFromBuild = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
        {
            if (options.GlobalOptions.TryGetValue("build_property.DapperXDatabaseProvider", out var value)
                && !string.IsNullOrWhiteSpace(value))
                return value;
            return string.Empty;
        });

        var compilationEntitiesInterfaces = context.CompilationProvider
            .Combine(entityClasses.Collect())
            .Combine(repoInterfaces.Collect())
            .Combine(providerFromBuild);

        context.RegisterSourceOutput(compilationEntitiesInterfaces, static (spc, source) =>
        {
            var (((compilation, entityFqns), interfaces), msBuildProvider) = source;
            var provider = CompileTimeDatabaseProvider.Resolve(compilation, msBuildProvider);

            var interfaceByEntity = BuildInterfaceLookup(compilation, interfaces, spc);

            var models = new Dictionary<string, EntityModel>(StringComparer.Ordinal);
            foreach (var entityFqn in entityFqns)
            {
                var entitySymbol = ResolveType(compilation, entityFqn);
                if (entitySymbol is null) continue;

                var model = MetadataBuilder.Build(entitySymbol, spc);
                if (model is null) continue;

                var key = NormalizeEntityFqn(entityFqn);
                models[key] = model;
            }

            RelationshipMetadataEnricher.Enrich(models, compilation, spc);

            foreach (var kvp in models)
            {
                var entityFqn = kvp.Key;
                var model = kvp.Value;
                var entitySymbol = ResolveType(compilation, entityFqn);
                if (entitySymbol is null) continue;

                MappingValidator.Validate(model, entitySymbol, spc, provider, compilation);
                NamedEntityGraphValidator.ValidateSubGraphNodes(model, entitySymbol, models, spc);
                RelationshipValidator.Validate(model, entitySymbol, spc, provider);
                MapKeyValidator.Validate(model, entitySymbol, spc, models);
                CompositeKeyGenerator.ValidateEntity(model, entitySymbol.Locations.FirstOrDefault(), spc);

                interfaceByEntity.TryGetValue(entityFqn, out var repositoryInterface);
                CompositeKeyGenerator.ValidateRepositoryInterface(model, repositoryInterface, spc);

                var implName = repositoryInterface?.ImplClassName
                    ?? RepositoryNaming.DefaultImplClassName(model.ClassName);
                var outputNamespace = repositoryInterface?.Namespace
                    ?? (string.IsNullOrEmpty(model.Namespace) ? "Generated" : $"{model.Namespace}.Generated");

                var repoSource = RepositoryEmitter.Emit(model, provider, repositoryInterface, spc, models, compilation);
                spc.AddSource($"{implName}.g.cs", repoSource);

                var lifecycleSource = LifecycleEmitter.Emit(model, outputNamespace);
                if (lifecycleSource is not null)
                {
                    spc.AddSource($"{model.ClassName}LifecycleInvoker.g.cs", lifecycleSource);
                }
            }

            if (models.Count > 0)
            {
                var allModels = models.Values.ToList();
                var diSource = DiExtensionEmitter.Emit(allModels, interfaceByEntity);
                spc.AddSource("DapperXServiceCollectionExtensions.g.cs", diSource);

                var connectionFactorySource = ConnectionFactoryEmitter.Emit(provider);
                spc.AddSource("DapperXConnectionFactory.g.cs", connectionFactorySource);
            }
        });
    }

    private static string NormalizeEntityFqn(string fqn)
        => fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring("global::".Length) : fqn;

    private static Dictionary<string, RepositoryInterfaceModel> BuildInterfaceLookup(
        Compilation compilation,
        ImmutableArray<RepoInterfacePair> interfaces,
        SourceProductionContext spc)
    {
        var map = new Dictionary<string, RepositoryInterfaceModel>(StringComparer.Ordinal);
        foreach (var pair in interfaces)
        {
            var ifaceSymbol = ResolveType(compilation, pair.InterfaceFqn);
            var entitySymbol = ResolveType(compilation, pair.EntityFqn);
            if (ifaceSymbol is null || entitySymbol is null) continue;

            var repoBase = ifaceSymbol.AllInterfaces.FirstOrDefault(i =>
                i.Name == "IRepository" && i.TypeArguments.Length == 2);
            var idTypeSymbol = repoBase?.TypeArguments[1];

            var declaredMethods = pair.DeclaredMethodKeys
                .Select(key => MethodSymbolKey.Resolve(ifaceSymbol, key))
                .Where(m => m is not null)
                .Cast<IMethodSymbol>()
                .ToList();

            var implClassName = RepositoryNaming.DeriveImplClassName(ifaceSymbol.Name);
            var ns = ifaceSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

            var model = new RepositoryInterfaceModel
            {
                InterfaceFqn = pair.InterfaceFqn,
                ImplClassName = implClassName,
                Namespace = ns,
                IdTypeName = idTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object",
                DeclaredMethods = declaredMethods,
            };

            if (map.TryGetValue(NormalizeEntityFqn(pair.EntityFqn), out var existing))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicateRepositoryInterface,
                    ifaceSymbol.Locations.FirstOrDefault(),
                    ifaceSymbol.Name,
                    existing.InterfaceFqn));
                continue;
            }

            map[NormalizeEntityFqn(pair.EntityFqn)] = model;
        }

        return map;
    }

    private static INamedTypeSymbol? ResolveType(Compilation compilation, string fullyQualifiedName)
    {
        var metadataName = ToMetadataName(fullyQualifiedName);
        return compilation.GetTypeByMetadataName(metadataName);
    }

    private static string ToMetadataName(string fullyQualifiedName)
        => fullyQualifiedName.StartsWith("global::", StringComparison.Ordinal)
            ? fullyQualifiedName.Substring("global::".Length)
            : fullyQualifiedName;

    private static string? GetEntityClassFqn(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.Node is not ClassDeclarationSyntax classDecl) return null;
        var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct) as INamedTypeSymbol;
        if (symbol is null) return null;
        return SyntaxHelper.HasAttribute(symbol, SyntaxHelper.EntityAttr)
            ? symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : null;
    }

    private static RepoInterfacePair? GetRepositoryInterface(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.Node is not InterfaceDeclarationSyntax ifaceDecl) return null;
        var symbol = ctx.SemanticModel.GetDeclaredSymbol(ifaceDecl, ct) as INamedTypeSymbol;
        if (symbol is null) return null;

        if (!SyntaxHelper.HasAttribute(symbol, SyntaxHelper.RepositoryAttr)) return null;

        var repoBase = symbol.AllInterfaces.FirstOrDefault(i =>
            i.Name == "IRepository" && i.TypeArguments.Length == 2);
        if (repoBase is null) return null;

        var entityType = repoBase.TypeArguments[0] as INamedTypeSymbol;
        if (entityType is null) return null;
        if (!SyntaxHelper.HasAttribute(entityType, SyntaxHelper.EntityAttr)) return null;

        var declaredMethodKeys = ifaceDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(m => ctx.SemanticModel.GetDeclaredSymbol(m, ct) as IMethodSymbol)
            .Where(m => m is not null)
            .Select(m => MethodSymbolKey.Format(m!))
            .ToList();

        var ifaceFqn = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var entityFqn = entityType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new RepoInterfacePair(ifaceFqn, entityFqn, declaredMethodKeys);
    }

    private sealed class RepoInterfacePair
    {
        public string InterfaceFqn { get; }
        public string EntityFqn { get; }
        public IReadOnlyList<string> DeclaredMethodKeys { get; }
        public RepoInterfacePair(string ifaceFqn, string entityFqn, IReadOnlyList<string> declaredMethodKeys)
        {
            InterfaceFqn = ifaceFqn;
            EntityFqn = entityFqn;
            DeclaredMethodKeys = declaredMethodKeys;
        }
    }

    /// <summary>Stable key for interface methods so overloads are not collapsed by name alone.</summary>
    private static class MethodSymbolKey
    {
        private const string Sep = "\u001F";

        public static string Format(IMethodSymbol method)
        {
            var paramTypes = method.Parameters
                .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            return string.Join(Sep, new[] { method.Name }.Concat(paramTypes));
        }

        public static IMethodSymbol? Resolve(INamedTypeSymbol iface, string key)
        {
            var parts = key.Split([Sep], StringSplitOptions.None);
            if (parts.Length == 0)
                return null;

            var name = parts[0];
            var paramTypes = parts.Skip(1).ToList();

            foreach (var candidate in iface.GetMembers(name).OfType<IMethodSymbol>())
            {
                if (candidate.Parameters.Length != paramTypes.Count)
                    continue;

                var matches = true;
                for (var i = 0; i < paramTypes.Count; i++)
                {
                    var actual = candidate.Parameters[i].Type
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (actual != paramTypes[i])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return candidate;
            }

            return null;
        }
    }

}
