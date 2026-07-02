using Dapper.Npa.Generator.Cpql;
using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Tests;

public class CpqlFilterTests
{
    [Fact]
    public void Translate_select_on_tenant_entity_appends_tenant_filter()
    {
        var entity = new EntityModel
        {
            Namespace = "Dapper.Npa.Tests.Fixtures",
            ClassName = "TenantScopedItem",
            TableName = "tenant_scoped_items",
            TenantIdColumn = "tenant_id",
            Properties =
            [
                new PropertyModel { PropertyName = "Id", ColumnName = "id", IsId = true },
                new PropertyModel { PropertyName = "Name", ColumnName = "name" },
                new PropertyModel { PropertyName = "TenantId", ColumnName = "tenant_id" },
            ],
        };
        var models = new Dictionary<string, EntityModel> { [entity.FullyQualifiedName] = entity };
        var ast = CpqlParser.Parse("SELECT t FROM TenantScopedItem t");
        var ctx = new CpqlTranslationContext(entity, "SqlServer", models);
        var sql = CpqlTranslator.Translate(ast, ctx);
        Assert.Contains("tenant_id = @tenantId", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_select_on_soft_delete_entity_appends_active_filter()
    {
        var entity = new EntityModel
        {
            Namespace = "Dapper.Npa.Tests.Fixtures",
            ClassName = "ArchivedItem",
            TableName = "archived_items",
            SoftDeleteColumn = "is_deleted",
            Properties =
            [
                new PropertyModel { PropertyName = "Id", ColumnName = "id", IsId = true },
                new PropertyModel { PropertyName = "Name", ColumnName = "name" },
                new PropertyModel { PropertyName = "IsDeleted", ColumnName = "is_deleted" },
            ],
        };
        var models = new Dictionary<string, EntityModel> { [entity.FullyQualifiedName] = entity };
        var ast = CpqlParser.Parse("SELECT a FROM ArchivedItem a");
        var ctx = new CpqlTranslationContext(entity, "SqlServer", models);
        var sql = CpqlTranslator.Translate(ast, ctx);
        Assert.Contains("is_deleted = 0", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_delete_on_soft_delete_entity_uses_soft_delete_update()
    {
        var entity = new EntityModel
        {
            Namespace = "Dapper.Npa.Tests.Fixtures",
            ClassName = "ArchivedItem",
            TableName = "archived_items",
            SoftDeleteColumn = "is_deleted",
            Properties =
            [
                new PropertyModel { PropertyName = "Id", ColumnName = "id", IsId = true },
                new PropertyModel { PropertyName = "IsDeleted", ColumnName = "is_deleted" },
            ],
        };
        var models = new Dictionary<string, EntityModel> { [entity.FullyQualifiedName] = entity };
        var ast = CpqlParser.Parse("DELETE FROM ArchivedItem a WHERE a.Id = :id");
        var ctx = new CpqlTranslationContext(entity, "SqlServer", models);
        var sql = CpqlTranslator.Translate(ast, ctx);
        Assert.Contains("UPDATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("is_deleted = 1", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);
    }
}
