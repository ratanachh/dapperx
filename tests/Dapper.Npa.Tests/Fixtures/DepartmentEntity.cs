using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("departments")]
public class Department
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(Employee.Department))]
    [MapKey("employee_code")]
    public LazyMap<string, Employee> EmployeesByCode { get; set; } = new(e => e.EmployeeCode);
}

[Entity]
[Table("employees")]
public class Employee
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "department_id")]
    public int DepartmentId { get; set; }

    [Column(Name = "employee_code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Column]
    public string FullName { get; set; } = string.Empty;

    [ManyToOne]
    [JoinColumn("department_id")]
    public Department Department { get; set; } = null!;
}

[Repository]
public interface IDepartmentRepository : IRepository<Department, int>
{
}
