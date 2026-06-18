using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuickPersistr.Tests.BackToSchool.Model;

namespace QuickPersistr.Tests.BackToSchool;

public class BackToSchoolPersistenceScope : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly BackToSchoolDbContext context;
    public BackToSchoolDbContext Context => context;

    public BackToSchoolPersistenceScope()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<BackToSchoolDbContext>()
            .UseSqlite(connection)
            .Options;
        context = new BackToSchoolDbContext(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    public void SaveAndClear()
    {
        context.SaveChanges();
        context.ChangeTracker.Clear();
    }
}
