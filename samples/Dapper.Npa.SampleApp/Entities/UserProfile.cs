using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.SampleApp.Entities;

[Entity]
[Table("user_profiles")]
public class UserProfile
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column(Name = "display_name")]
    public string DisplayName { get; set; } = string.Empty;
}
