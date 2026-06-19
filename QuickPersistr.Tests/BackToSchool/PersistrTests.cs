using Microsoft.EntityFrameworkCore;
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
                .HasMany(
                    new StudentPersistence(),
                    (a, b) => a.Students.Add(b),
                    (a, b) => a.Students.Any(c => c.Id == b.Id),
                    (a, b) => a.Query<BackToSchoolDbContext, Course>(
                        c => c.Set<Course>().Include(d => d.Students).Single(e => e.Id == (int)b!)),
                    a => a.Students.Clear(),
                    a => a.Students.Count == 0)
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
