using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.AdditiveOnlyCollection;

public class AdditiveOnlyCollectionTests : PersistrTest<AdditiveOnlyCollectionTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => true;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Additive-only collection")
            .Scope(() => new PlaylistScope())
            .Entities(new PlaylistPersistence())
            .StoreCaseFiles(journalist)
            .Run();

    protected override void Verify(Article article)
    {
        Assert.Equal("", article.FailureDescription());
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(8, article.Total().PassedExpectations());
        Assert.Equal(
            "Playlist Can Add Track",
            article.PassedExpectation(5).Read().Label);
        Assert.DoesNotContain(
            Enumerable.Range(1, 8)
                .Select(index => article.PassedExpectation(index).Read().Label),
            label => label.Contains("Remove") || label.Contains("Clear"));
        Assert.All(
            Enumerable.Range(1, 8),
            index => Assert.Equal(1, article.PassedExpectation(index).Read().TimesPassed));
    }
}

public class Playlist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Track> Tracks { get; } = [];

    public void AddTrack(Track track) => Tracks.Add(track);
}

public class Track
{
    public int Id { get; set; }
    public int PlaylistId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PlaylistPersistence : Persistence<PlaylistDbContext, Playlist>
{
    public override IPersistenceSpecification<PlaylistDbContext> Define() =>
        Entity
            .PrimaryKey(playlist => playlist.Id)
            .Property(playlist => playlist.Name)
            .HasMany(many => many
                .From(new TrackPersistence())
                .Add((playlist, track) => playlist.AddTrack(track))
                .Reload((reader, id) => reader.Query(context =>
                    context.Set<Playlist>()
                        .Include(playlist => playlist.Tracks)
                        .Single(playlist => playlist.Id == id)))
                .Contains((playlist, track) =>
                    playlist.Tracks.Any(candidate => candidate.Id == track.Id)))
            .Persist();
}

public class TrackPersistence : Persistence<PlaylistDbContext, Track>
{
    public override IPersistenceSpecification<PlaylistDbContext> Define() =>
        Entity
            .PrimaryKey(track => track.Id)
            .Property(track => track.Name)
            .Persist();
}

public class PlaylistScope()
    : SqlitePersistenceScope<PlaylistDbContext>(options => new PlaylistDbContext(options));

public class PlaylistDbContext(DbContextOptions<PlaylistDbContext> options)
    : DbContext(options)
{
    public DbSet<Playlist> Playlists => Set<Playlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var playlist = modelBuilder.Entity<Playlist>();
        playlist.HasMany(entity => entity.Tracks)
            .WithOne()
            .HasForeignKey(track => track.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
