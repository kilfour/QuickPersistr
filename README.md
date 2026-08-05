# QuickPersistr
> Look out, honey, 'cause I'm using technology


After that, I’d prioritise:
Identity uniqueness
Create several entities and verify every generated key is non-default and distinct. This catches constant/default generators and converter collisions.

Single-child collection mutations
HasMany currently tests add and clear. Removing one child while retaining the others catches faulty collection synchronisation, accidental delete-all behaviour, and equality mistakes.

Relationship reassignment
Move a dependent from one principal to another and reload both sides. Very effective for detecting FK/navigation disagreement.

Failure atomicity
Deliberately trigger a rejected update, then verify the previously persisted state remains intact. This catches partial commits and poor transaction handling.

Optimistic concurrency
Load twice, update both copies, and assert the declared conflict behaviour. Valuable, but more advanced and less universal.