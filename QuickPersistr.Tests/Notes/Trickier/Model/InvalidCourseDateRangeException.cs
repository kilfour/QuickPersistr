namespace QuickPersistr.Tests.Trickier.Model;

public class InvalidCourseDateRangeException : DomainException
{
    public InvalidCourseDateRangeException() : base("Start date must be before end date.") { }
}