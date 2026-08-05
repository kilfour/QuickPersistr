using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.Trickier;

public class TrickierPersistrTests : PersistrTest<TrickierPersistrTests>
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
            .Named("Trickier")
            .DomainConfiguration(Fuzz.TheDomain)
            .Scope(() => new TrickierPersistenceScope())
            .Entities(new CoursePersistence())
            .StoreCaseFiles(journalist)
            .Run();

    protected override void Verify(Article article)
    {
        Assert.Equal("", article.FailureDescription());
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(12, article.Total().PassedExpectations());
        Assert.Equal("Can Create Course", article.PassedExpectation(1).Read().Label);
        Assert.Equal(1, article.PassedExpectation(1).Read().TimesPassed);
        Assert.Equal("Can Read Course.Name", article.PassedExpectation(2).Read().Label);
        Assert.Equal(1, article.PassedExpectation(2).Read().TimesPassed);
        Assert.Equal("Can Read Course.TimeRange", article.PassedExpectation(3).Read().Label);
        Assert.Equal(1, article.PassedExpectation(3).Read().TimesPassed);
        Assert.Equal("Can Read Course.DateRange", article.PassedExpectation(4).Read().Label);
        Assert.Equal(1, article.PassedExpectation(4).Read().TimesPassed);
        Assert.Equal("Can Read Course.Days", article.PassedExpectation(5).Read().Label);
        Assert.Equal(1, article.PassedExpectation(5).Read().TimesPassed);
        Assert.Equal("Can Read Course.IsDeleted", article.PassedExpectation(6).Read().Label);
        Assert.Equal(1, article.PassedExpectation(6).Read().TimesPassed);
        Assert.Equal("Can Update Course.Name", article.PassedExpectation(7).Read().Label);
        Assert.Equal(1, article.PassedExpectation(7).Read().TimesPassed);
        Assert.Equal("Can Update Course.TimeRange", article.PassedExpectation(8).Read().Label);
        Assert.Equal(1, article.PassedExpectation(8).Read().TimesPassed);
        Assert.Equal("Can Update Course.DateRange", article.PassedExpectation(9).Read().Label);
        Assert.Equal(1, article.PassedExpectation(9).Read().TimesPassed);
        Assert.Equal("Can Update Course.Days", article.PassedExpectation(10).Read().Label);
        Assert.Equal(1, article.PassedExpectation(10).Read().TimesPassed);
        Assert.Equal("Can Update Course.IsDeleted", article.PassedExpectation(11).Read().Label);
        Assert.Equal(1, article.PassedExpectation(11).Read().TimesPassed);
        Assert.Equal("Can Delete Course", article.PassedExpectation(12).Read().Label);
        Assert.Equal(1, article.PassedExpectation(12).Read().TimesPassed);
    }
}
