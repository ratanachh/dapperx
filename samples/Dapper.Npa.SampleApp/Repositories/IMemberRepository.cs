using Dapper.Npa.SampleApp.Entities;
using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;

namespace Dapper.Npa.SampleApp.Repositories;

[Repository]
public interface IMemberRepository : IRepository<Member, int>
{
}
