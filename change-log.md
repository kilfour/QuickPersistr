### 0.0.1: Should I Stay or Should I Go

* Initial property-based persistence testing DSL, built on QuickCheckr and QuickFuzzr.
* Added create, read, update, and delete checks that reload entities in a fresh session.
* Added generated, generic, and composite identity checks, including non-default and uniqueness assertions.
* Added property checks with custom equality, explicit domain mutations, and post-delete assertions.
* Added one-to-many relationship checks for additive collections, removal, clearing, and reassignment.
* Added rejected create, update, and delete scenarios that verify persisted state is preserved.
* Added optimistic concurrency checks for conflicting updates.
