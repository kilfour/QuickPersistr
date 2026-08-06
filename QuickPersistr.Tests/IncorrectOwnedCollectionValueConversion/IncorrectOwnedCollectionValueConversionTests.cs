using Microsoft.EntityFrameworkCore;
using QuickCheckr;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPersistr.Tests.Trickier;
using QuickPersistr.Tests.Trickier.Model;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.IncorrectOwnedCollectionValueConversion;

[DocFile]
public class IncorrectOwnedCollectionValueConversionTests : PersistrTest<IncorrectOwnedCollectionValueConversionTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => true;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    [CodeSnippet]
    [CodeRemove(".StoreCaseFiles(journalist)")]
    protected override void GetPersistr(Journalist journalist)
    {
        Persistr
            .Named("Incorrect owned collection value conversion")
            .DomainConfiguration(Fuzz.TheDomain)
            .Scope(() => new BrokenCourseScope())
            .Entities(new BrokenCoursePersistence())
            .StoreCaseFiles(journalist)
            .Run(1399221127);
    }

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Read Course.Days", article.FailureDescription());
        var messages = article.FailingExpectationMessages();
        Assert.Equal(2, messages.Count);
        Assert.Contains("RemoteLearning", messages[0]);
        Assert.Contains("OnCampus", messages[1]);
        Assert.DoesNotContain("RemoteLearning", messages[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.NotNull(article.Seed());
        Assert.Equal(2, article.Total().Executions());
        Assert.Equal(2, article.Total().Actions());
        Assert.Equal(1, article.Total().Inputs());
        Assert.Equal(2, article.Total().PoolTraces());
        Assert.Equal(2, article.Total().PassedExpectations());
        Assert.True(article.ShrinkCount >= 1);
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create Course", article.Execution(1).Action(1).Read().Label);
        var input = article.Execution(1).Input(1).Read();
        Assert.Equal("Entity", input.Label);
        Assert.Contains(
            "RemoteLearning",
            input.Value.ToString());
        var shrunkCourse = Assert.IsType<Course>(input.InputValue);
        Assert.All(
            shrunkCourse.Days,
            day => Assert.Equal(CourseWeekDay.Monday, day.Day));
        Assert.Contains(
            shrunkCourse.Days,
            day => day.Mode == LearningMode.RemoteLearning);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("Course-1", article.Execution(1).PoolTrace(1).Read().Value);
        Assert.Equal(2, article.Execution(2).Read().ExecutionId);
        Assert.Equal("Read Course", article.Execution(2).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(2).PoolTrace(1).Read().Label);
        Assert.Equal("Course-1", article.Execution(2).PoolTrace(1).Read().Value);
        Assert.Equal("Can Create Course", article.PassedExpectation(1).Read().Label);
        Assert.Equal(1, article.PassedExpectation(1).Read().TimesPassed);
        Assert.Equal("Can Read Course.Id", article.PassedExpectation(2).Read().Label);
        Assert.Equal(1, article.PassedExpectation(2).Read().TimesPassed);
    }
}

public class BrokenCoursePersistence : Persistence<BrokenCourseDbContext, Course>
{
    public override IPersistenceSpecification<BrokenCourseDbContext> Define() =>
        Entity
            .PrimaryKey(course => course.Id)
            .Shrinking(
                // Shrink.OnType<CourseName>(
                //     collapse => collapse.Using(
                //         Collapse.Member(
                //             (CourseName courseName) => courseName.Value,
                //             (courseName, value) => new CourseName(value)))),
                Shrink.OnType<DateRange>(
                    collapse => collapse.Using(
                        Collapse.Member(
                            (DateRange dateRange) => dateRange.StartDate,
                            (dateRange, value) => new DateRange(value, dateRange.EndDate))
                            ,
                        Collapse.Member(
                            (DateRange dateRange) => dateRange.EndDate,
                            (dateRange, value) => new DateRange(dateRange.StartDate, value))
                    )),
                // Shrink.OnType<CourseName>(
                //     collapse => collapse.Using(
                //         Collapse.Member(
                //             (CourseName courseName) => courseName.Value,
                //             (course, value) => new CourseName(value)))),
                Shrink.OnType<Course>(
                    collapse => collapse.Using(
                        Collapse.Member(
                            (Course course) => course.Name,
                            (course, value) =>
                                new Course(
                                    value,
                                    course.DateRange,
                                    course.TimeRange,
                                    course.Days)),
                        Collapse.Member(
                            (Course course) => course.DateRange,
                            (course, value) =>
                                new Course(
                                    course.Name,
                                    value,
                                    course.TimeRange,
                                    course.Days)),
                        Collapse.Member(
                            (Course course) => course.TimeRange,
                            (course, value) =>
                                new Course(
                                    course.Name,
                                    course.DateRange,
                                    value,
                                    course.Days)),
                        Collapse.Member(
                            (Course course) => course.Days,
                            (course, value) =>
                                new Course(
                                    course.Name,
                                    course.DateRange,
                                    course.TimeRange,
                                    [.. value.Select(day => new CourseDay(
                                        CourseWeekDay.Monday,
                                        day.Mode))]))
                    )))
            .Property(
                course => course.Days,
                (expected, actual) => expected.SequenceEqual(actual))
            .Persist();

    private static readonly Shrinker CourseShrinker =
        Shrink.OnType<Course>(
            classify => classify.Classify(
                Try.Function<Course>(CourseCandidates)),
            reduce => reduce.Simplify(
                Reduce.Function<Course>(CourseCandidates)));

    private static IEnumerable<Course> CourseCandidates(Course course)
    {
        yield return new Course(
            course.Name,
            course.DateRange,
            course.TimeRange,
            [.. course.Days.Select(day => new CourseDay(
                CourseWeekDay.Monday,
                day.Mode))]);

        yield return new Course(
            course.Name,
            course.DateRange,
            course.TimeRange,
            [.. course.Days.Select(day => new CourseDay(
                CourseWeekDay.Monday,
                LearningMode.OnCampus))]);
    }
}

public class BrokenCourseScope()
    : EfPersistenceScope<BrokenCourseDbContext>(options => new BrokenCourseDbContext(options));

public class BrokenCourseDbContext(DbContextOptions<BrokenCourseDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CourseConfiguration());

        modelBuilder.Entity<Course>()
            .OwnsMany(course => course.Days, days =>
                days.Property(day => day.Mode)
                    // Bug: RemoteLearning is accidentally stored as OnCampus.
                    .HasConversion(
                        mode => mode == LearningMode.RemoteLearning
                            ? LearningMode.OnCampus
                            : mode,
                        storedMode => storedMode));
    }
}
