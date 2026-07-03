namespace DapperX.Tests;

public class ElementCollectionGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            implFileName));
        Assert.True(File.Exists(path), $"Expected generated file at {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void TaggedProductRepositoryImpl_emits_element_collection_sql_literals()
    {
        var source = ReadGenerated("TaggedProductRepositoryImpl.g.cs");

        Assert.Contains("LoadTagsSql = \"SELECT tags FROM product_tags WHERE product_id = @parentId\"", source);
        Assert.Contains("InsertTagsSql = \"INSERT INTO product_tags (product_id, tags) VALUES (@parentId, @tags)\"", source);
        Assert.Contains("DeleteTagsSql = \"DELETE FROM product_tags WHERE product_id = @parentId\"", source);
    }

    [Fact]
    public void TaggedProductRepositoryImpl_wires_lazy_collection_loader_on_post_load()
    {
        var source = ReadGenerated("TaggedProductRepositoryImpl.g.cs");

        Assert.Contains("protected override void OnPostLoad", source);
        Assert.Contains("WireLazyLoaders", source);
        Assert.Contains("LoadTagsSql", source);
        Assert.Contains("LazyCollection<string>", source);
        Assert.Contains("entity.Tags = new DapperX.Relations.Lazy.LazyCollection<string>", source);
    }

    [Fact]
    public void TaggedProductRepositoryImpl_persists_loaded_tags_on_insert_and_update()
    {
        var source = ReadGenerated("TaggedProductRepositoryImpl.g.cs");

        Assert.Contains("if (entity.Tags.IsLoaded)", source);
        Assert.Contains("await InsertTagsAsync(entity, entity.Tags.TryGet()", source);
        Assert.Contains("await DeleteTagsAsync(entity, transaction, ct);", source);
    }

    [Fact]
    public void TaggedProductRepositoryImpl_deletes_tags_before_parent_delete()
    {
        var source = ReadGenerated("TaggedProductRepositoryImpl.g.cs");

        Assert.Contains("await DeleteTagsAsync(entity, transaction, ct);", source);
        Assert.Contains("DeleteSql", source);
    }

    [Fact]
    public void OrderedTagsProductRepositoryImpl_emits_order_column_sql()
    {
        var source = ReadGenerated("OrderedTagsProductRepositoryImpl.g.cs");

        Assert.Contains("ORDER BY position", source);
        Assert.Contains("INSERT INTO ordered_product_tags (product_id, tag, position)", source);
        Assert.Contains("@position", source);
        Assert.Contains("position = index", source);
    }

    [Fact]
    public void GalleryProductRepositoryImpl_emits_embeddable_element_collection_insert()
    {
        var source = ReadGenerated("GalleryProductRepositoryImpl.g.cs");

        Assert.Contains("INSERT INTO product_images (product_id, image_url, image_caption)", source);
        Assert.Contains("imageUrl = value.Url", source);
        Assert.Contains("imageCaption = value.Caption", source);
    }

    [Fact]
    public void DiagnosticsReporter_defines_DPX011_missing_collection_table()
    {
        var source = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DapperX.Generator", "Utils", "DiagnosticsReporter.cs")));
        Assert.Contains("\"DPX011\"", source);
        Assert.Contains("Missing [CollectionTable]", source);
    }
}
