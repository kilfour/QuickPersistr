using QuickPersistr.Tests.BackToSchool.Model;

namespace QuickPersistr.Tests.BackToSchool;

public class BackToSchoolPersistenceScope : SqlitePersistenceScope<BackToSchoolDbContext>
{
    public BackToSchoolPersistenceScope() : base(a => new BackToSchoolDbContext(a)) { }
}

public abstract class BackToSchoolPersistence<TEntity>
    : Persistence<BackToSchoolDbContext, TEntity>
    where TEntity : class;
