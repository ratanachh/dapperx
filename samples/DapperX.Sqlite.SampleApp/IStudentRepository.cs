using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;

namespace DapperX.Sqlite.SampleApp;

[Repository]
public interface IStudentRepository : IRepository<Student, int>
{
}
