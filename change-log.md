### 0.0.2: Let the Children Boogie

* Child specifications supplied through `HasOne.From(...)` and `HasMany.From(...)` now run their complete configured contract through the parent: identity generation and uniqueness, property reads and updates, domain updates, optimistic concurrency, nested relationships, rejected operations, delete, and post-delete expectations. Parent-aware create attempts keep required foreign keys valid, and destructive child checks are ordered to preserve reproducible shrinking.

### 0.0.1: Should I Stay or Should I Go

* Initial property-based persistence testing DSL, built on QuickCheckr and QuickFuzzr.
* Added create, read, update, and delete checks that reload entities in a fresh session.
* Added generated, generic, and composite identity checks, including non-default and uniqueness assertions.
* Added property checks with custom equality, explicit domain mutations, and post-delete assertions.
* Added one-to-one and one-to-many relationship checks for setting, replacement, additive collections, removal, clearing, and reassignment.
* Added rejected create, update, and delete scenarios that verify persisted state is preserved.
* Added optimistic concurrency checks for conflicting updates.
