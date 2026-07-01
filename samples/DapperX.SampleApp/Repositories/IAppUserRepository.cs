using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.SampleApp.Entities;

namespace DapperX.SampleApp.Repositories;

[Repository]
public interface IAppUserRepository : IRepository<AppUser, int>
{
    Task<IReadOnlyList<AppUser>> FindByEmailAsync(string email, CancellationToken ct = default);
}
