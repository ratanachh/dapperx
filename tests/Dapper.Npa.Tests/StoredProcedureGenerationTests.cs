namespace Dapper.Npa.Tests;

public class StoredProcedureGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            implFileName));
        Assert.True(File.Exists(path), $"Expected generated file at {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void ProcOrderRepositoryImpl_basic_sp_emits_exec_and_query_async()
    {
        var source = ReadGenerated("ProcOrderRepositoryImpl.g.cs");

        Assert.Contains("ListOrdersSpAsync", source);
        Assert.Contains("\"sp_list_proc_orders\"", source);
        Assert.DoesNotContain("EXEC sp_list_proc_orders", source);
        Assert.Contains("QueryAsync<Dapper.Npa.Tests.Fixtures.ProcOrder>", source);
        Assert.Contains("commandType: System.Data.CommandType.StoredProcedure", source);
    }

    [Fact]
    public void ProcOrderRepositoryImpl_out_params_emit_proc_result_and_output_capture()
    {
        var source = ReadGenerated("ProcOrderRepositoryImpl.g.cs");

        Assert.Contains("ProcessOrderSpAsync", source);
        Assert.Contains("ParameterDirection.Output", source);
        Assert.Contains("ParameterDirection.InputOutput", source);
        Assert.Contains("ExecuteAsync", source);
        Assert.Contains("dp.Get<int>(\"@resultCode\")", source);
        Assert.Contains("dp.Get<string>(\"@message\")", source);
        Assert.Contains("ProcResult<int, string>", source);
    }

    [Fact]
    public void ProcOrderRepositoryImpl_multi_result_emits_query_multiple()
    {
        var source = ReadGenerated("ProcOrderRepositoryImpl.g.cs");

        Assert.Contains("GetOrderReportSpAsync", source);
        Assert.Contains("QueryMultipleAsync", source);
        Assert.Contains("MultiResult<Dapper.Npa.Tests.Fixtures.ProcOrderSummary, Dapper.Npa.Tests.Fixtures.ProcOrderLine>", source);
        Assert.Contains("ReadAsync<Dapper.Npa.Tests.Fixtures.ProcOrderSummary>", source);
        Assert.Contains("ReadAsync<Dapper.Npa.Tests.Fixtures.ProcOrderLine>", source);
    }

    [Fact]
    public void DiagnosticsReporter_defines_stored_procedure_dpx072_through_dpx075()
    {
        var source = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Dapper.Npa.Generator", "Utils", "DiagnosticsReporter.cs")));
        Assert.Contains("\"DPX072\"", source);
        Assert.Contains("\"DPX073\"", source);
        Assert.Contains("\"DPX074\"", source);
        Assert.Contains("\"DPX075\"", source);
    }
}
