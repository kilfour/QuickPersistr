using QuickCheckr;
using QuickCheckr.Protocol;
using QuickCheckr.Protocol.Custodians;
using QuickCheckr.UnderTheHood;
using QuickCheckr.UnderTheHood.Proceedings.ClerksOffice;
using QuickFuzzr;
using QuickFuzzr.UnderTheHood;

namespace QuickPersistr;

public static class Persistr
{
    public static PersistrBuilderScope Named(string name) => new(name);
}

public class PersistrBuilderScope(string name)
{
    private FuzzrOf<Intent> domainConfig = Result.Unit;

    public PersistrBuilderScope DomainConfiguration(FuzzrOf<Intent> domainConfig)
    {
        this.domainConfig = domainConfig;
        return this;
    }
    public PersisterBuilderEntities<TReader> Scope<TReader>(Func<IPersistenceScope<TReader>> scopeFactory)
        => new(name, domainConfig, scopeFactory);
}


public class PersisterBuilderEntities<TReader>(
    string name,
    FuzzrOf<Intent> domainConfig,
    Func<IPersistenceScope<TReader>> scopeFactory)
{
    public PersisterRunner<TReader> Entities(params IPersistence<TReader>[] entities)
        => new(name, domainConfig, scopeFactory, entities);
}


public class PersisterRunner<TReader>(
    string name,
    FuzzrOf<Intent> domainConfig,
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
        var cfgCheck = GetConfiguredCheckr();
        return cfgCheck.Checkr.Run(1.Runs(), cfgCheck.ExecutionCount);
    }

    public ConfiguredCheckr Run(RunCount runs)
    {
        var cfgCheck = GetConfiguredCheckr();
        return cfgCheck.Checkr.Run(runs, cfgCheck.ExecutionCount);
    }

    public ConfiguredCheckr Run(int seed)
    {
        var cfgCheck = GetConfiguredCheckr();
        return cfgCheck.Checkr.Run(seed, cfgCheck.ExecutionCount);
    }

    private (ConfiguredCheckr Checkr, ExecutionCount ExecutionCount) GetConfiguredCheckr()
    {
        var specifications = entities.Select(a => a.Define()).ToList();
        var count = specifications.Sum(a => a.CheckrCount);

        return (
            GetCheckr(specifications)
                .Configure(a => configure(a with
                {
                    FileAs = name,
                    Clerk = CourtClerk.Default().WithStackTrace(),
                    WarningLevel = WarningLevel.Verbose,
                    ShrinkMode = a.ShrinkMode | ShrinkMode.Reduction,
                })),
            count.ExecutionsPerRun());
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

    private CheckrOf<Case> GetCheckr(List<IPersistenceSpecification<TReader>> specifications) =>
        from scope in Trackr.Stashed(scopeFactory)
        from cfg in Trackr.Configr(domainConfig)
        from seq in Checkr.Sequence([.. specifications.SelectMany(a => a.ToCheckrs(scope))])
        select Case.Closed;
}