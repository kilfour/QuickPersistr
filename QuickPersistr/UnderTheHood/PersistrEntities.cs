namespace QuickPersistr.UnderTheHood;

public class PersistrEntities<TContext, TReader>(
    string name,
    Func<TContext> contextFactory,
    Func<TContext, TReader> readerFactory
)
{
    public void Entities() { }
}