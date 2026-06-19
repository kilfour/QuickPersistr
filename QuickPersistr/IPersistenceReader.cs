namespace QuickPersistr;

public interface IPersistenceReader<TReader>
{
    TResult Query<TResult>(Func<TReader, TResult> query);
}