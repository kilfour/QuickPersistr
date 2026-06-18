using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuickCheckr;
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
        db.SaveAndClear();

        var reloaded = Assert.Single(db.Context.Courses);
        Assert.Equal(course.Title, reloaded.Title);
    }

    [Fact]
    public void Can_Save_Course_Checkr()
    {
        var courseCreateFuzzr =
            from ignore in Configr.Ignore(a => a.Name == "Id")
            from entity in Fuzzr.One<Course>()
            select entity;

        FuzzrOf<Course> courseModifierFuzzr(Course course) =>
            from ignore in Configr.Ignore(a => a.Name == "Id")
            from entity in Fuzzr.One(() => course)
            select entity;

        var checkr =
            from _ in Checkr.Sequence(
                // CREATE
                from course in Checkr.Input("Course", courseCreateFuzzr)
                from create in Checkr.Act("Create", () =>
                {
                    db.Context.Courses.Add(course);
                    db.SaveAndClear();
                    return course;
                })
                from canCreate in Checkr.Expect("Can Create", () => course.Id != default)
                from stored in Trackr.ToPool("Course", () => create)
                select Case.Closed,
                // READ
                 Trackr.OneOfPool<Course>("Course Read", info =>
                    from course in Checkr.Act("Read",
                        () => db.Context.Courses.AsNoTracking().Single(a => info.Value.Id == a.Id))
                    from canRead in Checkr.Expect("Can Read", () => info.Value.Title == course.Title)
                    select Case.Closed),
                // UPDATE
                Trackr.OneOfPool<Course>("Course Update", info =>
                    from course in Checkr.Capture(
                        () => db.Context.Courses.Single(a => a.Id == info.Value.Id))
                    from updatedCourse in Checkr.Input("Updated Course", courseModifierFuzzr(course))
                    from updated in Checkr.Act("Update", () => { courseModifierFuzzr(course); db.SaveAndClear(); })
                    from reloaded in Checkr.Capture(
                        () => db.Context.Courses.AsNoTracking().Single(a => info.Value.Id == a.Id))
                    from canUpdate in Checkr.Expect("Can Update", () => course.Title == reloaded.Title)
                    from stored in info.Replace(reloaded)
                    select Case.Closed),
                // Delete
                Trackr.OneOfPool<Course>("Course Delete", info =>
                    from delete in Checkr.Act("Delete",
                        () =>
                        {
                            db.Context.Courses.Where(a => info.Value.Id == a.Id).ExecuteDelete();
                            db.SaveAndClear();
                        })
                    from reloaded in Checkr.Capture(
                        () => db.Context.Courses.SingleOrDefault(a => info.Value.Id == a.Id))
                    from canDelete in Checkr.Expect("Can Delete", () => reloaded is null)
                    from stored in info.Remove()
                    select Case.Closed)
            )
            select Case.Closed;

        checkr.Configure(a => a with { FileAs = "test" }).Run(4.ExecutionsPerRun());
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
        db.SaveAndClear();

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
        db.SaveAndClear();

        var reloadedCourse = Assert.Single(db.Context.Courses.Include(c => c.Students));
        Assert.Equal(course.Students.Single().Id, reloadedCourse.Students.Single().Id);
    }
}
