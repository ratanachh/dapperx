using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.SqlServer.SampleApp.Entities;

namespace DapperX.SqlServer.SampleApp.Repositories;

[Repository]
public interface IDepartmentRepository : IRepository<Department, int>
{
}
