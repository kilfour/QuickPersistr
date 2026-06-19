namespace QuickPersistr.Tests.WithGenericIdentity;

public class Thingamajig : DomainEntity<Thingamajig>
{
    public string Description { get; set; } = string.Empty;
}
