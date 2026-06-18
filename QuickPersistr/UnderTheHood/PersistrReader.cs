namespace QuickPersistr.UnderTheHood;

public class PersistrReader<TContext>(string name, Func<TContext> contextFactory)
{
    public PersistrEntities<TContext, TReader> Reader<TReader>(Func<TContext, TReader> readerFactory) => new(name, contextFactory, readerFactory);
}
