using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;
using DapperX.Relations.Lazy;

namespace DapperX.Tests.Fixtures;

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
