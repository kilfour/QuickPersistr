using QuickCheckr;

namespace QuickPersistr;

public static class Persistr
{
    public static PersistrBuilderScope Named(string name) => new(name);
}

public class PersistrBuilderScope(string name)
{
    public PersisterBuilderEntities Scope(Func<IPersistenceScope> scopeFactory)
        => new(name, scopeFactory);
}


public class PersisterBuilderEntities(
    string name,
    Func<IPersistenceScope> scopeFactory)
{
    public PersisterRunner Entities(params IPersistence[] entities)
        => new(name, scopeFactory, entities);
}


public class PersisterRunner(
    string name,
    Func<IPersistenceScope> scopeFactory,
    IPersistence[] entities)
{
    public void Run()
    {
        var specifications = entities.Select(a => a.Define()).ToList();
        var count = specifications.Sum(a => a.CheckrCount);

        GetCheckr(specifications)
            .Configure(a => a with { FileAs = name })
            .Run(1.Runs(), count.ExecutionsPerRun());
    }

    public CheckrOf<Case> GetCheckr(List<IPersistenceSpecification> specifications)
    {
        return from scope in Trackr.Stashed(scopeFactory)
               from _ in Checkr.Sequence([.. specifications.SelectMany(a => a.ToCheckrs(scope))])
               select Case.Closed;
    }
}