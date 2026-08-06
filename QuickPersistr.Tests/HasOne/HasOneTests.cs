using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickFuzzr;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.HasOne;

public class HasOneTests : PersistrTest<HasOneTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Has one")
            .DomainConfiguration(
                Configr.Ignore(property => property.Name == nameof(Person.Passport)))
            .Scope(() => new HasOneScope())
            .Entities(new PersonPersistence())
            .StoreCaseFiles(journalist)
            .Run(127648216);

    protected override void Verify(Article article)
    {
        Assert.Equal("", article.FailureDescription());
        Assert.Equal("", article.VerifyFailed());

        var labels = Enumerable.Range(1, article.Total().PassedExpectations())
            .Select(index => article.PassedExpectation(index).Read().Label)
            .ToList();

        Assert.Contains("Person Can Set Passport", labels);
        Assert.Contains("Person Can Replace Passport", labels);
        Assert.Contains("Person Releases Previous Passport", labels);
        Assert.Contains("Person Can Clear Passport", labels);
        Assert.Contains("Source Person Releases Passport", labels);
        Assert.Contains("Destination Person Receives Passport", labels);
    }
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Passport? Passport { get; private set; }

    public void SetPassport(Passport passport) => Passport = passport;
    public void ClearPassport() => Passport = null;
}

public class Passport
{
    public int Id { get; set; }
    public int? PersonId { get; set; }
    public string Number { get; set; } = string.Empty;
}

public class PersonPersistence : Persistence<HasOneDbContext, Person>
{
    public override IPersistenceSpecification<HasOneDbContext> Define() =>
        Entity
            .PrimaryKey(person => person.Id)
            .Property(person => person.Name)
            .HasOne(one => one
                .From(new PassportPersistence())
                .Set((person, passport) => person.SetPassport(passport))
                .Clear(person => person.ClearPassport())
                .Reload((reader, id) => reader.Query(db =>
                    db.Set<Person>()
                        .Include(person => person.Passport)
                        .Single(person => person.Id == id)))
                .Contains((person, passport) =>
                    person.Passport?.Id == passport.Id)
                .Empty(person => person.Passport is null))
            .HasOne(one => one
                .From(new PassportPersistence())
                .Set((person, passport) => person.SetPassport(passport))
                .Reassign((source, destination, _) =>
                {
                    var passport = source.Passport!;
                    source.ClearPassport();
                    destination.SetPassport(passport);
                })
                .Clear(person => person.ClearPassport())
                .Reload((reader, id) => reader.Query(db =>
                    db.Set<Person>()
                        .Include(person => person.Passport)
                        .Single(person => person.Id == id)))
                .Contains((person, passport) =>
                    person.Passport?.Id == passport.Id)
                .Empty(person => person.Passport is null))
            .HasOne(one => one
                .From(new PassportPersistence())
                .Set((person, passport) => person.SetPassport(passport))
                .Reload((reader, id) => reader.Query(db =>
                    db.Set<Person>()
                        .Include(person => person.Passport)
                        .Single(person => person.Id == id)))
                .Contains((person, passport) =>
                    person.Passport?.Id == passport.Id))
            .Persist();
}

public class PassportPersistence : Persistence<HasOneDbContext, Passport>
{
    public override IPersistenceSpecification<HasOneDbContext> Define() =>
        Entity
            .PrimaryKey(passport => passport.Id)
            .Property(passport => passport.Number)
            .Persist();
}

public class HasOneScope()
    : EfPersistenceScope<HasOneDbContext>(options => new HasOneDbContext(options));

public class HasOneDbContext(DbContextOptions<HasOneDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>()
            .HasOne(person => person.Passport)
            .WithOne()
            .HasForeignKey<Passport>(passport => passport.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
