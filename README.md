# QuickPersistr
> Look out, honey, 'cause I'm using technology


Failure atomicity
Deliberately trigger a rejected update, then verify the previously persisted state remains intact. This catches partial commits and poor transaction handling.

Optimistic concurrency
Load twice, update both copies, and assert the declared conflict behaviour. Valuable, but more advanced and less universal.


Explicit domain mutation declarations, such as Update(a => a.ChangeStatus(...)).
An additive-only collection specification that does not require remove/clear.
Two-session optimistic-concurrency scenarios.
A migration-capable PostgreSQL scope.