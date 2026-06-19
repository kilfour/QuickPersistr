namespace QuickPersistr.Tests.WithGenericIdentity;

public readonly record struct Id<T>(Guid Value);
public abstract class DomainEntity<T>
{
    public Id<T> Id { get; protected set; } = new(Guid.NewGuid());

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;

        var other = (DomainEntity<T>)obj;
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }
}
