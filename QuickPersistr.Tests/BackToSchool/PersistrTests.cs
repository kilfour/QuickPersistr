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
            .Entities(
                new CoursePersistence(),
                new StudentPersistence())
            .Run();
    }

    public class CoursePersistence : Persistence<Course>
    {
        public override IPersistenceSpecification Define() =>
            Entity
                .PrimaryKey(a => a.Id)
                .Property(a => a.Title)
                .Property(a => a.Description)
                // HasMany not yet functional
                .HasMany(new StudentPersistence(), (a, b) => a.Students.Add(b))
                .Persist();
    }

    public class StudentPersistence : Persistence<Student>
    {
        public override IPersistenceSpecification Define() =>
            Entity
                .PrimaryKey(a => a.Id)
                .Property(a => a.Name)
                .Persist();
    }
}
