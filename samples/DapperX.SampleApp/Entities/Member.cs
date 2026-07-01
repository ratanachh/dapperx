using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.SampleApp.Entities;

[Entity]
[Table("members")]
[SecondaryTable("member_profiles", PrimaryKeyJoinColumn = "member_id")]
public class Member
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Email { get; set; } = string.Empty;

    [Column(Table = "member_profiles")]
    public string Bio { get; set; } = string.Empty;
}
