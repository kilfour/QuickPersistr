using QuickFuzzr;
using QuickPersistr.Tests.Trickier.Model;
using QuickPulse.Show;

namespace QuickPersistr.Tests.Trickier;

public class TrickierPersistrTests
{
    [Fact]
    public void DomainPersist()
    {
        Persistr
            .Named("Trickier")
            .DomainConfiguration(Fuzz.TheDomain)
            .Scope(() => new TrickierPersistenceScope())
            .Entities(new CoursePersistence())
            .StoreCaseFiles()
            .Run();
    }

    [Fact(Skip = "explicit")]
    public void FuzzrCheck() =>
        Fuzz.TheDomain.One<Course>().Generate().PulseToQuickLog();
}
