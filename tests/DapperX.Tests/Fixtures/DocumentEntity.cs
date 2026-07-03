using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

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
