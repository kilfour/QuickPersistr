namespace QuickPersistr.Tests.Trickier.Model;

public class InvalidCourseTimeRangeException : DomainException
{
    public InvalidCourseTimeRangeException() : base("Start time must be before end time.") { }
}