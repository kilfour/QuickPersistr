using QuickPersistr.Tests.BackToSchool.Model;

namespace QuickPersistr.Tests.BackToSchool;

public class PersistrTests
{
    [Fact]
    public void TestIt()
    {
        Persistr
            .Named("Back to School")
            .Scope(() => new BackToSchoolPersistenceScope())
            .Reader(a => a.Context)
            .Entities();
    }
}