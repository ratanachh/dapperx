using Dapper.Npa.SampleApp.Entities;
using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;

namespace Dapper.Npa.SampleApp.Repositories;

[Repository]
public interface IAppUserRepository : IRepository<AppUser, int>
{
    Task<IReadOnlyList<AppUser>> FindByEmailAsync(string email, CancellationToken ct = default);
}
