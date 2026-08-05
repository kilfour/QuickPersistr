using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.IgnoredRelationshipReassignment;

[DocFile]
public class IgnoredRelationshipReassignmentTests : PersistrTest<IgnoredRelationshipReassignmentTests>
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
            .Named("Ignored relationship reassignment")
            .Scope(() => new IgnoredReassignmentScope())
            .Entities(new TeamPersistence())
            .StoreCaseFiles(journalist)
            .Run(751926438);

    protected override void Verify(Article article)
    {
        Assert.Equal("Source Team Releases Member", article.FailureDescription());
        Assert.Equal(
            "Expected: { Id: 2, TeamId: 1, Name: \"iqyraeowii\" } absent",
            article.FailingExpectationMessages()[0]);
        Assert.Equal(
            "Actual:   { Id: 2, TeamId: 1, Name: \"iqyraeowii\" } present",
            article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(4, article.Total().Executions());
        Assert.Equal(8, article.Total().Actions());
        Assert.Equal(3, article.Total().Inputs());
        Assert.Equal(4, article.Total().PoolTraces());
        Assert.Equal(7, article.Total().PassedExpectations());
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create Team", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).Input(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("Team-1", article.Execution(1).PoolTrace(1).Read().Value);
        Assert.Equal(4, article.Execution(4).Read().ExecutionId);
        Assert.Equal("Children", article.Execution(4).Input(1).Read().Label);
        Assert.Equal("Entity", article.Execution(4).PoolTrace(1).Read().Label);
        Assert.Equal("Team-1", article.Execution(4).PoolTrace(1).Read().Value);
        Assert.Equal("Can Create Team", article.PassedExpectation(1).Read().Label);
        Assert.Equal("Can Read Team.Id", article.PassedExpectation(2).Read().Label);
        Assert.Equal("Team Can Add Member", article.PassedExpectation(3).Read().Label);
        Assert.Equal("Team Can Remove Member", article.PassedExpectation(4).Read().Label);
        Assert.Equal("Team Retains Other Member", article.PassedExpectation(5).Read().Label);
        Assert.Equal("Source Team Retains Other Member", article.PassedExpectation(6).Read().Label);
        Assert.Equal("Team Can Clear Member", article.PassedExpectation(7).Read().Label);
        Assert.All(
            Enumerable.Range(1, 7),
            index => Assert.Equal(1, article.PassedExpectation(index).Read().TimesPassed));
    }
}

public class Team
{
    public int Id { get; set; }
    public List<Member> Members { get; } = [];
}

public class Member
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TeamPersistence : Persistence<IgnoredReassignmentDbContext, Team>
{
    public override IPersistenceSpecification<IgnoredReassignmentDbContext> Define() =>
        Entity
            .PrimaryKey(team => team.Id)
            .HasMany(many => many
                .From(new MemberPersistence())
                .Add((team, member) => team.Members.Add(member))
                .Remove((team, member) => team.Members.RemoveAll(
                    candidate => candidate.Id == member.Id))
                .Reassign((source, destination, member) =>
                {
                    var persistedMember = source.Members.Single(
                        candidate => candidate.Id == member.Id);
                    source.Members.Remove(persistedMember);
                    destination.Members.Add(persistedMember);
                })
                .Clear(team => team.Members.Clear())
                .Reload((reader, id) => reader.Query(db =>
                    db.Set<Team>()
                        .Include(team => team.Members)
                        .Single(team => team.Id == id)))
                .Contains((team, member) => team.Members.Any(
                    candidate => candidate.Id == member.Id))
                .Empty(team => team.Members.Count == 0))
            .Persist();
}

public class MemberPersistence : Persistence<IgnoredReassignmentDbContext, Member>
{
    public override IPersistenceSpecification<IgnoredReassignmentDbContext> Define() =>
        Entity
            .PrimaryKey(member => member.Id)
            .Property(member => member.Name)
            .Persist();
}

public class IgnoredReassignmentScope()
    : EfPersistenceScope<IgnoredReassignmentDbContext>(
        options => new IgnoredReassignmentDbContext(options));

public class IgnoredReassignmentDbContext(
    DbContextOptions<IgnoredReassignmentDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>()
            .HasMany(team => team.Members)
            .WithOne()
            .HasForeignKey(member => member.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Bug: navigation fix-up changes TeamId in memory, but the update is not persisted.
        modelBuilder.Entity<Member>()
            .Property(member => member.TeamId)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }
}
