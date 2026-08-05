namespace QuickPersistr.UnderTheHood;

public record AfterDeleteCheck<TReader, TEntity>(
    string Description,
    Func<IPersistenceReader<TReader>, TEntity, bool> Check)
where TEntity : class;
