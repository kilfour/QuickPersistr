using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPersistr.Tests.BackToSchool.Model;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.BackToSchool;

[DocFile]
public class PersistrTests : PersistrTest<PersistrTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    [CodeSnippet]
    [CodeRemove(".StoreCaseFiles(TheJournalist)")]
    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("BackToSchool")
            .Scope(() => new BackToSchoolPersistenceScope())
            .Entities(
                new CoursePersistence(),
                new StudentPersistence())
            .StoreCaseFiles(journalist)
            .Run();

    protected override void Verify(Article article)
    {

    }
}

public class CoursePersistence : BackToSchoolPersistence<Course>
{
    public override IPersistenceSpecification<BackToSchoolDbContext> Define() =>
        Entity
            .PrimaryKey(a => a.Id)
            .Property(a => a.Title)
            .Property(a => a.Description)
            .HasMany(many => many
                .From(new StudentPersistence())
                .AddOne((course, student) => course.Students.Add(student))
                .Added((course, student) => course.Students.Any(a => a.Id == student.Id))
                .Reload((reader, id) => reader.Query(
                    a => a.Set<Course>()
                        .Include(course => course.Students)
                        .Single(course => course.Id == id)))
                .Clear(a => a.Students.Clear())
                .Cleared(a => a.Students.Count == 0))
            .Persist();
}

public class StudentPersistence : BackToSchoolPersistence<Student>
{
    public override IPersistenceSpecification<BackToSchoolDbContext> Define() =>
        Entity
            .PrimaryKey(a => a.Id)
            .Property(a => a.Name)
            .Persist();
}