using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.SqlServer.SampleApp.Entities;

namespace DapperX.SqlServer.SampleApp.Repositories;

[Repository]
public interface IAppUserRepository : IRepository<AppUser, int>
{
    Task<IReadOnlyList<AppUser>> FindByEmailAsync(string email, CancellationToken ct = default);
}
