# QuickPersistr
> Look out, honey, 'cause I'm using technology





Relationship reassignment
Move a dependent from one principal to another and reload both sides. Very effective for detecting FK/navigation disagreement.

Failure atomicity
Deliberately trigger a rejected update, then verify the previously persisted state remains intact. This catches partial commits and poor transaction handling.

Optimistic concurrency
Load twice, update both copies, and assert the declared conflict behaviour. Valuable, but more advanced and less universal.