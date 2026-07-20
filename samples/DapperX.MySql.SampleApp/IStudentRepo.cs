using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;

namespace DapperX.MySql.SampleApp;

[Repository]
public interface IStudentRepository : IRepository<Student, string>
{
    
}