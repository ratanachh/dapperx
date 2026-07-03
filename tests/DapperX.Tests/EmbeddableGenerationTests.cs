namespace DapperX.Tests;

public class EmbeddableGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            implFileName));
        return File.ReadAllText(path);
    }

    [Fact]
    public void ProductRepositoryImpl_uses_db_row_mapping_and_mutation_parameters()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");

        Assert.Contains("private sealed class ProductDbRow", source);
        Assert.Contains("MapFromDbRow", source);
        Assert.Contains("BuildMutationParameters", source);
        Assert.Contains("AddressCity = entity.Address?.City", source);
        // Product.Address is declared non-nullable, so MapFromDbRow must always construct an instance
        // rather than null it out when the underlying columns are NULL (that would violate the entity's
        // own nullability contract).
        Assert.Contains("entity.Address = new", source);
        Assert.DoesNotContain("entity.Address = null", source);
        Assert.Contains("address_city", source);
    }

    [Fact]
    public void DualAddressUserRepositoryImpl_applies_attribute_overrides()
    {
        var source = ReadGenerated("DualAddressUserRepositoryImpl.g.cs");

        Assert.Contains("billing_address_city", source);
        Assert.Contains("shipping_city", source);
        Assert.Contains("ShippingAddressCity", source);
        Assert.Contains("MapFromDbRow", source);
    }
}
