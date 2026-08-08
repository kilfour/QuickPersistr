using QuickPersistr.Tests.Notes.BackToSchool.Model;

namespace QuickPersistr.Tests.Notes.BackToSchool;

public class BackToSchoolPersistenceScope : SqlitePersistenceScope<BackToSchoolDbContext>
{
    public BackToSchoolPersistenceScope() : base(a => new BackToSchoolDbContext(a)) { }
}

public abstract class BackToSchoolPersistence<TEntity>
    : Persistence<BackToSchoolDbContext, TEntity>
    where TEntity : class;
