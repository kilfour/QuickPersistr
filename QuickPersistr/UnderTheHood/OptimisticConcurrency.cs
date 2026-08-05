using QuickCheckr;
using QuickCheckr.UnderTheHood;

namespace QuickPersistr.UnderTheHood;

public sealed record OptimisticConcurrency<TEntity>(
    string Description,
    Action<TEntity> WinningUpdate,
    Action<TEntity> ConflictingUpdate,
    Func<string, DelayedResult, CheckrOf<Case>> ExpectConflict)
where TEntity : class;
