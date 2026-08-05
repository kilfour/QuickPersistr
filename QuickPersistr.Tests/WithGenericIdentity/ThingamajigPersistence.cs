namespace QuickPersistr.Tests.WithGenericIdentity;

public class ThingamajigPersistence : Persistence<GenericIdentityDbContext, Thingamajig>
{
    public override IPersistenceSpecification<GenericIdentityDbContext> Define() =>
        Entity
            .PrimaryKey(a => a.Id)
            .Property(a => a.Description)
            .Persist();
}
