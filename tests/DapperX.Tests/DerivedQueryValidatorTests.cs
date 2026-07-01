using System.Reflection;
using DapperX.Generator.Generators;

namespace DapperX.Tests;

public class DerivedQueryValidatorTests
{
    private static string BuildRegexPredicate(string column, string paramName, string provider)
    {
        var method = typeof(DerivedQueryGenerator).GetMethod(
            "BuildRegexPredicate",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, [column, paramName, provider])!;
    }

    [Fact]
    public void BuildRegexPredicate_PostgreSql_uses_tilde_operator()
    {
        Assert.Equal("email ~ @pattern", BuildRegexPredicate("email", "pattern", "PostgreSql"));
    }

    [Fact]
    public void BuildRegexPredicate_MySql_uses_regexp()
    {
        Assert.Equal("email REGEXP @pattern", BuildRegexPredicate("email", "pattern", "MySql"));
    }
}
