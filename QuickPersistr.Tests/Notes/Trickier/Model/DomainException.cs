namespace QuickPersistr.Tests.Trickier.Model;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}