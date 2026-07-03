namespace DapperX.Tests;

public class ConverterGenerationTests
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
    public void ConverterOrderRepositoryImpl_emits_enum_converter_on_read_and_write()
    {
        var source = ReadGenerated("ConverterOrderRepositoryImpl.g.cs");

        Assert.Contains("EnumToStringConverter", source);
        Assert.Contains("_conv_Status", source);
        Assert.Contains("DbExecutor.ConvertToProperty(_conv_Status.ToProperty", source);
        Assert.Contains("DbExecutor.ConvertToColumn(_conv_Status.ToColumn", source);
        Assert.Contains("ConverterOrderDbRow", source);
    }
}
