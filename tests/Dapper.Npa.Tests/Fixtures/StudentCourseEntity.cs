using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("students")]
public class Student
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [ManyToMany(Cascade = CascadeType.All)]
    [JoinTable("student_courses", JoinColumn = "student_id", InverseJoinColumn = "course_id")]
    public LazyCollection<Course> Courses { get; set; } = new();
}

[Entity]
[Table("courses")]
public class Course
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Title { get; set; } = string.Empty;
}

[Repository]
public interface IStudentRepository : IRepository<Student, int>
{
}
