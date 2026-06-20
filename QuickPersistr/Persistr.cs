using QuickCheckr;
using QuickCheckr.Diagnostics;
using QuickCheckr.Protocol;
using QuickCheckr.Protocol.Custodians;
using QuickCheckr.UnderTheHood;
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
    private Func<CheckrConfig, CheckrConfig> configure = a => a;
    public PersisterRunner<TReader> StoreCaseFiles(ICustodian? custodian = null)
    {
        configure = a => a with { FileAs = name, Custodian = custodian ?? Custodian.Default };
        return this;
    }

    public ConfiguredCheckr Run()
    {
        var specifications = entities.Select(a => a.Define()).ToList();
        var count = specifications.Sum(a => a.CheckrCount);

        return GetCheckr(specifications)
            .Configure(a => configure(a with
            {
                Clerk = CourtClerk.Default().WithStackTrace(),
                WarningLevel = WarningLevel.None
            }))
            .Run(1.Runs(), count.ExecutionsPerRun());
    }

    // public ConfiguredCheckr Autopsy(int seed, AutopsyProbe probe)
    // {
    //     var specifications = entities.Select(a => a.Define()).ToList();
    //     var count = specifications.Sum(a => a.CheckrCount);

    //     return GetCheckr(specifications)
    //         .Configure(a => configure(a with
    //         {
    //             Clerk = CourtClerk.Default().WithStackTrace(),
    //             WarningLevel = WarningLevel.None
    //         }))
    //         .Autopsy(seed, count.ExecutionsPerRun(), probe);
    // }

    private CheckrOf<Case> GetCheckr(List<IPersistenceSpecification<TReader>> specifications)
    {
        return from scope in Trackr.Stashed(scopeFactory)
               from _ in Checkr.Sequence([.. specifications.SelectMany(a => a.ToCheckrs(scope))])
               select Case.Closed;
    }
}