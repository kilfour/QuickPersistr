using Microsoft.EntityFrameworkCore;
using QuickFuzzr;
using QuickPersistr.Tests.BackToSchool.Model;

namespace QuickPersistr.Tests.BackToSchool;

public class PersistencyTests : IDisposable
{
    private readonly BackToSchoolPersistenceScope db = new();
    public void Dispose() => db.Dispose();

    [Fact]
    public void Can_Save_Course()
    {
        var courseFuzzr =
            from ignore in Configr.Ignore(a => a.Name == "Id")
            from entity in Fuzzr.One<Course>()
            select entity;

        var course = courseFuzzr.Generate();
        db.Context.Courses.Add(course);
        db.Commit();

        var reloaded = Assert.Single(db.Context.Courses);
        Assert.Equal(course.Title, reloaded.Title);
    }

    [Fact]
    public void Can_Save_Course_With_Student()
    {
        var courseFuzzr =
            from ignore in Configr.Ignore(a => a.Name == "Id")
            from student in Fuzzr.One<Student>()
            from entity in Fuzzr.One<Course>().Apply(a => a.Students.Add(student))
            select entity;

        var course = courseFuzzr.Generate();
        db.Context.Courses.Add(course);
        db.Commit();

        var reloadedCourse = Assert.Single(db.Context.Courses);
        Assert.Equal(course.Title, reloadedCourse.Title);

        var reloadedStudent = Assert.Single(db.Context.Students);
        Assert.Equal(course.Students.Single().Name, reloadedStudent.Name);
        Assert.Equal(course.Students.Single().Id, reloadedCourse.Students.Single().Id);
    }

    [Fact]
    public void Can_Load_Students_With_Include()
    {
        var courseFuzzr =
            from ignore in Configr.Ignore(a => a.Name == "Id")
            from student in Fuzzr.One<Student>()
            from entity in Fuzzr.One<Course>().Apply(a => a.Students.Add(student))
            select entity;

        var course = courseFuzzr.Generate();
        db.Context.Courses.Add(course);
        db.Commit();

        var reloadedCourse = Assert.Single(db.Context.Courses.Include(c => c.Students));
        Assert.Equal(course.Students.Single().Id, reloadedCourse.Students.Single().Id);
    }
}
