using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.SampleApp.Entities;

namespace DapperX.SampleApp.Repositories;

[Repository]
public interface IMemberRepository : IRepository<Member, int>
{
}
