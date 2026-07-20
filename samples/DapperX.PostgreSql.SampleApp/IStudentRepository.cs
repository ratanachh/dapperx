using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;

namespace DapperX.PostgreSql.SampleApp;

[Repository]
public interface IStudentRepository : IRepository<Student, int>
{
}
