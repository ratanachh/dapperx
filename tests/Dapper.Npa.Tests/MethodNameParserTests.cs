using Dapper.Npa.Generator.MethodNameParsing;
using LogicalConnector = Dapper.Npa.Generator.MethodNameParsing.LogicalConnector;
using OperatorKind = Dapper.Npa.Generator.MethodNameParsing.OperatorKind;

namespace Dapper.Npa.Tests;

public class MethodNameParserTests
{
    [Fact]
    public void TryParse_FindByName_parses_name_equality()
    {
        var parsed = MethodNameParser.TryParse("FindByNameAsync", ["Id", "Name"]);
        Assert.NotNull(parsed);
        Assert.Equal(SubjectKind.Find, parsed!.Subject);
        Assert.Single(parsed.Conditions);
        Assert.Equal("Name", parsed.Conditions[0].PropertyName);
        Assert.Equal(OperatorKind.Equal, parsed.Conditions[0].Operator);
    }

    [Fact]
    public void TryParse_FindByNameOrderByIdDesc_parses_order_by()
    {
        var parsed = MethodNameParser.TryParse("FindByNameOrderByIdDescAsync", ["Id", "Name"]);
        Assert.NotNull(parsed);
        Assert.Equal("Name", parsed!.Conditions[0].PropertyName);
        Assert.Single(parsed.OrderBySegments);
        Assert.Equal("Id", parsed.OrderBySegments[0].PropertyName);
        Assert.False(parsed.OrderBySegments[0].Ascending);
    }

    [Fact]
    public void TryParse_ExistsByName_parses_exists_subject()
    {
        var parsed = MethodNameParser.TryParse("ExistsByNameAsync", ["Id", "Name"]);
        Assert.NotNull(parsed);
        Assert.Equal(SubjectKind.Exists, parsed!.Subject);
    }

    [Fact]
    public void TryParse_FindByAddressCity_parses_embedded_path()
    {
        var paths = new[] { "Id", "Name", "AddressCity", "AddressCountry", "CustomerId", "CustomerName" };
        var parsed = MethodNameParser.TryParse("FindByAddressCityAsync", paths);
        Assert.NotNull(parsed);
        Assert.Equal("AddressCity", parsed!.Conditions[0].PropertyName);
    }

    [Fact]
    public void TryParse_FindByNameSorted_strips_runtime_suffix()
    {
        var parsed = MethodNameParser.TryParse("FindByNameSortedAsync", ["Id", "Name"]);
        Assert.NotNull(parsed);
        Assert.Equal("Name", parsed!.Conditions[0].PropertyName);
    }

    [Fact]
    public void TryParse_ambiguous_Not_vs_NotDeleted_returns_null()
    {
        var paths = new[] { "Deleted", "NotDeleted" };
        var result = MethodNameParser.TryParseDetailed("FindByNotDeletedAsync", paths);
        Assert.Null(result.Query);
        Assert.True(result.IsAmbiguous);
    }

    [Fact]
    public void TryParse_IsActive_property_wins_over_Is_keyword()
    {
        var parsed = MethodNameParser.TryParse("FindByIsActiveAsync", ["Id", "IsActive"]);
        Assert.NotNull(parsed);
        Assert.Equal("IsActive", parsed!.Conditions[0].PropertyName);
        Assert.Equal(OperatorKind.Equal, parsed.Conditions[0].Operator);
    }

    [Fact]
    public void TryParse_Like_property_is_equality_not_like_operator()
    {
        var parsed = MethodNameParser.TryParse("FindByLikeAsync", ["Id", "Like"]);
        Assert.NotNull(parsed);
        Assert.Equal("Like", parsed!.Conditions[0].PropertyName);
        Assert.Equal(OperatorKind.Equal, parsed.Conditions[0].Operator);
    }

    [Fact]
    public void TryParse_NameLike_uses_like_operator()
    {
        var parsed = MethodNameParser.TryParse("FindByNameLikeAsync", ["Id", "Name"]);
        Assert.NotNull(parsed);
        Assert.Equal("Name", parsed!.Conditions[0].PropertyName);
        Assert.Equal(OperatorKind.Like, parsed.Conditions[0].Operator);
    }

    [Fact]
    public void TryParse_And_joins_two_conditions()
    {
        var parsed = MethodNameParser.TryParse("FindByNameAndIsActiveAsync", ["Id", "Name", "IsActive"]);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed!.Conditions.Count);
        Assert.Equal(LogicalConnector.And, parsed.Conditions[1].Connector);
    }
}

