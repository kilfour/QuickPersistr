using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPersistr.Tests.BackToSchool;
using QuickPersistr.Tests.BackToSchool.Model;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.RemoveAllInsteadOfOne;

[DocFile]
public class PersistrTests : PersistrTest<PersistrTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => true;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    [CodeSnippet]
    [CodeRemove(".StoreCaseFiles(journalist)")]
    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Remove all instead of one")
            .Scope(() => new BackToSchoolPersistenceScope())
            .Entities(new CoursePersistence())
            .StoreCaseFiles(journalist)
            .Run(751926438);

    protected override void Verify(Article article)
    {
        Assert.Equal("Course Retains Other Student", article.FailureDescription());
        Assert.Equal(
            "Expected: [ { Id: 2, Name: \"gbg\" } ]",
            article.FailingExpectationMessages()[0]);
        Assert.Equal("Actual:   [ ]", article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(2, article.Total().Executions());
        Assert.Equal(4, article.Total().Actions());
        Assert.Equal(2, article.Total().Inputs());
        Assert.Equal(2, article.Total().PoolTraces());
        Assert.Equal(5, article.Total().PassedExpectations());
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
        Assert.Equal("Can Read Course.Id", article.PassedExpectation(2).Read().Label);
        Assert.Equal("Course Can Add Student", article.PassedExpectation(3).Read().Label);
        Assert.Equal("Course Can Remove Student", article.PassedExpectation(4).Read().Label);
        Assert.Equal("Course Can Clear Student", article.PassedExpectation(5).Read().Label);
        Assert.All(
            Enumerable.Range(1, 5),
            index => Assert.Equal(1, article.PassedExpectation(index).Read().TimesPassed));
    }
}

public class CoursePersistence : Persistence<BackToSchoolDbContext, Course>
{
    public override IPersistenceSpecification<BackToSchoolDbContext> Define() =>
        Entity
            .PrimaryKey(course => course.Id)
            .HasMany(many => many
                .From(new StudentPersistence())
                .Add((course, student) => course.Students.Add(student))
                // Bug: removing one Student accidentally removes the entire collection.
                .Remove((course, student) => course.Students.Clear())
                .Clear(course => course.Students.Clear())
                .Reload((reader, id) => reader.Query(db =>
                    db.Set<Course>()
                        .Include(course => course.Students)
                        .Single(course => course.Id == id)))
                .Contains((course, student) => course.Students.Any(
                    candidate => candidate.Id == student.Id))
                .Empty(course => course.Students.Count == 0))
            .Persist();
}

public class StudentPersistence : Persistence<BackToSchoolDbContext, Student>
{
    public override IPersistenceSpecification<BackToSchoolDbContext> Define() =>
        Entity
            .PrimaryKey(student => student.Id)
            .Property(student => student.Name)
            .Persist();
}
