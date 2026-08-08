using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPersistr.Tests.Notes.BackToSchool.Model;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.Notes;

[DocFile]
public class OmittedChildTests : PersistrTest<OmittedChildTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    [CodeSnippet]
    [CodeRemove(".StoreCaseFiles(journalist)")]
    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Omitted child")
            .Scope(() => new OmittedChildScope())
            .Entities(new CoursePersistence())
            .StoreCaseFiles(journalist)
            .Run(164298753);

    protected override void Verify(Article article)
    {
        Assert.Equal("Course Can Add Student", article.FailureDescription());
        Assert.Empty(article.FailingExpectationMessages());
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(2, article.Total().Executions());
        Assert.Equal(4, article.Total().Actions());
        Assert.Equal(2, article.Total().Inputs());
        Assert.Equal(2, article.Total().PoolTraces());
        Assert.Equal(4, article.Total().PassedExpectations());
        Assert.Equal(11, article.ShrinkCount);
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create Course", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).Input(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("Course-1", article.Execution(1).PoolTrace(1).Read().Value);
        Assert.Equal(4, article.Execution(2).Read().ExecutionId);
        Assert.Equal("Add Children", article.Execution(2).Action(1).Read().Label);
        Assert.Equal("Clear Children", article.Execution(2).Action(2).Read().Label);
        Assert.Equal("Children", article.Execution(2).Input(1).Read().Label);
        Assert.Equal("Entity", article.Execution(2).PoolTrace(1).Read().Label);
        Assert.Equal("Course-1", article.Execution(2).PoolTrace(1).Read().Value);
        Assert.Equal("Can Create Course", article.PassedExpectation(1).Read().Label);
        Assert.Equal(1, article.PassedExpectation(1).Read().TimesPassed);
        Assert.Equal("Can Read Course.Id", article.PassedExpectation(2).Read().Label);
        Assert.Equal(1, article.PassedExpectation(2).Read().TimesPassed);
        Assert.Equal("Course Can Remove Student", article.PassedExpectation(3).Read().Label);
        Assert.Equal(1, article.PassedExpectation(3).Read().TimesPassed);
        Assert.Equal("Course Can Clear Student", article.PassedExpectation(4).Read().Label);
        Assert.Equal(1, article.PassedExpectation(4).Read().TimesPassed);
    }

    public class CoursePersistence : Persistence<OmittedChildDbContext, Course>
    {
        public override IPersistenceSpecification<OmittedChildDbContext> Define() =>
            Entity
                .PrimaryKey(course => course.Id)
                .HasMany(many => many
                    .From(new StudentPersistence())
                    .Add((course, student) => course.Students.Add(student))
                    .Remove((course, student) => course.Students.RemoveAll(
                        candidate => candidate.Id == student.Id))
                    .Clear(course => course.Students.Clear())
                    .Reload((reader, id) => reader.Query(db =>
                        db.Set<Course>().Single(course => course.Id == id)))
                    .Contains((course, student) => course.Students.Any(
                        candidate => candidate.Id == student.Id))
                    .Empty(course => course.Students.Count == 0))
                .Persist();
    }

    public class StudentPersistence : Persistence<OmittedChildDbContext, Student>
    {
        public override IPersistenceSpecification<OmittedChildDbContext> Define() =>
            Entity
                .PrimaryKey(student => student.Id)
                .Property(student => student.Name)
                .Persist();
    }

    public class OmittedChildScope()
        : SqlitePersistenceScope<OmittedChildDbContext>(options => new OmittedChildDbContext(options));

    public class OmittedChildDbContext(DbContextOptions<OmittedChildDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Course>().Ignore(course => course.Students);
        }
    }
}
