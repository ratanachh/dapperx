using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.MySql.SampleApp;

[Entity]
[Table("students")]
public class Student
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public string Id { get; set; } = string.Empty;


    public string Name {get; set; } = string.Empty;
}
