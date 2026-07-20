using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.PostgreSql.SampleApp;

[Entity]
[Table("students")]
public class Student
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
