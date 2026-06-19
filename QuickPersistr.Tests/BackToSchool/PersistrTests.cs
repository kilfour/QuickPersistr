using QuickCheckr;
using QuickPersistr.Tests.BackToSchool.Model;

namespace QuickPersistr.Tests.BackToSchool;

public class PersistrTests
{
    [Fact]
    public void FromClass()
    {
        Persistr
            .Named("BackToSchool")
            .Scope(() => new BackToSchoolPersistenceScope())
            .Entities(new CoursePersistence())
            .Run();
    }

    public class CoursePersistence : Persistence<Course>
    {
        public override IPersistenceSpecification Define() =>
            Entity
                .PrimaryKey(a => a.Id)
                .Property(a => a.Title)
                .Property(a => a.Description)
                .Persist();
    }
}
