using QuickCheckr;
using QuickCheckr.UnderTheHood.Proceedings.ClerksOffice;

namespace QuickPersistr;

public static class Persistr
{
    public static PersistrBuilderScope Named(string name) => new(name);
}

public class PersistrBuilderScope(string name)
{
    public PersisterBuilderEntities<TReader> Scope<TReader>(Func<IPersistenceScope<TReader>> scopeFactory)
        => new(name, scopeFactory);
}


public class PersisterBuilderEntities<TReader>(
    string name,
    Func<IPersistenceScope<TReader>> scopeFactory)
{
    public PersisterRunner<TReader> Entities(params IPersistence<TReader>[] entities)
        => new(name, scopeFactory, entities);
}


public class PersisterRunner<TReader>(
    string name,
    Func<IPersistenceScope<TReader>> scopeFactory,
    IPersistence<TReader>[] entities)
{
    public void Run()
    {
        var specifications = entities.Select(a => a.Define()).ToList();
        var count = specifications.Sum(a => a.CheckrCount);

        GetCheckr(specifications)
            .Configure(a => a with { FileAs = name, Clerk = CourtClerk.Default().WithStackTrace() })
            .Run(1.Runs(), count.ExecutionsPerRun());
    }

    public CheckrOf<Case> GetCheckr(List<IPersistenceSpecification<TReader>> specifications)
    {
        return from scope in Trackr.Stashed(scopeFactory)
               from _ in Checkr.Sequence([.. specifications.SelectMany(a => a.ToCheckrs(scope))])
               select Case.Closed;
    }
}