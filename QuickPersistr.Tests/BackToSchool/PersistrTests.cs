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

    public class CoursePersistence : BackToSchoolPersistence<Course>
    {
        public override IPersistenceSpecification<BackToSchoolDbContext> Define() =>
            Entity
                .PrimaryKey(a => a.Id)
                .Property(a => a.Title)
                .Property(a => a.Description)
                .HasMany(many => many
                    .From(new StudentPersistence())
                    .AddOne((course, student) => course.Students.Add(student))
                    .Added((course, student) => course.Students.Any(a => a.Id == student.Id))
                    .Reload((reader, id) => reader.Query(
                        a => a.Set<Course>()
                            .Include(course => course.Students)
                            .Single(course => course.Id == id)))
                    .Clear(a => a.Students.Clear())
                    .Cleared(a => a.Students.Count == 0))
                .Persist();
    }

    public class StudentPersistence : BackToSchoolPersistence<Student>
    {
        public override IPersistenceSpecification<BackToSchoolDbContext> Define() =>
            Entity
                .PrimaryKey(a => a.Id)
                .Property(a => a.Name)
                .Persist();
    }
}
