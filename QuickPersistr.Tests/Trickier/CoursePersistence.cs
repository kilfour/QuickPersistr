using QuickPersistr.Tests.Trickier.Model;

namespace QuickPersistr.Tests.Trickier;

public class CoursePersistence : TrickierPersistence<Course>
{
    public override IPersistenceSpecification<TrickierDbContext> Define() =>
        Entity
            .PrimaryKey(a => a.Id)
            .Property(a => a.Name)
            .Property(a => a.TimeRange)
            .Property(a => a.DateRange)
            .Property(a => a.Days, (expected, actual) => expected.SequenceEqual(actual))
            .Property(a => a.IsDeleted)
            .Persist();
}
