using QuickCheckr;
using QuickCheckr.UnderTheHood;

namespace QuickPersistr.UnderTheHood;

public sealed record RejectedOperation<TEntity>(
    string Description,
    Action<TEntity> Attempt,
    Func<string, DelayedResult, CheckrOf<Case>> ExpectRejection)
where TEntity : class;
