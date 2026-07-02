using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("documents")]
[SecondaryTable("document_details", PrimaryKeyJoinColumn = "document_id")]
public class Document
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Title { get; set; } = string.Empty;

    [Column(Table = "document_details")]
    public string Summary { get; set; } = string.Empty;
}

[Repository]
public interface IDocumentRepository : IRepository<Document, int>
{
}
