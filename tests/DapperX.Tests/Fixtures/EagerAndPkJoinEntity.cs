using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("accounts")]
public class Account
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;
}

[Entity]
[Table("orders_with_eager_account")]
public class EagerOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "account_id")]
    public int AccountId { get; set; }

    [ManyToOne(Fetch = FetchType.Eager)]
    [JoinColumn("account_id")]
    public Account Account { get; set; } = null!;
}

[Repository]
public interface IEagerOrderRepository : IRepository<EagerOrder, int>
{
}

[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Email { get; set; } = string.Empty;

    [OneToOne]
    [PrimaryKeyJoinColumn]
    public UserProfile Profile { get; set; } = null!;
}

[Entity]
[Table("user_profiles")]
public class UserProfile
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string DisplayName { get; set; } = string.Empty;
}

[Repository]
public interface IUserRepository : IRepository<User, int>
{
}
