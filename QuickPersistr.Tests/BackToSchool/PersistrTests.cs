using QuickCheckr;
using QuickPersistr.Tests.BackToSchool.Model;

namespace QuickPersistr.Tests.BackToSchool;

public class PersistrTests
{
    [Fact]
    public void FromClass()
    {
        new CoursePersistence().Define().GetCheckr(() => new BackToSchoolPersistenceScope())
            .Configure(a => a with { FileAs = "FromClass" })
            .Run(4.ExecutionsPerRun());
    }

    public class CoursePersistence : Persistence<Course>
    {
        public override IPersistenceSpecification Define() =>
            Entity
                .PrimaryKey(a => a.Id)
                .Property(a => a.Title)
                .Persist();
    }
}
